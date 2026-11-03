using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
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
                // LAST_INSERT_ID(expr) stores the new value per connection; the UPDATE holds the row lock until commit.
                await DbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE reference_counters SET `last_value` = LAST_INSERT_ID(`last_value` + 1) WHERE edition_id = {registration.EditionId}");
                var next = (await DbContext.Database.SqlQueryRaw<long>("SELECT CAST(LAST_INSERT_ID() AS SIGNED) AS `Value`").ToListAsync()).Single();
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
        catch (DbUpdateException e) when (e.InnerException is MySqlException { ErrorCode: MySqlErrorCode.DuplicateKeyEntry } m && m.Message.Contains("ix_registrations_one_active"))
        {
            DbContext.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task<PagerEntity<StaffRegistrationRow>> RetrieveStaffPagedAsync(Guid editionId, StaffRegistrationFilter filter, int page, int max)
    {
        var text = string.IsNullOrWhiteSpace(filter.Text) ? null : $"%{filter.Text.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
        var query = from r in Query()
                    join p in DbContext.Set<Person>().AsNoTracking() on r.PersonId equals p.Id
                    where r.EditionId == editionId
                        && (filter.State == null || r.State == filter.State)
                        && (filter.Mode == null || r.ParticipationMode == filter.Mode)
                        && (filter.AgeBand == null || r.AgeBand == filter.AgeBand)
                        && (text == null || EF.Functions.Like(p.GivenName + " " + p.FamilyName, text) || EF.Functions.Like(r.Reference, text))
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

    public async Task<StaffRegistrationStats> RetrieveStaffStatsAsync(Guid editionId, DateTime staleBefore)
    {
        // Cancelled, rejected and expired registrations are not "in" the event, so only live ones feed the mode and age-band counts.
        var all = Query().Where(r => r.EditionId == editionId);
        var live = all.Where(r => r.State != RegistrationState.Cancelled && r.State != RegistrationState.Rejected && r.State != RegistrationState.Expired);
        return new StaffRegistrationStats
        {
            ByState = await all.GroupBy(r => r.State).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N),
            ByMode = await live.GroupBy(r => r.ParticipationMode).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N),
            ByAgeBand = await live.Where(r => r.AgeBand != null).GroupBy(r => r.AgeBand!).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N),
            StaleDrafts = await all.CountAsync(r => r.State == RegistrationState.Draft && r.DateModified < staleBefore)
        };
    }

    public async Task<StaffRegistrationDetailRow?> RetrieveStaffDetailAsync(Guid registrationId)
    {
        var registration = await Query().FirstOrDefaultAsync(r => r.Id == registrationId);
        if (registration is null) return null;
        var person = await DbContext.Set<Person>().AsNoTracking().FirstAsync(p => p.Id == registration.PersonId);
        var actors = await (from a in DbContext.Set<RegistrationActor>().AsNoTracking()
                            join p in DbContext.Set<Person>().AsNoTracking() on a.PersonId equals p.Id
                            where a.RegistrationId == registrationId
                            orderby a.DateCreated
                            select new StaffActorRow { PersonId = p.Id, Name = p.GivenName + " " + p.FamilyName, Role = a.Role }).ToListAsync();
        var history = await (from h in DbContext.Set<RegistrationStatusHistory>().AsNoTracking()
                             join p in DbContext.Set<Person>().AsNoTracking() on h.ActorPersonId equals p.Id into actor
                             from p in actor.DefaultIfEmpty()
                             where h.RegistrationId == registrationId
                             orderby h.DateCreated, h.Id
                             select new StaffHistoryRow
                             {
                                 FromState = h.FromState, ToState = h.ToState, Reason = h.Reason, At = h.DateCreated,
                                 ActorName = p == null ? null : p.GivenName + " " + p.FamilyName
                             }).ToListAsync();
        return new StaffRegistrationDetailRow { Registration = registration, Person = person, Actors = actors, History = history };
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

public class CampDecisionDao(EmsDbContext dbContext, IHttpContextAccessor httpContextAccessor, IAuditEventPublisher auditEventPublisher)
    : BaseDao<EmsDbContext, CampDecision>(dbContext, httpContextAccessor, auditEventPublisher), ICampDecisionDao
{
    public Task<List<CampQueueRow>> RetrieveQueueAsync(Guid editionId)
    {
        // A List, not an array: on C# 14 an array's Contains binds to the span overload, which EF Core 9 cannot translate.
        var live = new List<RegistrationState> { RegistrationState.Submitted, RegistrationState.NeedsAction, RegistrationState.Confirmed, RegistrationState.Completed };
        var latest = DbContext.Set<CampDecision>().AsNoTracking();
        return (from r in DbContext.Set<Registration>().AsNoTracking()
                join p in DbContext.Set<Person>().AsNoTracking() on r.PersonId equals p.Id
                where r.EditionId == editionId && r.ParticipationMode == ParticipationMode.Camp && live.Contains(r.State)
                let d = latest.Where(x => x.RegistrationId == r.Id).OrderByDescending(x => x.DateCreated).ThenByDescending(x => x.Id).FirstOrDefault()
                orderby r.DateCreated
                select new CampQueueRow
                {
                    RegistrationId = r.Id, Reference = r.Reference, PersonId = p.Id, GivenName = p.GivenName, FamilyName = p.FamilyName,
                    AgeOnReferenceDate = r.AgeOnReferenceDate, State = r.State,
                    GuardianConsentGiven = DbContext.Set<ConsentRecord>().Where(c => c.RegistrationId == r.Id && c.Kind == ConsentKind.GuardianAuthorization)
                        .OrderByDescending(c => c.DateCreated).ThenByDescending(c => c.Id).Select(c => c.Accepted).FirstOrDefault(),
                    HasHealthFlags = DbContext.Set<HealthDisclosure>().Any(h => h.RegistrationId == r.Id && h.HasFlags),
                    Decision = d == null ? null : d.Decision, DecisionReason = d == null ? null : d.Reason, DecidedAt = d == null ? null : d.DateCreated
                }).ToListAsync();
    }
}
