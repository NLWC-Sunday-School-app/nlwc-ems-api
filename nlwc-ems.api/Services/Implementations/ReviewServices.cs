using Microsoft.Extensions.Options;
using nlwc.ems.Core.Models;
using nlwc.ems.Dtos;
using nlwc.ems.Services.Messaging;
using nlwc.ems.Services.Options;

namespace nlwc.ems.Services.Implementations;

public class StaffPeopleService(IStaffPeopleDao peopleDao, IPersonDao personDao, IAuditEventPublisher auditPublisher)
    : BaseService<PersonRelationship>(peopleDao), IStaffPeopleService
{
    private const int MaxPairs = 100;

    private static StaffPersonSummaryDto ToDto(StaffPersonRow r) => new()
    {
        Id = r.Id, GivenName = r.GivenName, FamilyName = r.FamilyName, DateOfBirth = r.DateOfBirth, City = r.City, Country = r.Country, Registrations = r.Registrations
    };

    public async Task<PagerEntity<StaffPersonSummaryDto>> SearchAsync(string? text, int page, int max)
    {
        var rows = await peopleDao.SearchAsync(text, page, max);
        return new PagerEntity<StaffPersonSummaryDto> { Page = rows.Page, Max = rows.Max, Count = rows.Count, Data = rows.Data.Select(ToDto).ToList() };
    }

    public async Task<StaffPersonDetailDto> GetDetailAsync(Guid personId)
    {
        var d = await peopleDao.RetrieveDetailAsync(personId) ?? throw new NotFoundException("Not found.");
        return new StaffPersonDetailDto
        {
            Person = new StaffPersonSummaryDto
            {
                Id = d.Person.Id, GivenName = d.Person.GivenName, FamilyName = d.Person.FamilyName, DateOfBirth = d.Person.DateOfBirth,
                City = d.Person.City, Country = d.Person.Country, Registrations = d.Registrations.Count
            },
            Gender = d.Person.Gender,
            Relations = d.Relations.Select(r => new StaffPersonRelationDto { Relation = r.Relation, PersonId = r.PersonId, Name = r.Name }).ToList(),
            Registrations = d.Registrations.Select(r => new StaffPersonRegistrationDto { Id = r.Id, Reference = r.Reference, EditionName = r.EditionName, State = r.State, Mode = r.Mode }).ToList()
        };
    }

    public async Task<IReadOnlyList<DuplicatePairDto>> GetDuplicatesAsync()
        => (await peopleDao.RetrieveDuplicatesAsync(MaxPairs)).Select(p => new DuplicatePairDto { A = ToDto(p.A), B = ToDto(p.B) }).ToList();

    public async Task DismissDuplicateAsync(Guid staffPersonId, Guid personAId, Guid personBId, string reason)
    {
        if (personAId == personBId) throw new ValidationException(["Pick two different people."], "same_person");
        await RequirePersonAsync(personAId);
        await RequirePersonAsync(personBId);
        var (a, b) = personAId.CompareTo(personBId) < 0 ? (personAId, personBId) : (personBId, personAId);   // one row per pair
        try
        {
            await peopleDao.InsertDismissalAsync(new DuplicateDismissal { Id = Guid.NewGuid(), PersonAId = a, PersonBId = b, Reason = reason.Trim(), DismissedBy = staffPersonId });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            throw new AppException("This pair was already reviewed.", isConflict: true, code: "already_reviewed");
        }
    }

    public async Task PairSpousesAsync(Guid staffPersonId, Guid personId, Guid spouseId, string reason)
    {
        if (personId == spouseId) throw new ValidationException(["A person can't be their own spouse."], "same_person");
        foreach (var id in new[] { personId, spouseId })
        {
            var person = await RequirePersonAsync(id);
            if (person.DateOfBirth is not { } dob || PersonRules.AgeOn(dob, DateOnly.FromDateTime(DateTime.UtcNow)) < RegistrationRules.AdultAge)
                throw new ValidationException([$"{person.GivenName} {person.FamilyName} must be an adult with a date of birth on file."], "spouse_must_be_adult");
            if (await peopleDao.RetrieveSpouseIdAsync(id) is not null)
                throw new AppException($"{person.GivenName} {person.FamilyName} already has a spouse on record. Remove that link first.", isConflict: true, code: "already_paired");
        }
        await peopleDao.PairSpousesAsync(personId, spouseId);
        await auditPublisher.PublishAsync(new EntityAuditEvent
        {
            EntityType = nameof(PersonRelationship), RecordId = personId.ToString(), DisplayName = "Spouses paired", Action = "spouse.paired",
            Reason = reason.Trim(), Detail = new { personId, spouseId }
        });
    }

    public async Task UnpairSpousesAsync(Guid staffPersonId, Guid personId, string reason)
    {
        await RequirePersonAsync(personId);
        var spouseId = await peopleDao.RetrieveSpouseIdAsync(personId) ?? throw new NotFoundException("This person has no spouse on record.");
        await peopleDao.UnpairSpousesAsync(personId, spouseId);
        await auditPublisher.PublishAsync(new EntityAuditEvent
        {
            EntityType = nameof(PersonRelationship), RecordId = personId.ToString(), DisplayName = "Spouses unpaired", Action = "spouse.unpaired",
            Reason = reason.Trim(), Detail = new { personId, spouseId }
        });
    }

    private async Task<Person> RequirePersonAsync(Guid id) => await personDao.RetrieveByIdAsync(id) ?? throw new NotFoundException("Person not found.");
}

public class CampReviewService(ICampDecisionDao decisionDao, IRegistrationDao registrationDao, IEditionDao editionDao, IAuditEventPublisher auditPublisher)
    : BaseService<CampDecision>(decisionDao), ICampReviewService
{
    public async Task<CampQueueDto> GetQueueAsync(Guid editionId, bool includeHealthFlags)
    {
        var edition = await editionDao.RetrieveByIdAsync(editionId) ?? throw new NotFoundException("Edition not found.");
        var rows = await decisionDao.RetrieveQueueAsync(editionId);
        return new CampQueueDto
        {
            Capacity = edition.CampCapacity,
            Approved = rows.Count(r => r.Decision == CampDecisionKind.Approved),
            Rows = rows.Select(r => new CampQueueItemDto
            {
                RegistrationId = r.RegistrationId, Reference = r.Reference, PersonId = r.PersonId, GivenName = r.GivenName, FamilyName = r.FamilyName,
                Age = r.AgeOnReferenceDate, State = r.State, GuardianConsentGiven = r.GuardianConsentGiven,
                HasHealthFlags = includeHealthFlags ? r.HasHealthFlags : null,
                Decision = r.Decision, DecisionReason = r.DecisionReason, DecidedAt = r.DecidedAt
            }).ToList()
        };
    }

    public async Task DecideAsync(Guid staffPersonId, Guid registrationId, CampDecisionKind decision, string reason)
    {
        var registration = await registrationDao.RetrieveByIdAsync(registrationId) ?? throw new NotFoundException("Not found.");
        var queue = await decisionDao.RetrieveQueueAsync(registration.EditionId);
        var row = queue.FirstOrDefault(r => r.RegistrationId == registrationId)
            ?? throw new AppException("Only a submitted camp request can be reviewed.", isConflict: true, code: "camp_not_reviewable");

        if (decision == CampDecisionKind.Approved)
        {
            var edition = await editionDao.RetrieveByIdAsync(registration.EditionId) ?? throw new NotFoundException("Edition not found.");
            if (edition.CampCapacity is not { } capacity)
                throw new AppException("Set the camp capacity before approving anyone.", isConflict: true, code: "camp_capacity_not_set");
            // Someone already approved keeps their place when their own decision is re-saved.
            var others = queue.Count(r => r.Decision == CampDecisionKind.Approved && r.RegistrationId != registrationId);
            if (others >= capacity)
                throw new AppException("Camp is full. Waitlist this person instead.", isConflict: true, code: "camp_full");
        }

        await decisionDao.InsertAsync(new CampDecision { Id = Guid.NewGuid(), RegistrationId = registrationId, Decision = decision, Reason = reason.Trim(), DecidedBy = staffPersonId });
        await auditPublisher.PublishAsync(new EntityAuditEvent
        {
            EntityType = nameof(Registration), RecordId = registrationId.ToString(), DisplayName = row.Reference, Action = $"camp.{decision.ToString().ToLowerInvariant()}",
            Reason = reason.Trim(), EditionId = registration.EditionId
        });
    }
}

public class RegistrationNotifier(IRegistrationDao registrationDao, IPersonDao personDao, IAccountDao accountDao, IEditionDao editionDao, IRatePlanService ratePlanService,
    ICampDecisionDao campDecisionDao, IEmailService email, IOptions<MailgunOptions> options, ILogger<RegistrationNotifier> logger) : IRegistrationNotifier
{
    private static readonly RegistrationState[] Live = [RegistrationState.Submitted, RegistrationState.NeedsAction, RegistrationState.Confirmed, RegistrationState.Completed];

    public async Task NotifyRegisteredAsync(Guid guardianPersonId, Guid registrationId)
    {
        try
        {
            var registration = await registrationDao.RetrieveByIdAsync(registrationId);
            if (registration is null || registration.PersonId == guardianPersonId) return;   // an adult registering themself is not this email

            // Only Bible Bootcamp has a confirmation template so far; other programmes get theirs when it is designed.
            if (await editionDao.RetrieveTemplateCodeAsync(registration.EditionId) != "BB")
            {
                logger.LogInformation("No confirmation email template for the programme of registration {Registration}; nothing sent.", registration.Reference);
                return;
            }
            var account = await accountDao.RetrieveByPersonIdAsync(guardianPersonId);
            var guardian = await personDao.RetrieveByIdAsync(guardianPersonId);
            var edition = await editionDao.RetrieveByIdAsync(registration.EditionId);
            if (account is null || guardian is null || edition is null) return;

            // Every child this guardian has registered for the event, so a second child's email lists both.
            var camp = (await campDecisionDao.RetrieveQueueAsync(edition.Id)).ToDictionary(r => r.RegistrationId);
            var children = new List<ConfirmationChild>();
            foreach (var r in (await registrationDao.RetrieveForActorAsync(guardianPersonId))
                         .Where(r => r.EditionId == edition.Id && r.PersonId != guardianPersonId && Live.Contains(r.State)).OrderBy(r => r.DateCreated))
            {
                var child = await personDao.RetrieveByIdAsync(r.PersonId);
                if (child is null) continue;
                var (label, pending) = Status(r, camp.GetValueOrDefault(r.Id));
                children.Add(new ConfirmationChild(child.GivenName, $"{child.GivenName} {child.FamilyName}", r.AgeOnReferenceDate, r.Reference, r.ParticipationMode, label, pending));
            }
            if (children.Count == 0) return;

            var venue = edition.Venue;
            var html = RegistrationConfirmationEmail.Render(guardian.GivenName,
                new ConfirmationEdition(edition.Name, edition.StartsOn, edition.EndsOn, venue?.Name, venue?.Address,
                    await ratePlanService.GetPriceStateAsync(edition.Id) == PriceState.Free, edition.PickupWindowStart, edition.PickupWindowEnd),
                children, options.Value.WebBaseUrl);
            await email.SendEmailAsync(new Mailer(account.Email, guardian.GivenName), null, RegistrationConfirmationEmail.Subject(edition.Name), html);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "The registration confirmation email for {Registration} could not be sent.", registrationId);
        }
    }

    private static (string Label, bool Pending) Status(Registration r, CampQueueRow? camp)
    {
        if (r.ParticipationMode == ParticipationMode.Camp)
            return camp?.Decision switch
            {
                CampDecisionKind.Approved => ("Camp place confirmed", false),
                CampDecisionKind.Waitlisted => ("On the camp waitlist", true),
                CampDecisionKind.Declined => ("Camp is not available", true),
                _ => ("Pending our team's review", true)
            };
        return r.State switch
        {
            RegistrationState.Confirmed or RegistrationState.Completed => ("Confirmed", false),
            RegistrationState.NeedsAction => ("Needs your attention", true),
            _ => ("Awaiting payment", true)
        };
    }
}
