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
