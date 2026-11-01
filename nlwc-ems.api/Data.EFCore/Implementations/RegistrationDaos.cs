using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using nlwc.ems.Core;
using nlwc.ems.Core.Models;

namespace nlwc.ems.Data.EFCore.Implementations;

public class RegistrationDao(EmsDbContext dbContext, IHttpContextAccessor httpContextAccessor, IAuditEventPublisher auditEventPublisher)
    : BaseDao<EmsDbContext, Registration>(dbContext, httpContextAccessor, auditEventPublisher), IRegistrationDao
{
    public Task<List<Registration>> RetrieveForActorAsync(Guid personId)
        => Query().Where(r => DbContext.Set<RegistrationActor>().Any(a => a.RegistrationId == r.Id && a.PersonId == personId))
            .OrderByDescending(r => r.DateCreated).ThenByDescending(r => r.Id).ToListAsync();

    public Task<Registration?> RetrieveForActorAsync(Guid personId, Guid registrationId)
        => Query().FirstOrDefaultAsync(r => r.Id == registrationId
            && DbContext.Set<RegistrationActor>().Any(a => a.RegistrationId == r.Id && a.PersonId == personId));

    public Task<Registration?> RetrieveActiveAsync(Guid personId, Guid editionId)
        => Query().FirstOrDefaultAsync(r => r.PersonId == personId && r.EditionId == editionId
            && r.State != RegistrationState.Cancelled && r.State != RegistrationState.Rejected && r.State != RegistrationState.Expired);

    public async Task<bool> InsertNewAsync(Registration registration, string editionCode, IReadOnlyList<RegistrationActor> actors)
    {
        try
        {
            await InTransactionAsync(async () =>
            {
                // One statement increments and reads, so two concurrent registrations can never share a number.
                var next = (await DbContext.Database
                    .SqlQuery<long>($"UPDATE reference_counters SET last_value = last_value + 1 WHERE edition_id = {registration.EditionId} RETURNING last_value AS \"Value\"")
                    .ToListAsync()).Single();
                registration.Reference = RegistrationReference.Format(editionCode, next);

                await InsertAsync(registration);
                foreach (var actor in actors) { StampNew(actor); DbContext.Set<RegistrationActor>().Add(actor); }
                var history = new RegistrationStatusHistory
                {
                    Id = Guid.NewGuid(), RegistrationId = registration.Id, FromState = null, ToState = registration.State,
                    ActorPersonId = actors.FirstOrDefault(a => a.Role == RegistrationRole.Registrant)?.PersonId
                };
                StampNew(history);
                DbContext.Set<RegistrationStatusHistory>().Add(history);
                await DbContext.SaveChangesAsync();
            });
            return true;
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { ConstraintName: "ix_registrations_one_active" })
        {
            DbContext.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task<PagerEntity<StaffRegistrationRow>> RetrieveStaffPagedAsync(Guid editionId, RegistrationState? state, int page, int max)
    {
        var query = from r in Query()
                    join p in DbContext.Set<Person>().AsNoTracking() on r.PersonId equals p.Id
                    where r.EditionId == editionId && (state == null || r.State == state)
                    select new StaffRegistrationRow
                    {
                        Id = r.Id, Reference = r.Reference, PersonId = p.Id, GivenName = p.GivenName, FamilyName = p.FamilyName, State = r.State,
                        ParticipationMode = r.ParticipationMode, AgeBand = r.AgeBand, DateCreated = r.DateCreated,
                        HasHealthFlags = DbContext.Set<HealthDisclosure>().Any(h => h.RegistrationId == r.Id && h.HasFlags)
                    };
        var count = await query.CountAsync();
        var data = await query.OrderByDescending(x => x.DateCreated).ThenByDescending(x => x.Id).Skip(page * max).Take(max).ToListAsync();
        return new PagerEntity<StaffRegistrationRow> { Page = page, Max = max, Count = count, Data = data };
    }

    public Task TransitionAsync(Registration registration, RegistrationState from, string? reason, Guid actorPersonId)
        => InTransactionAsync(async () =>
        {
            await UpdateAsync(registration);
            var history = new RegistrationStatusHistory
            {
                Id = Guid.NewGuid(), RegistrationId = registration.Id, FromState = from, ToState = registration.State,
                ActorPersonId = actorPersonId, Reason = reason
            };
            StampNew(history);
            DbContext.Set<RegistrationStatusHistory>().Add(history);
            await DbContext.SaveChangesAsync();
        });
}

public class ConsentRecordDao(EmsDbContext dbContext, IHttpContextAccessor httpContextAccessor, IAuditEventPublisher auditEventPublisher)
    : BaseDao<EmsDbContext, ConsentRecord>(dbContext, httpContextAccessor, auditEventPublisher), IConsentRecordDao
{
    public async Task<List<ConsentRecord>> RetrieveCurrentAsync(Guid registrationId)
    {
        var all = await Query().Where(c => c.RegistrationId == registrationId).OrderBy(c => c.DateCreated).ThenBy(c => c.Id).ToListAsync();
        return all.GroupBy(c => c.Kind).Select(g => g.Last()).ToList();
    }

    public Task<PolicyVersion?> RetrieveApprovedPolicyAsync(string documentId)
        => DbContext.Set<PolicyVersion>().AsNoTracking()
            .Where(p => p.DocumentId == documentId && p.ApprovedAt != null).OrderByDescending(p => p.Version).FirstOrDefaultAsync();
}

public class HealthDisclosureDao(EmsDbContext dbContext, IHttpContextAccessor httpContextAccessor, IAuditEventPublisher auditEventPublisher)
    : BaseDao<EmsDbContext, HealthDisclosure>(dbContext, httpContextAccessor, auditEventPublisher), IHealthDisclosureDao
{
    public Task<HealthDisclosure?> RetrieveForRegistrationAsync(Guid registrationId)
        => Query().FirstOrDefaultAsync(h => h.RegistrationId == registrationId);

    public Task ReplaceAsync(HealthDisclosure disclosure)
        => InTransactionAsync(async () =>
        {
            await DbContext.Set<HealthDisclosure>().Where(h => h.RegistrationId == disclosure.RegistrationId).ExecuteDeleteAsync();
            await InsertAsync(disclosure);
        });
}

public class AccessLogDao(EmsDbContext dbContext, IHttpContextAccessor httpContextAccessor, IAuditEventPublisher auditEventPublisher)
    : BaseDao<EmsDbContext, AccessLog>(dbContext, httpContextAccessor, auditEventPublisher), IAccessLogDao;
