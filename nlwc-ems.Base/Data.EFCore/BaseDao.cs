using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using nlwc.ems.Base.Core;
using nlwc.ems.Base.Core.Audit;
using nlwc.ems.Base.Core.Paging;
using nlwc.ems.Base.Core.Security;
using nlwc.ems.Base.Services.Interfaces;

namespace nlwc.ems.Base.Data.EFCore;

public abstract class BaseDao<TContext, T>(
    TContext dbContext, IHttpContextAccessor httpContextAccessor, IAuditEventPublisher auditEventPublisher) : IBaseDao<T>
    where TContext : DbContext
    where T : BaseEntity
{
    private static readonly bool IsAuditable = typeof(T).IsDefined(typeof(AuditableAttribute), inherit: false);

    protected TContext DbContext { get; } = dbContext;

    protected virtual IQueryable<T> Query() => DbContext.Set<T>().AsNoTracking();

    public virtual async Task<PagerEntity<T>> RetrievePagedAsync(int page, int max, Expression<Func<T, bool>>? predicate = null)
    {
        var query = Query();
        if (predicate is not null) query = query.Where(predicate);

        var count = await query.CountAsync();
        // The Id tiebreaker is not cosmetic: rows with the same DateCreated otherwise fall back to the engine's
        // physical order, which an UPDATE silently reshuffles. Id is stable and unique.
        var data = await query
            .OrderByDescending(x => x.DateCreated)
            .ThenByDescending(x => x.Id)
            .Skip(page * max)
            .Take(max)
            .ToListAsync();

        return new PagerEntity<T> { Page = page, Max = max, Count = count, Data = data };
    }

    public virtual async Task<IEnumerable<T>> RetrieveAllAsync()
        => await Query().OrderByDescending(x => x.DateCreated).ThenByDescending(x => x.Id).ToListAsync();

    public virtual Task<T?> RetrieveByIdAsync(Guid id) => Query().FirstOrDefaultAsync(x => x.Id == id);

    public virtual async Task<IEnumerable<T>> RetrieveByIdsAsync(IEnumerable<Guid> ids)
        => await Query().Where(x => ids.Contains(x.Id)).ToListAsync();

    public virtual async Task<T> InsertAsync(T item)
    {
        var now = DateTime.UtcNow;
        item.DateCreated = now;
        item.DateModified = now;
        item.CreatedBy = CurrentUserId;
        item.CreatedByIp = CurrentIp;
        item.ModifiedBy = item.CreatedBy;
        item.ModifiedByIp = item.CreatedByIp;

        DbContext.Set<T>().Add(item);
        await DbContext.SaveChangesAsync();
        await PublishAuditEventAsync(AuditAction.Create, newValue: item, oldValue: null);
        return item;
    }

    public virtual async Task<T> UpdateAsync(T item)
    {
        // Callers mutate the fetched entity in place, so `item` no longer holds the pre-update values: re-read the
        // persisted row (no-tracking, so it can't collide with `item` attached below). Only for [Auditable] types.
        var before = IsAuditable
            ? await DbContext.Set<T>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == item.Id)
            : null;

        item.DateModified = DateTime.UtcNow;
        item.ModifiedBy = CurrentUserId;
        item.ModifiedByIp = CurrentIp;

        // Entry(...).State = Modified (not Set<T>().Update(...)) so only the root is marked dirty and an
        // eagerly-loaded navigation graph isn't over-marked.
        DbContext.Entry(item).State = EntityState.Modified;
        await DbContext.SaveChangesAsync();

        await PublishAuditEventAsync(AuditAction.Update, newValue: item, oldValue: before);
        return item;
    }

    public virtual async Task RemoveAsync(T item)
    {
        DbContext.Set<T>().Remove(item);
        await DbContext.SaveChangesAsync();
        await PublishAuditEventAsync(AuditAction.Delete, newValue: null, oldValue: item);
    }

    /// <summary>Stamps an entity a DAO adds directly (a child row written alongside the root) the same way
    /// <see cref="InsertAsync"/> stamps the root, so no row reaches the database without its audit columns.</summary>
    protected void StampNew(BaseEntity entity)
    {
        var now = DateTime.UtcNow;
        entity.DateCreated = now;
        entity.DateModified = now;
        entity.CreatedBy = CurrentUserId;
        entity.CreatedByIp = CurrentIp;
        entity.ModifiedBy = entity.CreatedBy;
        entity.ModifiedByIp = entity.CreatedByIp;
    }

    /// <summary>Runs several writes as one unit. Goes through the execution strategy so it stays correct if a
    /// retrying strategy is ever enabled; the work may call this DAO's own insert/update methods.</summary>
    protected async Task InTransactionAsync(Func<Task> work)
    {
        var strategy = DbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await DbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);
            await work();
            await transaction.CommitAsync();
        });
    }

    /// <summary>No-op for a type without <see cref="AuditableAttribute"/>. Never throws.</summary>
    private async Task PublishAuditEventAsync(string action, T? newValue, T? oldValue)
    {
        if (!IsAuditable) return;

        var subject = newValue ?? oldValue!;
        await auditEventPublisher.PublishAsync(new EntityAuditEvent
        {
            EntityType = typeof(T).Name,
            RecordId = subject.Id.ToString(),
            DisplayName = AuditFieldExtractor.ResolveDisplayName(subject),
            Action = action,
            NewValues = newValue is null ? null : AuditFieldExtractor.Snapshot(newValue),
            OldValues = oldValue is null ? null : AuditFieldExtractor.Snapshot(oldValue),
            UserId = CurrentUserId,
            IpAddress = CurrentIp
        });
    }

    /// <summary>Null outside an authenticated request (sign-up, seeding): an absent stamp is fine, a failed save is not.</summary>
    private string? CurrentUserId => httpContextAccessor.HttpContext?.User.GetUserIdOrNull();

    private string? CurrentIp => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
