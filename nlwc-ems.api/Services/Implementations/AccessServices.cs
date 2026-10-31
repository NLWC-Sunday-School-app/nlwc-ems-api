using System.Text.Json;
using nlwc.ems.Core.Models;
using nlwc.ems.Core.Security;
using nlwc.ems.Dtos;

namespace nlwc.ems.Services.Implementations;

public class RoleService(IRoleDao roleDao) : BaseService<Role>(roleDao), IRoleService
{
    public Task<Role?> RetrieveByKeyAsync(string key) => roleDao.RetrieveByKeyAsync(key);
}

public class RoleGrantService(IRoleGrantDao grantDao, IRoleDao roleDao, IAuditEventPublisher auditPublisher)
    : BaseService<RoleGrant>(grantDao), IRoleGrantService
{
    public async Task<IReadOnlyList<string>> GetPermissionCodesAsync(Guid accountId)
    {
        var grants = await grantDao.RetrieveActiveForAccountAsync(accountId);
        return grants
            .SelectMany(g => (g.Role?.Permissions ?? [])
                .SelectMany(p => EmsPermissions.Expand(p.Resource, p.Level))
                .Select(code => g.EditionId is { } edition ? $"{code}@{edition}" : code))
            .Distinct()
            .ToList();
    }

    public async Task<RoleGrant> GrantAsync(Guid actorAccountId, GrantRoleRequest request)
    {
        if (request.ExpiresAt is { } expires && expires <= DateTime.UtcNow)
            throw new ValidationException(["The expiry must be in the future."]);

        var role = await roleDao.RetrieveByKeyAsync(request.Role) ?? throw new ValidationException(["Unknown role."], "unknown_role");
        return await InsertOrThrowAsync(new RoleGrant
        {
            Id = Guid.NewGuid(), AccountId = request.AccountId!.Value, RoleId = role.Id, EditionId = request.EditionId,
            GrantedBy = actorAccountId, Reason = request.Reason.Trim(), ExpiresAt = request.ExpiresAt
        });
    }

    public async Task RevokeAsync(Guid actorAccountId, Guid grantId, string reason)
    {
        if (!await grantDao.RevokeAsync(grantId)) throw new NotFoundException("Grant not found, or already revoked.");

        // The generic update is audited by the DAO; this adds the reason a person gave.
        await auditPublisher.PublishAsync(new EntityAuditEvent
        {
            EntityType = nameof(RoleGrant), RecordId = grantId.ToString(), Action = "role.revoked", Reason = reason, UserId = actorAccountId.ToString()
        });
    }
}

public class AuditService(IAuditEventDao auditDao) : IAuditService
{
    public Task<PagerEntity<AuditEvent>> RetrievePagedAsync(int page, int max, string? action) => auditDao.RetrievePagedAsync(page, max, action);

    public Task<long?> VerifyChainAsync() => auditDao.VerifyChainAsync();
}

/// <summary>
/// Writes audit events into the hash-chained <c>audit_events</c> table. Per the <see cref="IAuditEventPublisher"/>
/// contract it never throws into the caller, so a missing audit row cannot fail a save; the failure is logged loudly.
/// </summary>
public class DbAuditEventPublisher(IAuditEventDao auditDao, ILogger<DbAuditEventPublisher> logger) : IAuditEventPublisher
{
    public async Task PublishAsync(EntityAuditEvent e)
    {
        try
        {
            await auditDao.AppendAsync(new AuditEvent
            {
                Id = Guid.CreateVersion7(),
                ActorAccountId = Guid.TryParse(e.UserId, out var actor) ? actor : null,
                Action = e.Action,
                EntityType = e.EntityType,
                RecordId = e.RecordId,
                DisplayName = e.DisplayName,
                EditionId = e.EditionId,
                Reason = e.Reason,
                Detail = JsonSerializer.Serialize(new { oldValues = e.OldValues, newValues = e.NewValues, detail = e.Detail, ip = e.IpAddress })
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not write audit event {Action} for {EntityType} {RecordId}", e.Action, e.EntityType, e.RecordId);
        }
    }
}
