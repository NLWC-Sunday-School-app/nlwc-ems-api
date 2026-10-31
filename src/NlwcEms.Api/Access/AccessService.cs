using Dapper;
using Npgsql;
using NlwcEms.Api.Identity;

namespace NlwcEms.Api.Access;

public sealed record PermissionHeld(string Resource, string Level);

public sealed class AccessService(NpgsqlDataSource db, AuditService audit)
{
    /// <summary>Highest level held per resource from active grants that apply to the edition (church-wide grants always apply).</summary>
    public async Task<IReadOnlyList<PermissionHeld>> PermissionsAsync(Guid accountId, Guid? editionId)
    {
        await using var conn = await db.OpenConnectionAsync();
        var rows = await conn.QueryAsync<PermissionHeld>(
            """
            SELECT DISTINCT rp.resource, rp.level
            FROM role_grant g
            JOIN role_permission rp ON rp.role_id = g.role_id
            WHERE g.account_id = @accountId AND g.revoked_at IS NULL
              AND (g.expires_at IS NULL OR g.expires_at > now())
              AND (g.edition_id IS NULL OR g.edition_id = @editionId)
            """, new { accountId, editionId });
        return rows.GroupBy(r => r.Resource)
            .Select(g => g.Aggregate((a, b) => Permissions.Satisfies(g.Key, a.Level, b.Level) ? a : b))
            .ToList();
    }

    /// <summary>The single policy check: can(actor, resource, level, edition).</summary>
    public async Task<bool> CanAsync(Guid accountId, string resource, string level, Guid? editionId) =>
        Permissions.Known(resource, level) &&
        (await PermissionsAsync(accountId, editionId)).Any(p => p.Resource == resource && Permissions.Satisfies(resource, p.Level, level));

    public async Task<Guid> GrantAsync(Guid actor, Guid accountId, string roleKey, Guid? editionId, string reason, DateTime? expiresAt)
    {
        await using var conn = await db.OpenConnectionAsync();
        var roleId = await conn.ExecuteScalarAsync<Guid?>("SELECT id FROM role WHERE key = @roleKey", new { roleKey })
            ?? throw new AuthException("unknown_role");
        var id = Crypto.NewId();
        await conn.ExecuteAsync(
            "INSERT INTO role_grant(id, account_id, role_id, edition_id, granted_by, reason, expires_at) VALUES (@id, @accountId, @roleId, @editionId, @actor, @reason, @expiresAt)",
            new { id, accountId, roleId, editionId, actor, reason, expiresAt });
        await audit.WriteAsync(actor, "role.granted", "staff", editionId, reason, new { grantId = id, accountId, roleKey, expiresAt });
        return id;
    }

    public async Task RevokeAsync(Guid actor, Guid grantId, string reason)
    {
        await using var conn = await db.OpenConnectionAsync();
        var n = await conn.ExecuteAsync("UPDATE role_grant SET revoked_at = now() WHERE id = @grantId AND revoked_at IS NULL", new { grantId });
        if (n == 0) throw new AuthException("not_found");
        await audit.WriteAsync(actor, "role.revoked", "staff", null, reason, new { grantId });
    }
}
