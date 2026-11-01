using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using nlwc.ems.Core.Models;

namespace nlwc.ems.Data.EFCore.Implementations;

public class EditionDao(EmsDbContext dbContext, IHttpContextAccessor httpContextAccessor, IAuditEventPublisher auditEventPublisher)
    : BaseDao<EmsDbContext, EventEdition>(dbContext, httpContextAccessor, auditEventPublisher), IEditionDao
{
    // Every screen that shows an edition needs its venue, days and capability switches, so every read carries them.
    protected override IQueryable<EventEdition> Query() => base.Query()
        .Include(e => e.Venue).Include(e => e.Days.OrderBy(d => d.Day)).Include(e => e.Capabilities).AsSplitQuery();

    public Task<List<EventEdition>> RetrievePublicAsync()
        => Query().Where(e => e.Visibility == EditionVisibility.Public).OrderBy(e => e.StartsOn).ToListAsync();

    public Task<EventEdition?> RetrievePublicBySlugAsync(string slug)
        => Query().FirstOrDefaultAsync(e => e.Slug == slug && e.Visibility == EditionVisibility.Public);

    public Task<EventTemplate?> RetrieveTemplateByCodeAsync(string code)
        => DbContext.Set<EventTemplate>().AsNoTracking().FirstOrDefaultAsync(t => t.Code == code);

    public Task<bool> SlugOrCodeExistsAsync(string slug, string code)
        => DbContext.Set<EventEdition>().AnyAsync(e => e.Slug == slug || e.Code == code);

    public Task InsertWithChildrenAsync(EventEdition edition, Venue? venue, IReadOnlyList<EventDay> days,
        IReadOnlyList<EditionCapability> capabilities, ReferenceCounter counter)
        => InTransactionAsync(async () =>
        {
            if (venue is not null)
            {
                StampNew(venue);
                DbContext.Set<Venue>().Add(venue);
                await DbContext.SaveChangesAsync();
                edition.VenueId = venue.Id;
            }

            // The edition goes in through the base insert so it is stamped and audited like every other [Auditable] root.
            // Its children reference it, so they follow.
            await InsertAsync(edition);
            foreach (var child in days.Cast<BaseEntity>().Concat(capabilities).Append(counter)) StampNew(child);
            DbContext.Set<EventDay>().AddRange(days);
            DbContext.Set<EditionCapability>().AddRange(capabilities);
            DbContext.Set<ReferenceCounter>().Add(counter);
            await DbContext.SaveChangesAsync();
        });

    public async Task<bool> SetCapabilityAsync(Guid editionId, Capability capability, bool enabled)
    {
        var now = DateTime.UtcNow;
        var n = await DbContext.Set<EditionCapability>().Where(c => c.EditionId == editionId && c.Capability == capability)
            .ExecuteUpdateAsync(set => set.SetProperty(c => c.Enabled, enabled).SetProperty(c => c.DateModified, now));
        return n > 0;
    }

    public Task<bool> IsCapabilityEnabledAsync(Guid editionId, Capability capability)
        => DbContext.Set<EditionCapability>().AsNoTracking().AnyAsync(c => c.EditionId == editionId && c.Capability == capability && c.Enabled);
}

public class RatePlanDao(EmsDbContext dbContext, IHttpContextAccessor httpContextAccessor, IAuditEventPublisher auditEventPublisher)
    : BaseDao<EmsDbContext, RatePlan>(dbContext, httpContextAccessor, auditEventPublisher), IRatePlanDao
{
    public Task<List<RateKind>> RetrievePublishedKindsAsync(Guid editionId)
        => (from p in DbContext.Set<RatePlan>().AsNoTracking()
            join v in DbContext.Set<RatePlanVersion>().AsNoTracking() on p.Id equals v.PlanId
            join r in DbContext.Set<RateRule>().AsNoTracking() on v.Id equals r.VersionId
            where p.EditionId == editionId && v.State == RateVersionState.Published
            select r.Kind).ToListAsync();
}

public class RatePlanVersionDao(EmsDbContext dbContext, IHttpContextAccessor httpContextAccessor, IAuditEventPublisher auditEventPublisher)
    : BaseDao<EmsDbContext, RatePlanVersion>(dbContext, httpContextAccessor, auditEventPublisher), IRatePlanVersionDao
{
    public async Task<RatePlanVersion?> InsertDraftAsync(Guid editionId, Guid draftedBy, string? note, IReadOnlyList<RateRule> rules)
    {
        RatePlanVersion? created = null;
        await InTransactionAsync(async () =>
        {
            if (!await DbContext.Set<EventEdition>().AnyAsync(e => e.Id == editionId)) return;

            var plan = await DbContext.Set<RatePlan>().AsNoTracking().FirstOrDefaultAsync(p => p.EditionId == editionId);
            if (plan is null)
            {
                plan = new RatePlan { Id = Guid.NewGuid(), EditionId = editionId, Name = "Rates" };
                StampNew(plan);
                DbContext.Set<RatePlan>().Add(plan);
                await DbContext.SaveChangesAsync();
            }

            // Serialise version numbering per plan.
            await DbContext.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(hashtext({0}))", plan.Id.ToString());
            var next = (await DbContext.Set<RatePlanVersion>().Where(v => v.PlanId == plan.Id).MaxAsync(v => (int?)v.Version) ?? 0) + 1;

            var version = new RatePlanVersion { Id = Guid.NewGuid(), PlanId = plan.Id, Version = next, DraftedBy = draftedBy, Note = note };
            foreach (var rule in rules) { rule.Id = Guid.NewGuid(); rule.VersionId = version.Id; StampNew(rule); }
            DbContext.Set<RateRule>().AddRange(rules);
            // The version goes in through the base insert (stamped, audited); its rules are flushed in the same save.
            created = await InsertAsync(version);
        });
        return created;
    }

    public Task<RatePlanVersion?> RetrieveForEditionAsync(Guid editionId, Guid versionId)
        => (from v in Query().Include(v => v.Rules)
            join p in DbContext.Set<RatePlan>().AsNoTracking() on v.PlanId equals p.Id
            where v.Id == versionId && p.EditionId == editionId
            select v).FirstOrDefaultAsync();

    public Task PublishAsync(RatePlanVersion version)
        => InTransactionAsync(async () =>
        {
            var now = DateTime.UtcNow;
            // Retire first: at most one published version per plan is a unique index.
            await DbContext.Set<RatePlanVersion>().Where(v => v.PlanId == version.PlanId && v.State == RateVersionState.Published)
                .ExecuteUpdateAsync(set => set.SetProperty(v => v.State, RateVersionState.Retired).SetProperty(v => v.DateModified, now));
            version.State = RateVersionState.Published;
            version.PublishedAt = now;
            await UpdateAsync(version);
        });
}
