using Dapper;
using Npgsql;
using OtpNet;

namespace NlwcEms.Api.Identity;

public enum Surface { Web, Admin, Checkin }

public sealed class AuthException(string code) : Exception(code)
{
    public string Code { get; } = code;
}

public sealed record SessionInfo(Guid SessionId, Guid AccountId, Guid PersonId, Surface Surface, bool MfaVerified, bool EmailVerified);

public sealed class IdentityService(NpgsqlDataSource db, AppConfig config, IMailer mailer)
{
    private const int MaxFailed = 5;
    private static readonly TimeSpan LockFor = TimeSpan.FromMinutes(15);

    private static TimeSpan Idle(Surface s) => s switch { Surface.Admin => TimeSpan.FromMinutes(30), Surface.Checkin => TimeSpan.FromMinutes(15), _ => TimeSpan.FromHours(8) };
    private static TimeSpan Absolute(Surface s) => s switch { Surface.Web => TimeSpan.FromDays(30), _ => TimeSpan.FromHours(12) };
    private static string Db(Surface s) => s.ToString().ToLowerInvariant();
    private static Surface FromDb(string s) => Enum.Parse<Surface>(s, true);

    /// <summary>Always succeeds from the caller's view, so sign-up cannot reveal who has an account.</summary>
    public async Task SignUpAsync(string emailRaw, string password, string givenName, string familyName, string? gender)
    {
        var email = emailRaw.Trim().ToLowerInvariant();
        var hash = Crypto.HashPassword(password);
        var token = Crypto.NewToken();

        await using var conn = await db.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();
        var exists = await conn.ExecuteScalarAsync<bool>("SELECT EXISTS (SELECT 1 FROM account WHERE email = @email)", new { email }, tx);
        var created = false;
        if (!exists)
        {
            Guid personId = Crypto.NewId(), accountId = Crypto.NewId();
            await conn.ExecuteAsync("INSERT INTO person(id, given_name, family_name, gender) VALUES (@personId, @givenName, @familyName, @gender)", new { personId, givenName, familyName, gender }, tx);
            await conn.ExecuteAsync("INSERT INTO account(id, person_id, email, password_hash) VALUES (@accountId, @personId, @email, @hash)", new { accountId, personId, email, hash }, tx);
            await conn.ExecuteAsync("INSERT INTO verified_contact(id, person_id, kind, value) VALUES (@id, @personId, 'email', @email)", new { id = Crypto.NewId(), personId, email }, tx);
            await conn.ExecuteAsync("INSERT INTO email_verification(id, account_id, token_hash, expires_at) VALUES (@id, @accountId, @th, now() + interval '24 hours')", new { id = Crypto.NewId(), accountId, th = Crypto.HashToken(token) }, tx);
            created = true;
        }
        await tx.CommitAsync();
        await mailer.SendAsync(email, created ? "verify_email" : "account_exists", created ? new Dictionary<string, string> { ["token"] = token } : new Dictionary<string, string>());
    }

    private sealed record VerificationRow(Guid Id, Guid AccountId, DateTime ExpiresAt, DateTime? UsedAt);

    public async Task VerifyEmailAsync(string token)
    {
        await using var conn = await db.OpenConnectionAsync();
        await using var tx = await conn.BeginTransactionAsync();
        var row = await conn.QuerySingleOrDefaultAsync<VerificationRow>(
            "SELECT id, account_id AS AccountId, expires_at AS ExpiresAt, used_at AS UsedAt FROM email_verification WHERE token_hash = @th FOR UPDATE",
            new { th = Crypto.HashToken(token) }, tx);
        if (row is null || row.UsedAt is not null || row.ExpiresAt < DateTime.UtcNow) throw new AuthException("invalid_token");
        await conn.ExecuteAsync("UPDATE email_verification SET used_at = now() WHERE id = @id", new { id = row.Id }, tx);
        await conn.ExecuteAsync(
            """
            UPDATE verified_contact vc SET verified_at = now()
            FROM account a WHERE a.id = @aid AND vc.person_id = a.person_id AND vc.kind = 'email' AND vc.value = a.email
            """, new { aid = row.AccountId }, tx);
        await tx.CommitAsync();
    }

    private sealed record AccountRow(Guid Id, string? PasswordHash, string Status, int FailedLogins, DateTime? LockedUntil);

    public async Task<(string Token, SessionInfo Session)> LogInAsync(string emailRaw, string password, Surface surface)
    {
        var email = emailRaw.Trim().ToLowerInvariant();
        await using var conn = await db.OpenConnectionAsync();
        var acct = await conn.QuerySingleOrDefaultAsync<AccountRow>(
            "SELECT id, password_hash AS PasswordHash, status, failed_logins AS FailedLogins, locked_until AS LockedUntil FROM account WHERE email = @email", new { email });
        var ok = Crypto.VerifyPassword(acct?.PasswordHash ?? Crypto.DummyHash, password);
        if (acct is null || acct.PasswordHash is null) throw new AuthException("invalid_credentials");
        if (acct.LockedUntil > DateTime.UtcNow) throw new AuthException("locked");
        if (!ok || acct.Status != "active")
        {
            var failed = acct.FailedLogins + 1;
            DateTime? until = failed >= MaxFailed ? DateTime.UtcNow + LockFor : null;
            await conn.ExecuteAsync("UPDATE account SET failed_logins = @failed, locked_until = @until WHERE id = @id", new { failed, until, id = acct.Id });
            throw new AuthException("invalid_credentials");
        }
        await conn.ExecuteAsync("UPDATE account SET failed_logins = 0, locked_until = NULL WHERE id = @id", new { id = acct.Id });

        var token = Crypto.NewToken();
        await conn.ExecuteAsync(
            "INSERT INTO session(id, account_id, token_hash, surface, idle_expires_at, absolute_expires_at) VALUES (@id, @aid, @th, @surface, @idle, @abs)",
            new { id = Crypto.NewId(), aid = acct.Id, th = Crypto.HashToken(token), surface = Db(surface), idle = DateTime.UtcNow + Idle(surface), abs = DateTime.UtcNow + Absolute(surface) });
        return (token, (await ResolveAsync(token))!);
    }

    private sealed record SessionRow(Guid Id, Guid AccountId, string Surface, DateTime? MfaVerifiedAt, Guid PersonId, DateTime? EmailVerifiedAt);

    public async Task<SessionInfo?> ResolveAsync(string token)
    {
        await using var conn = await db.OpenConnectionAsync();
        var r = await conn.QuerySingleOrDefaultAsync<SessionRow>(
            """
            SELECT s.id, s.account_id AS AccountId, s.surface, s.mfa_verified_at AS MfaVerifiedAt, a.person_id AS PersonId, vc.verified_at AS EmailVerifiedAt
            FROM session s
            JOIN account a ON a.id = s.account_id
            LEFT JOIN verified_contact vc ON vc.person_id = a.person_id AND vc.kind = 'email' AND vc.value = a.email
            WHERE s.token_hash = @th AND s.revoked_at IS NULL AND s.idle_expires_at > now() AND s.absolute_expires_at > now()
            """, new { th = Crypto.HashToken(token) });
        if (r is null) return null;
        var surface = FromDb(r.Surface);
        await conn.ExecuteAsync("UPDATE session SET last_seen_at = now(), idle_expires_at = now() + @idle WHERE id = @id", new { id = r.Id, idle = Idle(surface) });
        return new SessionInfo(r.Id, r.AccountId, r.PersonId, surface, r.MfaVerifiedAt is not null, r.EmailVerifiedAt is not null);
    }

    public async Task LogOutAsync(Guid sessionId)
    {
        await using var conn = await db.OpenConnectionAsync();
        await conn.ExecuteAsync("UPDATE session SET revoked_at = now() WHERE id = @sessionId", new { sessionId });
    }

    // ---- TOTP MFA ----
    public async Task<string> MfaEnrollAsync(SessionInfo s)
    {
        await using var conn = await db.OpenConnectionAsync();
        var email = await conn.ExecuteScalarAsync<string>("SELECT email::text FROM account WHERE id = @id", new { id = s.AccountId });
        var secret = KeyGeneration.GenerateRandomKey(20);
        await conn.ExecuteAsync("DELETE FROM mfa_factor WHERE account_id = @id AND confirmed_at IS NULL", new { id = s.AccountId });
        await conn.ExecuteAsync("INSERT INTO mfa_factor(id, account_id, secret_encrypted) VALUES (@id, @aid, @sec)",
            new { id = Crypto.NewId(), aid = s.AccountId, sec = Crypto.Encrypt(config.MasterKey, secret) });
        return new OtpUri(OtpType.Totp, secret, email, "NLWC EMS").ToString();
    }

    private sealed record FactorRow(Guid Id, byte[] Secret, DateTime? ConfirmedAt);

    /// <summary>Confirms a pending factor, or step-up verifies an existing one. Marks the session MFA-verified.</summary>
    public async Task MfaVerifyAsync(SessionInfo s, string code)
    {
        await using var conn = await db.OpenConnectionAsync();
        var f = await conn.QuerySingleOrDefaultAsync<FactorRow>(
            "SELECT id, secret_encrypted AS Secret, confirmed_at AS ConfirmedAt FROM mfa_factor WHERE account_id = @id ORDER BY confirmed_at DESC NULLS LAST LIMIT 1", new { id = s.AccountId });
        if (f is null) throw new AuthException("mfa_required");
        var totp = new Totp(Crypto.Decrypt(config.MasterKey, f.Secret));
        if (!totp.VerifyTotp(code, out _, new VerificationWindow(1, 1))) throw new AuthException("invalid_code");
        if (f.ConfirmedAt is null) await conn.ExecuteAsync("UPDATE mfa_factor SET confirmed_at = now() WHERE id = @id", new { id = f.Id });
        await conn.ExecuteAsync("UPDATE session SET mfa_verified_at = now() WHERE id = @id", new { id = s.SessionId });
    }
}
