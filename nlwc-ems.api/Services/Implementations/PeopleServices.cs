using nlwc.ems.Core.Models;
using nlwc.ems.Dtos;

namespace nlwc.ems.Services.Implementations;

public class ProfileService(IPersonDao personDao, IAccountDao accountDao, IEmergencyContactDao contactDao, IFieldEncryptor encryptor,
    IAuditEventPublisher auditPublisher) : BaseService<Person>(personDao), IProfileService
{
    public async Task<ProfileDto> GetAsync(Guid accountId, Guid personId, bool emailVerified)
    {
        var person = await personDao.RetrieveByIdAsync(personId) ?? throw new NotFoundException("Profile not found.");
        var account = await accountDao.RetrieveByIdAsync(accountId) ?? throw new NotFoundException("Account not found.");
        var contact = await contactDao.RetrieveForPersonAsync(personId);
        return new ProfileDto
        {
            PersonId = person.Id, GivenName = person.GivenName, FamilyName = person.FamilyName, MiddleName = person.MiddleName,
            DateOfBirth = person.DateOfBirth, Gender = person.Gender, MaritalStatus = person.MaritalStatus, Country = person.Country, City = person.City,
            Email = account.Email, EmailVerified = emailVerified, NeedsDateOfBirth = person.DateOfBirth is null,
            EmergencyContact = contact is null ? null : EmergencyContactService.ToDto(contact, encryptor)
        };
    }

    public async Task UpdateAsync(Guid accountId, Guid personId, UpdateProfileRequest request)
    {
        var person = await personDao.RetrieveByIdAsync(personId) ?? throw new NotFoundException("Profile not found.");

        var firstDateOfBirth = false;
        if (request.DateOfBirth is { } dob)
        {
            PersonRules.ValidateDateOfBirth(dob);
            // Set once. A later change affects age bands, prices and eligibility, so it is reviewed rather than self-served.
            if (person.DateOfBirth is { } existing && existing != dob)
                throw new AppException("Your date of birth can't be changed here. Contact the church office.", isConflict: true, code: "dob_locked");
            firstDateOfBirth = person.DateOfBirth is null;
            person.DateOfBirth = dob;
        }

        person.GivenName = request.GivenName.Trim();
        person.FamilyName = request.FamilyName.Trim();
        person.MiddleName = request.MiddleName?.Trim();
        person.Gender = request.Gender;
        person.MaritalStatus = request.MaritalStatus;
        person.Country = request.Country;
        person.City = request.City;
        await UpdateOrThrowAsync(person);

        if (firstDateOfBirth)
            await auditPublisher.PublishAsync(new EntityAuditEvent
            {
                EntityType = nameof(Person), RecordId = personId.ToString(), Action = "person.dob_set", UserId = accountId.ToString()
            });
    }
}

public class DependantService(IGuardianAuthorizationDao guardianDao, IAuditEventPublisher auditPublisher)
    : BaseService<GuardianAuthorization>(guardianDao), IDependantService
{
    public async Task<IReadOnlyList<DependantDto>> ListAsync(Guid guardianPersonId)
        => (await guardianDao.RetrieveDependantsAsync(guardianPersonId))
            .Select(d => new DependantDto
            {
                PersonId = d.PersonId, GivenName = d.GivenName, FamilyName = d.FamilyName, DateOfBirth = d.DateOfBirth, Gender = d.Gender, Relationship = d.Relationship
            }).ToList();

    public async Task<Guid> AddAsync(Guid guardianPersonId, AddDependantRequest request)
    {
        var dob = PersonRules.ValidateDateOfBirth(request.DateOfBirth!.Value);
        if (PersonRules.AgeOn(dob, PersonRules.TodayLagos()) > PersonRules.MaxDependantAge)
            throw new ValidationException(["Only people under 18 can be added as a child. Adults manage their own profile."], "too_old_for_dependant");

        var dependant = new Person
        {
            Id = Guid.NewGuid(), GivenName = request.GivenName.Trim(), FamilyName = request.FamilyName.Trim(), DateOfBirth = dob, Gender = request.Gender
        };
        var authorization = new GuardianAuthorization
        {
            Id = Guid.NewGuid(), GuardianPersonId = guardianPersonId, DependantPersonId = dependant.Id, DeclaredRelationship = request.Relationship!.Value.ToString()
        };
        var relationship = new PersonRelationship { Id = Guid.NewGuid(), FromPersonId = dependant.Id, ToPersonId = guardianPersonId, Kind = RelationshipKind.DependantOf };
        await guardianDao.InsertDependantAsync(dependant, authorization, relationship);
        return dependant.Id;
    }

    public async Task RemoveAsync(Guid guardianPersonId, Guid dependantPersonId, string reason)
    {
        var authorization = await guardianDao.RetrieveActiveAsync(guardianPersonId, dependantPersonId) ?? throw new NotFoundException("Not found.");
        authorization.RevokedAt = DateTime.UtcNow;
        await UpdateOrThrowAsync(authorization);

        await auditPublisher.PublishAsync(new EntityAuditEvent
        {
            EntityType = nameof(GuardianAuthorization), RecordId = authorization.Id.ToString(), Action = "dependant.removed", Reason = reason,
            Detail = new { guardianPersonId, dependantPersonId }
        });
    }
}

public class CollectorService(IAuthorizedCollectorDao collectorDao, IGuardianAuthorizationDao guardianDao, IFieldEncryptor encryptor)
    : BaseService<AuthorizedCollector>(collectorDao), ICollectorService
{
    public async Task<IReadOnlyList<CollectorDto>> ListAsync(Guid guardianPersonId, Guid childPersonId)
    {
        await PersonRules.RequireGuardianAsync(guardianDao, guardianPersonId, childPersonId);
        return (await collectorDao.RetrieveForChildAsync(childPersonId))
            .Select(c => new CollectorDto
            {
                CollectorId = c.Id, FullName = c.FullName, Relationship = c.Relationship, Phone = encryptor.DecryptString(c.PhoneEncrypted), ValidDays = c.ValidDays
            }).ToList();
    }

    public async Task<Guid> AddAsync(Guid guardianPersonId, Guid childPersonId, AddCollectorRequest request)
    {
        await PersonRules.RequireGuardianAsync(guardianDao, guardianPersonId, childPersonId);
        var phone = PersonRules.NormalizePhone(request.Phone);
        if (request.ValidDays is { Count: 0 }) throw new ValidationException(["Choose at least one day, or leave days empty for every day."], "validation_failed");

        var collector = await InsertOrThrowAsync(new AuthorizedCollector
        {
            Id = Guid.NewGuid(), ChildPersonId = childPersonId, FullName = request.FullName.Trim(), Relationship = request.Relationship.Trim(),
            PhoneEncrypted = encryptor.EncryptString(phone), ValidDays = request.ValidDays
        });
        return collector.Id;
    }

    public async Task RemoveAsync(Guid guardianPersonId, Guid childPersonId, Guid collectorId)
    {
        await PersonRules.RequireGuardianAsync(guardianDao, guardianPersonId, childPersonId);
        var collector = await collectorDao.RetrieveByIdAsync(collectorId);
        if (collector is null || collector.ChildPersonId != childPersonId || collector.RemovedAt is not null) throw new NotFoundException("Not found.");

        // The row is kept: who was authorised on which day is part of the custody record.
        collector.RemovedAt = DateTime.UtcNow;
        await UpdateOrThrowAsync(collector);
    }
}

public class EmergencyContactService(IEmergencyContactDao contactDao, IGuardianAuthorizationDao guardianDao, IFieldEncryptor encryptor)
    : BaseService<EmergencyContact>(contactDao), IEmergencyContactService
{
    public static ContactDto ToDto(EmergencyContact c, IFieldEncryptor encryptor) =>
        new() { FullName = c.FullName, Relationship = c.Relationship, Phone = encryptor.DecryptString(c.PhoneEncrypted) };

    public async Task SetAsync(Guid callerPersonId, Guid subjectPersonId, EmergencyContactRequest request)
    {
        if (subjectPersonId != callerPersonId) await PersonRules.RequireGuardianAsync(guardianDao, callerPersonId, subjectPersonId);
        var phone = PersonRules.NormalizePhone(request.Phone);
        await contactDao.ReplaceAsync(new EmergencyContact
        {
            Id = Guid.NewGuid(), PersonId = subjectPersonId, FullName = request.FullName.Trim(), Relationship = request.Relationship.Trim(),
            PhoneEncrypted = encryptor.EncryptString(phone)
        });
    }

    public async Task<ContactDto?> GetAsync(Guid callerPersonId, Guid subjectPersonId)
    {
        if (subjectPersonId != callerPersonId) await PersonRules.RequireGuardianAsync(guardianDao, callerPersonId, subjectPersonId);
        var contact = await contactDao.RetrieveForPersonAsync(subjectPersonId);
        return contact is null ? null : ToDto(contact, encryptor);
    }
}
