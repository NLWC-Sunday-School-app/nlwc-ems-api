using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using nlwc.ems.Core.Models;

namespace nlwc.ems.Data.EFCore.Implementations;

public class RoleDao(EmsDbContext dbContext, IHttpContextAccessor httpContextAccessor, IAuditEventPublisher auditEventPublisher)
    : BaseDao<EmsDbContext, Role>(dbContext, httpContextAccessor, auditEventPublisher), IRoleDao
{
    protected override IQueryable<Role> Query() => base.Query().Include(r => r.Permissions);

    public Task<Role?> RetrieveByKeyAsync(string key) => Query().FirstOrDefaultAsync(r => r.Key == key);
}

public class RoleGrantDao(EmsDbContext dbContext, IHttpContextAccessor httpContextAccessor, IAuditEventPublisher auditEventPublisher)
    : BaseDao<EmsDbContext, RoleGrant>(dbContext, httpContextAccessor, auditEventPublisher), IRoleGrantDao
{
    protected override IQueryable<RoleGrant> Query() => base.Query().Include(g => g.Role);

    public Task<List<RoleGrant>> RetrieveActiveForAccountAsync(Guid accountId)
    {
        var now = DateTime.UtcNow;
        return DbContext.Set<RoleGrant>().AsNoTracking()
            .Include(g => g.Role!).ThenInclude(r => r.Permissions)
            .Where(g => g.AccountId == accountId && g.RevokedAt == null && (g.ExpiresAt == null || g.ExpiresAt > now))
            .ToListAsync();
    }

    public async Task<bool> RevokeAsync(Guid grantId)
    {
        var grant = await DbContext.Set<RoleGrant>().AsNoTracking().FirstOrDefaultAsync(g => g.Id == grantId && g.RevokedAt == null);
        if (grant is null) return false;

        grant.RevokedAt = DateTime.UtcNow;
        await UpdateAsync(grant);   // audited as an Update by BaseDao
        return true;
    }
}

public class AuditEventDao(EmsDbContext dbContext) : IAuditEventDao
{
    public const string Genesis = "genesis";

    /// <summary>Each row's hash covers the previous hash, so an edit or removal anywhere breaks every later link.</summary>
    public static string ComputeHash(string prev, AuditEvent e) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|',
            prev, e.Id, e.OccurredAt.ToUniversalTime().ToString("O"), e.ActorAccountId, e.Action, e.EntityType, e.RecordId, e.EditionId, e.Reason, e.Detail))));

    public async Task AppendAsync(AuditEvent auditEvent)
    {
        // Joins the caller's transaction when there is one (a DAO writing several rows), so the audit row commits or rolls back
        // with the change it describes.
        var ownTransaction = dbContext.Database.CurrentTransaction is null;
        await using var tx = ownTransaction ? await dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted) : null;
        // One row lock, held until the transaction ends, so two appends can never read the same previous hash. (READ COMMITTED,
        // set by every transaction in this app, makes the read below see the latest committed row once the lock is ours.)
        await dbContext.Database.ExecuteSqlRawAsync("SELECT id FROM audit_chain_lock WHERE id = 1 FOR UPDATE");
        var prev = await dbContext.Set<AuditEvent>().AsNoTracking().OrderByDescending(a => a.Seq).Select(a => a.Hash).FirstOrDefaultAsync() ?? Genesis;

        // datetime(6) keeps microseconds, so truncate to keep the stored value hashing the same as the one computed here.
        auditEvent.OccurredAt = new DateTime(DateTime.UtcNow.Ticks / 10 * 10, DateTimeKind.Utc);
        auditEvent.PrevHash = prev;
        auditEvent.Hash = ComputeHash(prev, auditEvent);

        dbContext.Set<AuditEvent>().Add(auditEvent);
        await dbContext.SaveChangesAsync();
        if (tx is not null) await tx.CommitAsync();
        dbContext.Entry(auditEvent).State = EntityState.Detached;
    }

    public async Task<PagerEntity<AuditEvent>> RetrievePagedAsync(int page, int max, string? action)
    {
        var query = dbContext.Set<AuditEvent>().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(a => a.Action == action);
        var count = await query.CountAsync();
        var data = await query.OrderByDescending(a => a.Seq).Skip(page * max).Take(max).ToListAsync();
        return new PagerEntity<AuditEvent> { Page = page, Max = max, Count = count, Data = data };
    }

    public async Task<long?> VerifyChainAsync()
    {
        var prev = Genesis;
        await foreach (var e in dbContext.Set<AuditEvent>().AsNoTracking().OrderBy(a => a.Seq).AsAsyncEnumerable())
        {
            if (e.PrevHash != prev || e.Hash != ComputeHash(prev, e)) return e.Seq;
            prev = e.Hash;
        }
        return null;
    }
}
