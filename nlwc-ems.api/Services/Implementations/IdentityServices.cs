using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OtpNet;
using nlwc.ems.Core.Models;
using nlwc.ems.Core.Security;
using nlwc.ems.Dtos;
using nlwc.ems.Services.Options;

namespace nlwc.ems.Services.Implementations;

public class AccountService(IAccountDao accountDao, IEmailVerificationDao verificationDao, ISessionService sessionService,
    IPasswordHasher hasher, ITokenGenerator tokens, IMailer mailer)
    : BaseService<Account>(accountDao), IAccountService
{
    private const int MaxFailedLogins = 5;
    private static readonly TimeSpan LockFor = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan VerificationLifetime = TimeSpan.FromHours(24);

    public async Task SignUpAsync(SignUpRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        // Hash first, whether or not the email exists, so both paths cost the same.
        var passwordHash = hasher.Hash(request.Password);
        var token = tokens.NewToken();

        var created = false;
        if (!await accountDao.EmailExistsAsync(email))
        {
            var person = new Person { Id = Guid.NewGuid(), GivenName = request.GivenName.Trim(), FamilyName = request.FamilyName.Trim(), Gender = request.Gender };
            var account = new Account { Id = Guid.NewGuid(), PersonId = person.Id, Email = email, PasswordHash = passwordHash };
            var contact = new VerifiedContact { Id = Guid.NewGuid(), PersonId = person.Id, Kind = ContactKind.Email, Value = email };
            var verification = new EmailVerification
            {
                Id = Guid.NewGuid(), AccountId = account.Id, TokenHash = tokens.Hash(token), ExpiresAt = DateTime.UtcNow + VerificationLifetime
            };
            try
            {
                await accountDao.InsertWithPersonAsync(person, account, contact, verification);
                created = true;
            }
            catch (DbUpdateException)
            {
                // Lost a race with another sign-up for the same email: it exists now, so treat it as existing.
            }
        }

        await mailer.SendAsync(email, created ? "verify_email" : "account_exists",
            created ? new Dictionary<string, string> { ["token"] = token } : new Dictionary<string, string>());
    }

    public async Task VerifyEmailAsync(string token)
    {
        if (!await verificationDao.ConsumeAsync(tokens.Hash(token)))
            throw new AppException("This verification link is invalid or has expired.", code: "invalid_token");
    }

    public async Task<(string Token, SessionPrincipal Principal)> LogInAsync(string email, string password, Surface surface)
    {
        email = email.Trim().ToLowerInvariant();
        var account = await accountDao.RetrieveByEmailAsync(email);

        // Verify even for an unknown account so the response time does not reveal which emails exist.
        var passwordOk = hasher.Verify(account?.PasswordHash ?? hasher.DummyHash, password);
        if (account?.PasswordHash is null) throw InvalidCredentials();

        if (account.LockedUntil > DateTime.UtcNow)
            throw new UnauthorizedException("Too many failed attempts. Try again later.", "locked");

        if (!passwordOk || account.Status != BaseStatus.Active)
        {
            await accountDao.RegisterFailedLoginAsync(account.Id, MaxFailedLogins, DateTime.UtcNow + LockFor);
            throw InvalidCredentials();
        }

        await accountDao.ResetFailedLoginsAsync(account.Id);
        return await sessionService.CreateAsync(account.Id, surface);
    }

    private static UnauthorizedException InvalidCredentials() => new("Invalid email or password.", "invalid_credentials");
}

public class SessionService(IAuthSessionDao sessionDao, IRoleGrantService grantService, ITokenGenerator tokens, IOptions<SecurityOptions> security)
    : BaseService<AuthSession>(sessionDao), ISessionService
{
    private static TimeSpan Idle(Surface s) => SessionPolicy.Idle(s);
    private static TimeSpan Absolute(Surface s) => SessionPolicy.Absolute(s);

    public async Task<(string Token, SessionPrincipal Principal)> CreateAsync(Guid accountId, Surface surface)
    {
        var token = tokens.NewToken();
        var now = DateTime.UtcNow;
        await sessionDao.InsertAsync(new AuthSession
        {
            Id = Guid.NewGuid(), AccountId = accountId, TokenHash = tokens.Hash(token), Surface = surface, LastSeenAt = now,
            IdleExpiresAt = now + Idle(surface), AbsoluteExpiresAt = now + Absolute(surface)
        });
        return (token, (await ResolveAsync(token))!);
    }

    public async Task<SessionPrincipal?> ResolveAsync(string token)
    {
        var resolved = await sessionDao.RetrieveActiveAsync(tokens.Hash(token));
        if (resolved is null) return null;

        var s = resolved.Session;
        await sessionDao.TouchAsync(s.Id, DateTime.UtcNow + Idle(s.Surface));
        var permissions = await grantService.GetPermissionCodesAsync(s.AccountId);
        return new SessionPrincipal(s.Id, s.AccountId, resolved.PersonId, s.Surface, !security.Value.RequireMfa || s.MfaVerifiedAt is not null, resolved.EmailVerified, permissions);
    }

    public Task LogOutAsync(Guid sessionId) => sessionDao.RevokeAsync(sessionId);

    public Task MarkMfaVerifiedAsync(Guid sessionId) => sessionDao.MarkMfaVerifiedAsync(sessionId);
}

public class MfaService(IMfaFactorDao factorDao, IAccountDao accountDao, ISessionService sessionService, IFieldEncryptor encryptor)
    : BaseService<MfaFactor>(factorDao), IMfaService
{
    public async Task<string> EnrollAsync(Guid accountId)
    {
        var account = await accountDao.RetrieveByIdAsync(accountId) ?? throw new NotFoundException("Account not found.");
        if (await factorDao.HasConfirmedAsync(accountId))
            throw new AppException("An authenticator is already set up. Ask an administrator to reset it if the device is lost.", isConflict: true, code: "mfa_already_enrolled");
        var secret = KeyGeneration.GenerateRandomKey(20);
        await factorDao.ReplacePendingAsync(new MfaFactor { Id = Guid.NewGuid(), AccountId = accountId, SecretEncrypted = encryptor.Encrypt(secret) });
        return new OtpUri(OtpType.Totp, secret, account.Email, "NLWC EMS").ToString();
    }

    public Task<bool> IsEnrolledAsync(Guid accountId) => factorDao.HasConfirmedAsync(accountId);

    public async Task VerifyAsync(Guid accountId, Guid sessionId, string code)
    {
        var factor = await factorDao.RetrieveLatestAsync(accountId)
            ?? throw new AppException("Set up an authenticator first.", code: "mfa_required");
        var totp = new Totp(encryptor.Decrypt(factor.SecretEncrypted));
        if (!totp.VerifyTotp(code, out _, new VerificationWindow(1, 1)))
            throw new AppException("That code is not correct.", code: "invalid_code");

        if (factor.ConfirmedAt is null) await factorDao.ConfirmAsync(factor.Id);
        await sessionService.MarkMfaVerifiedAsync(sessionId);
    }
}
