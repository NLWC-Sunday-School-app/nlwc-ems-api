using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using nlwc.ems.Core.Models;

namespace nlwc.ems.Data.EFCore.Implementations;

public class GuardianAuthorizationDao(EmsDbContext dbContext, IHttpContextAccessor httpContextAccessor, IAuditEventPublisher auditEventPublisher)
    : BaseDao<EmsDbContext, GuardianAuthorization>(dbContext, httpContextAccessor, auditEventPublisher), IGuardianAuthorizationDao
{
    public Task<GuardianAuthorization?> RetrieveActiveAsync(Guid guardianPersonId, Guid dependantPersonId)
        => Query().FirstOrDefaultAsync(g => g.GuardianPersonId == guardianPersonId && g.DependantPersonId == dependantPersonId && g.RevokedAt == null);

    public Task<List<DependantRow>> RetrieveDependantsAsync(Guid guardianPersonId)
        => (from g in Query()
            join p in DbContext.Set<Person>().AsNoTracking() on g.DependantPersonId equals p.Id
            where g.GuardianPersonId == guardianPersonId && g.RevokedAt == null
            orderby p.DateOfBirth
            select new DependantRow
            {
                PersonId = p.Id, GivenName = p.GivenName, FamilyName = p.FamilyName,
                DateOfBirth = p.DateOfBirth, Gender = p.Gender, Relationship = g.DeclaredRelationship
            }).ToListAsync();

    public Task InsertDependantAsync(Person dependant, GuardianAuthorization authorization, PersonRelationship relationship)
        => InTransactionAsync(async () =>
        {
            StampNew(dependant);
            DbContext.Set<Person>().Add(dependant);
            DbContext.Set<PersonRelationship>().Add(Stamped(relationship));
            await DbContext.SaveChangesAsync();
            await InsertAsync(authorization);   // audited as a Create by BaseDao
        });

    private PersonRelationship Stamped(PersonRelationship r)
    {
        StampNew(r);
        return r;
    }
}

public class AuthorizedCollectorDao(EmsDbContext dbContext, IHttpContextAccessor httpContextAccessor, IAuditEventPublisher auditEventPublisher)
    : BaseDao<EmsDbContext, AuthorizedCollector>(dbContext, httpContextAccessor, auditEventPublisher), IAuthorizedCollectorDao
{
    public Task<List<AuthorizedCollector>> RetrieveForChildAsync(Guid childPersonId)
        => Query().Where(c => c.ChildPersonId == childPersonId && c.RemovedAt == null).OrderBy(c => c.DateCreated).ToListAsync();
}

public class EmergencyContactDao(EmsDbContext dbContext, IHttpContextAccessor httpContextAccessor, IAuditEventPublisher auditEventPublisher)
    : BaseDao<EmsDbContext, EmergencyContact>(dbContext, httpContextAccessor, auditEventPublisher), IEmergencyContactDao
{
    public Task<EmergencyContact?> RetrieveForPersonAsync(Guid personId) => Query().FirstOrDefaultAsync(c => c.PersonId == personId);

    public Task ReplaceAsync(EmergencyContact contact)
        => InTransactionAsync(async () =>
        {
            await DbContext.Set<EmergencyContact>().Where(c => c.PersonId == contact.PersonId).ExecuteDeleteAsync();
            await InsertAsync(contact);
        });
}

public class StaffPeopleDao(EmsDbContext dbContext, IHttpContextAccessor httpContextAccessor, IAuditEventPublisher auditEventPublisher)
    : BaseDao<EmsDbContext, PersonRelationship>(dbContext, httpContextAccessor, auditEventPublisher), IStaffPeopleDao
{
    private IQueryable<StaffPersonRow> Rows() => DbContext.Set<Person>().AsNoTracking().Select(p => new StaffPersonRow
    {
        Id = p.Id, GivenName = p.GivenName, FamilyName = p.FamilyName, DateOfBirth = p.DateOfBirth, City = p.City, Country = p.Country,
        Registrations = DbContext.Set<Registration>().Count(r => r.PersonId == p.Id)
    });

    public async Task<PagerEntity<StaffPersonRow>> SearchAsync(string? text, int page, int max)
    {
        var query = Rows();
        if (!string.IsNullOrWhiteSpace(text))
        {
            var pattern = $"%{text.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
            query = query.Where(p => EF.Functions.ILike(p.GivenName + " " + p.FamilyName, pattern));
        }
        var count = await query.CountAsync();
        var data = await query.OrderBy(p => p.FamilyName).ThenBy(p => p.GivenName).ThenBy(p => p.Id).Skip(page * max).Take(max).ToListAsync();
        return new PagerEntity<StaffPersonRow> { Page = page, Max = max, Count = count, Data = data };
    }

    public async Task<StaffPersonDetailRow?> RetrieveDetailAsync(Guid personId)
    {
        var person = await DbContext.Set<Person>().AsNoTracking().FirstOrDefaultAsync(p => p.Id == personId);
        if (person is null) return null;
        var people = DbContext.Set<Person>().AsNoTracking();
        var rels = DbContext.Set<PersonRelationship>().AsNoTracking();
        // From == this person: a spouse, or the guardian this person depends on. To == this person with DependantOf: their dependants.
        var outgoing = await (from r in rels join o in people on r.ToPersonId equals o.Id where r.FromPersonId == personId
                              select new StaffPersonRelationRow { Relation = r.Kind == RelationshipKind.Spouse ? "Spouse" : "Guardian", PersonId = o.Id, Name = o.GivenName + " " + o.FamilyName }).ToListAsync();
        var incoming = await (from r in rels join o in people on r.FromPersonId equals o.Id where r.ToPersonId == personId && r.Kind == RelationshipKind.DependantOf
                              select new StaffPersonRelationRow { Relation = "Dependant", PersonId = o.Id, Name = o.GivenName + " " + o.FamilyName }).ToListAsync();
        var registrations = await (from r in DbContext.Set<Registration>().AsNoTracking()
                                   join e in DbContext.Set<EventEdition>().AsNoTracking() on r.EditionId equals e.Id
                                   where r.PersonId == personId
                                   orderby r.DateCreated descending
                                   select new StaffPersonRegistrationRow { Id = r.Id, Reference = r.Reference, EditionName = e.Name, State = r.State, Mode = r.ParticipationMode }).ToListAsync();
        return new StaffPersonDetailRow { Person = person, Relations = [.. outgoing, .. incoming], Registrations = registrations };
    }

    public async Task<List<DuplicatePairRow>> RetrieveDuplicatesAsync(int max)
    {
        var people = DbContext.Set<Person>().AsNoTracking();
        var dismissed = DbContext.Set<DuplicateDismissal>().AsNoTracking();
        var pairs = await (from a in people
                           join b in people on new { a.DateOfBirth, Family = a.FamilyName.ToLower() } equals new { b.DateOfBirth, Family = b.FamilyName.ToLower() }
                           where a.Id.CompareTo(b.Id) < 0 && a.DateOfBirth != null
                              && (a.GivenName.ToLower() == b.GivenName.ToLower() || EF.Functions.ILike(a.GivenName, b.GivenName + "%") || EF.Functions.ILike(b.GivenName, a.GivenName + "%"))
                              && !dismissed.Any(d => d.PersonAId == a.Id && d.PersonBId == b.Id)
                           orderby a.FamilyName, a.GivenName
                           select new { A = a.Id, B = b.Id }).Take(max).ToListAsync();
        if (pairs.Count == 0) return [];
        var ids = pairs.SelectMany(p => new[] { p.A, p.B }).Distinct().ToList();
        var rows = await Rows().Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
        return pairs.Select(p => new DuplicatePairRow { A = rows[p.A], B = rows[p.B] }).ToList();
    }

    public async Task InsertDismissalAsync(DuplicateDismissal dismissal)
    {
        StampNew(dismissal);
        DbContext.Set<DuplicateDismissal>().Add(dismissal);
        await DbContext.SaveChangesAsync();
    }

    public async Task<Guid?> RetrieveSpouseIdAsync(Guid personId)
        => await Query().Where(r => r.FromPersonId == personId && r.Kind == RelationshipKind.Spouse).Select(r => (Guid?)r.ToPersonId).FirstOrDefaultAsync();

    public Task PairSpousesAsync(Guid a, Guid b)
        => InTransactionAsync(async () =>
        {
            foreach (var (from, to) in new[] { (a, b), (b, a) })
            {
                var row = new PersonRelationship { Id = Guid.NewGuid(), FromPersonId = from, ToPersonId = to, Kind = RelationshipKind.Spouse };
                StampNew(row);
                DbContext.Set<PersonRelationship>().Add(row);
            }
            await DbContext.SaveChangesAsync();
        });

    public Task UnpairSpousesAsync(Guid a, Guid b)
        => InTransactionAsync(async () =>
        {
            var rows = await DbContext.Set<PersonRelationship>()
                .Where(r => r.Kind == RelationshipKind.Spouse && ((r.FromPersonId == a && r.ToPersonId == b) || (r.FromPersonId == b && r.ToPersonId == a))).ToListAsync();
            DbContext.Set<PersonRelationship>().RemoveRange(rows);
            await DbContext.SaveChangesAsync();
        });
}
