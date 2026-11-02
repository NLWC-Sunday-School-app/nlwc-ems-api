using System.Text.Json;
using nlwc.ems.Core.Models;
using nlwc.ems.Dtos;

namespace nlwc.ems.Services.Implementations;

public class RegistrationService(IRegistrationDao registrationDao, IConsentRecordDao consentDao, IHealthDisclosureDao healthDao,
    IPersonDao personDao, IEditionDao editionDao, IGuardianAuthorizationDao guardianDao, IEmergencyContactDao contactDao,
    IAuditEventPublisher auditPublisher, IRegistrationNotifier notifier) : BaseService<Registration>(registrationDao), IRegistrationService
{
    private const int MaxDraftBytes = 20_000;

    public async Task<IReadOnlyList<RegistrationDto>> ListAsync(Guid callerPersonId)
        => (await registrationDao.RetrieveForActorAsync(callerPersonId)).Select(ToDto).ToList();

    public async Task<RegistrationDto> GetAsync(Guid callerPersonId, Guid registrationId)
        => ToDto(await RequireAsync(callerPersonId, registrationId));

    public async Task<RegistrationDto> CreateAsync(Guid callerPersonId, CreateRegistrationRequest request)
    {
        var edition = await editionDao.RetrieveByIdAsync(request.EditionId!.Value) ?? throw new NotFoundException("Edition not found.");
        if (edition.RegistrationWindow != RegistrationWindow.Open)
            throw new AppException("Registration is not open for this event.", isConflict: true, code: "registration_closed");

        var subjectId = request.PersonId ?? callerPersonId;
        if (subjectId != callerPersonId) await PersonRules.RequireGuardianAsync(guardianDao, callerPersonId, subjectId);

        var subject = await personDao.RetrieveByIdAsync(subjectId) ?? throw new NotFoundException("Not found.");
        if (subject.DateOfBirth is not { } dob)
            throw new ValidationException(["Add a date of birth before registering."], "dob_required");

        var age = PersonRules.AgeOn(dob, edition.AgeReferenceDate);
        if (age < 0) throw new ValidationException(["The date of birth is after the event's age reference date."], "invalid_date_of_birth");

        // A person under 18 registers through a guardian, who gives the consents. A teen with their own login is not built yet,
        // so a self-registration under 18 is refused rather than left without a guardian's consent.
        if (RegistrationRules.IsMinor(age) && subjectId == callerPersonId)
            throw new ValidationException(["A guardian must register a child under 18."], "guardian_required");

        // Someone who is 18 on the reference date is an adult for this event: a guardian cannot answer consents for them.
        if (!RegistrationRules.IsMinor(age) && subjectId != callerPersonId)
            throw new ValidationException(["This person is 18 or over at this event and must register themself."], "adult_registers_themself");

        if (!RegistrationRules.WithinEditionAge(age, edition.MaxAge))
            throw new ValidationException([$"This event is for people up to {edition.MaxAge} years old."], "age_ineligible");

        var mode = request.ParticipationMode!.Value;
        if (mode == ParticipationMode.Camp && !RegistrationRules.CampAgeAllowed(age, edition.CampMinAge, edition.CampMaxAge))
            throw new ValidationException(["This person is outside the camp age range."], "camp_age_ineligible");

        if (await registrationDao.RetrieveActiveAsync(subjectId, edition.Id) is not null) throw AlreadyRegistered();

        var registration = new Registration
        {
            Id = Guid.NewGuid(), EditionId = edition.Id, PersonId = subjectId, State = RegistrationState.Draft, ParticipationMode = mode,
            AgeOnReferenceDate = age, AgeBand = RegistrationRules.AgeBand(age)
        };
        List<RegistrationActor> actors =
        [
            new() { Id = Guid.NewGuid(), RegistrationId = registration.Id, PersonId = callerPersonId, Role = RegistrationRole.Registrant },
            new() { Id = Guid.NewGuid(), RegistrationId = registration.Id, PersonId = subjectId, Role = RegistrationRole.Beneficiary }
        ];
        if (!await registrationDao.InsertNewAsync(registration, edition.Code, actors)) throw AlreadyRegistered();
        return ToDto(registration);
    }

    public async Task SaveDraftAsync(Guid callerPersonId, Guid registrationId, SaveDraftRequest request)
    {
        var registration = await RequireEditableAsync(callerPersonId, registrationId);
        if (request.Draft.ValueKind != JsonValueKind.Object) throw new ValidationException(["The draft must be a JSON object."], "validation_failed");
        var raw = request.Draft.GetRawText();
        if (raw.Length > MaxDraftBytes) throw new ValidationException(["The draft is too large."], "validation_failed");
        registration.Draft = raw;
        await UpdateOrThrowAsync(registration);
    }

    public async Task<ReviewDto> ReviewAsync(Guid callerPersonId, Guid registrationId)
        => await BuildReviewAsync(await RequireAsync(callerPersonId, registrationId));

    public async Task<ReviewDto> SubmitAsync(Guid callerPersonId, Guid registrationId)
    {
        var registration = await RequireEditableAsync(callerPersonId, registrationId);
        var review = await BuildReviewAsync(registration);
        if (!review.CanSubmit)
            throw new AppException("Complete the open steps before submitting.", code: "registration_incomplete");   // the review lists them

        var from = registration.State;
        // With no payment step on the edition there is nothing left to wait for. Camp is a request reviewed separately.
        registration.State = await editionDao.IsCapabilityEnabledAsync(registration.EditionId, Capability.PaidRegistration)
            ? RegistrationState.Submitted
            : RegistrationState.Confirmed;
        await registrationDao.TransitionAsync(registration, from, null, callerPersonId);
        await auditPublisher.PublishAsync(new EntityAuditEvent
        {
            EntityType = nameof(Registration), RecordId = registration.Id.ToString(), DisplayName = registration.Reference,
            Action = "registration.submitted", EditionId = registration.EditionId
        });
        await notifier.NotifyRegisteredAsync(callerPersonId, registration.Id);   // never throws; a failed email must not undo the registration
        review.State = registration.State;
        return review;
    }

    public async Task CancelAsync(Guid callerPersonId, Guid registrationId, string reason)
    {
        var registration = await RequireAsync(callerPersonId, registrationId);
        if (!RegistrationRules.Cancellable(registration.State))
            throw new AppException("This registration can no longer be cancelled.", isConflict: true, code: "registration_not_cancellable");

        var from = registration.State;
        registration.State = RegistrationState.Cancelled;
        await registrationDao.TransitionAsync(registration, from, reason, callerPersonId);
        await auditPublisher.PublishAsync(new EntityAuditEvent
        {
            EntityType = nameof(Registration), RecordId = registration.Id.ToString(), DisplayName = registration.Reference,
            Action = "registration.cancelled", Reason = reason, EditionId = registration.EditionId
        });
    }

    private async Task<ReviewDto> BuildReviewAsync(Registration registration)
    {
        var tasks = new List<ReviewTaskDto>();
        var age = registration.AgeOnReferenceDate ?? 0;

        if (RegistrationRules.IsPhysical(registration.ParticipationMode))
        {
            if (await contactDao.RetrieveForPersonAsync(registration.PersonId) is null) tasks.Add(new() { Code = "emergency_contact", Blocking = true });
            if (await healthDao.RetrieveForRegistrationAsync(registration.Id) is null) tasks.Add(new() { Code = "health", Blocking = true });
        }

        var answers = (await consentDao.RetrieveCurrentAsync(registration.Id)).ToDictionary(c => c.Kind);
        foreach (var kind in RegistrationRules.RequiredConsents(age))
            if (!answers.TryGetValue(kind, out var a) || !a.Accepted) tasks.Add(new() { Code = ConsentTask(kind), Blocking = true });

        // Media permission only applies to children, and a No is a complete answer. Only "unanswered" is open (Q20).
        var holdsPass = RegistrationRules.IsMinor(age) && !answers.ContainsKey(ConsentKind.Media);
        if (holdsPass) tasks.Add(new() { Code = ConsentTask(ConsentKind.Media), Blocking = false });

        return new ReviewDto
        {
            RegistrationId = registration.Id, State = registration.State, OpenTasks = tasks,
            CanSubmit = RegistrationRules.Editable(registration.State) && tasks.All(t => !t.Blocking), HoldsPass = holdsPass
        };
    }

    internal static string ConsentTask(ConsentKind kind) => kind switch
    {
        ConsentKind.Terms => "consent_terms",
        ConsentKind.GuardianAuthorization => "consent_guardian_authorization",
        ConsentKind.MedicalEmergency => "consent_medical_emergency",
        _ => "consent_media"
    };

    private async Task<Registration> RequireAsync(Guid callerPersonId, Guid registrationId)
        => await registrationDao.RetrieveForActorAsync(callerPersonId, registrationId) ?? throw new NotFoundException("Not found.");

    private async Task<Registration> RequireEditableAsync(Guid callerPersonId, Guid registrationId)
    {
        var registration = await RequireAsync(callerPersonId, registrationId);
        if (!RegistrationRules.Editable(registration.State))
            throw new AppException("This registration can no longer be edited.", isConflict: true, code: "registration_locked");
        return registration;
    }

    private static AppException AlreadyRegistered()
        => new("This person already has a registration for this event.", isConflict: true, code: "already_registered");

    private static RegistrationDto ToDto(Registration r) => new()
    {
        Id = r.Id, Reference = r.Reference, EditionId = r.EditionId, PersonId = r.PersonId, State = r.State, ParticipationMode = r.ParticipationMode,
        AgeOnReferenceDate = r.AgeOnReferenceDate, AgeBand = r.AgeBand, Draft = JsonDocument.Parse(r.Draft).RootElement.Clone(), DateCreated = r.DateCreated
    };
}

public class ConsentService(IConsentRecordDao consentDao, IRegistrationDao registrationDao, IEditionDao editionDao,
    IAuditEventPublisher auditPublisher) : BaseService<ConsentRecord>(consentDao), IConsentService
{
    public async Task<IReadOnlyList<ConsentDto>> ListAsync(Guid callerPersonId, Guid registrationId)
    {
        await RequireAsync(callerPersonId, registrationId);
        return (await consentDao.RetrieveCurrentAsync(registrationId)).Select(ToDto).ToList();
    }

    public async Task<ConsentDto> RecordAsync(Guid callerPersonId, Guid registrationId, ConsentKind kind, bool accepted)
    {
        var registration = await RequireAsync(callerPersonId, registrationId);
        if (!RegistrationRules.Editable(registration.State))
            throw new AppException("This registration can no longer be edited.", isConflict: true, code: "registration_locked");

        var age = registration.AgeOnReferenceDate ?? 0;
        // Guardian and medical-emergency consent are a parent's to give for a child; an adult has nothing to authorise for themself.
        if (kind is ConsentKind.GuardianAuthorization or ConsentKind.MedicalEmergency or ConsentKind.Media && !RegistrationRules.IsMinor(age))
            throw new ValidationException(["This permission only applies to a child."], "consent_not_applicable");

        var edition = await editionDao.RetrieveByIdAsync(registration.EditionId) ?? throw new NotFoundException("Not found.");
        var documentId = $"{edition.Code}-{kind.ToString().ToLowerInvariant()}";
        var policy = await consentDao.RetrieveApprovedPolicyAsync(documentId);

        var record = await InsertOrThrowAsync(new ConsentRecord
        {
            Id = Guid.NewGuid(), RegistrationId = registrationId, PersonId = registration.PersonId, Kind = kind, Accepted = accepted,
            DocumentId = documentId, DocumentVersion = policy?.Version ?? 0, AnsweredBy = callerPersonId
        });
        await auditPublisher.PublishAsync(new EntityAuditEvent
        {
            EntityType = nameof(ConsentRecord), RecordId = record.Id.ToString(), DisplayName = documentId, Action = "consent.recorded",
            EditionId = registration.EditionId, Detail = new { registrationId, kind = kind.ToString(), accepted }
        });
        return ToDto(record);
    }

    private async Task<Registration> RequireAsync(Guid callerPersonId, Guid registrationId)
        => await registrationDao.RetrieveForActorAsync(callerPersonId, registrationId) ?? throw new NotFoundException("Not found.");

    private static ConsentDto ToDto(ConsentRecord c)
        => new() { Kind = c.Kind, Accepted = c.Accepted, DocumentId = c.DocumentId, DocumentVersion = c.DocumentVersion, AnsweredAt = c.DateCreated };
}

public class HealthService(IHealthDisclosureDao healthDao, IRegistrationDao registrationDao, IFieldEncryptor encryptor,
    IAuditEventPublisher auditPublisher) : BaseService<HealthDisclosure>(healthDao), IHealthService
{
    public async Task<HealthDto?> GetAsync(Guid callerPersonId, Guid registrationId)
    {
        await RequireAsync(callerPersonId, registrationId);
        var disclosure = await healthDao.RetrieveForRegistrationAsync(registrationId);
        return disclosure is null ? null : JsonSerializer.Deserialize<HealthDto>(encryptor.DecryptString(disclosure.DataEncrypted));
    }

    public async Task SetAsync(Guid callerPersonId, Guid registrationId, HealthRequest request)
    {
        var registration = await RequireAsync(callerPersonId, registrationId);
        if (!RegistrationRules.Editable(registration.State))
            throw new AppException("This registration can no longer be edited.", isConflict: true, code: "registration_locked");

        var conditions = HealthRules.Validate(request.None, request.Conditions, request.Allergies, request.Notes);
        var dto = new HealthDto { None = request.None, Conditions = conditions, Allergies = request.Allergies?.Trim(), Notes = request.Notes?.Trim() };
        var hasFlags = !request.None || !string.IsNullOrWhiteSpace(dto.Allergies) || !string.IsNullOrWhiteSpace(dto.Notes);

        await healthDao.ReplaceAsync(new HealthDisclosure
        {
            Id = Guid.NewGuid(), RegistrationId = registrationId, DataEncrypted = encryptor.EncryptString(JsonSerializer.Serialize(dto)), HasFlags = hasFlags
        });
        // The event records that health was updated, never what it says.
        await auditPublisher.PublishAsync(new EntityAuditEvent
        {
            EntityType = nameof(HealthDisclosure), RecordId = registrationId.ToString(), Action = "health.updated", EditionId = registration.EditionId
        });
    }

    private async Task<Registration> RequireAsync(Guid callerPersonId, Guid registrationId)
        => await registrationDao.RetrieveForActorAsync(callerPersonId, registrationId) ?? throw new NotFoundException("Not found.");
}

public class StaffRegistrationService(IRegistrationDao registrationDao, IHealthDisclosureDao healthDao, IAccessLogDao accessLogDao,
    IFieldEncryptor encryptor, IConsentRecordDao consentDao, IEmergencyContactDao contactDao, IAuditEventPublisher auditPublisher)
    : BaseService<Registration>(registrationDao), IStaffRegistrationService
{
    public async Task<StaffRegistrationDetailDto> GetDetailAsync(Guid registrationId, bool includeHealthFlags)
    {
        var d = await registrationDao.RetrieveStaffDetailAsync(registrationId) ?? throw new NotFoundException("Not found.");
        var r = d.Registration;
        var contact = await contactDao.RetrieveForPersonAsync(r.PersonId);
        var consents = await consentDao.RetrieveCurrentAsync(r.Id);
        return new StaffRegistrationDetailDto
        {
            Id = r.Id, Reference = r.Reference, EditionId = r.EditionId, State = r.State, ParticipationMode = r.ParticipationMode,
            AgeBand = r.AgeBand, AgeOnReferenceDate = r.AgeOnReferenceDate, DateCreated = r.DateCreated,
            Person = new StaffPersonDto
            {
                PersonId = d.Person.Id, GivenName = d.Person.GivenName, FamilyName = d.Person.FamilyName, DateOfBirth = d.Person.DateOfBirth,
                Gender = d.Person.Gender, Country = d.Person.Country, City = d.Person.City
            },
            HasHealthFlags = includeHealthFlags ? (await healthDao.RetrieveForRegistrationAsync(r.Id))?.HasFlags ?? false : null,
            Actors = d.Actors.Select(a => new StaffActorDto { PersonId = a.PersonId, Name = a.Name, Role = a.Role }).ToList(),
            Consents = consents.OrderBy(c => c.Kind).Select(c => new StaffConsentDto { Kind = c.Kind, Accepted = c.Accepted, DocumentVersion = c.DocumentVersion, At = c.DateCreated }).ToList(),
            EmergencyContact = contact is null ? null : new StaffEmergencyContactDto { FullName = contact.FullName, Relationship = contact.Relationship },
            History = d.History.Select(h => new StaffHistoryDto { From = h.FromState, To = h.ToState, Actor = h.ActorName, Reason = h.Reason, At = h.At }).ToList(),
            Cancellable = RegistrationRules.Cancellable(r.State)
        };
    }

    public async Task<StaffDashboardDto> GetDashboardAsync(Guid editionId)
    {
        var stats = await registrationDao.RetrieveStaffStatsAsync(editionId, DateTime.UtcNow.AddDays(-7));
        return new StaffDashboardDto
        {
            Total = stats.ByState.Values.Sum(), ByState = stats.ByState, ByMode = stats.ByMode, ByAgeBand = stats.ByAgeBand,
            NeedsAction = stats.ByState.GetValueOrDefault(RegistrationState.NeedsAction), StaleDrafts = stats.StaleDrafts
        };
    }

    public async Task<string> ExportCsvAsync(Guid editionId, StaffRegistrationFilter filter, bool includeHealthFlags, string reason)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("Reference,Given name,Family name,State,Mode,Age band,Created");
        if (includeHealthFlags) sb.Append(",Health flags");
        sb.Append("\r\n");
        var total = 0;
        for (var page = 0; ; page++)
        {
            var rows = await registrationDao.RetrieveStaffPagedAsync(editionId, filter, page, 500);
            var batch = rows.Data.ToList();
            foreach (var r in batch)
            {
                sb.Append(string.Join(',', Csv(r.Reference), Csv(r.GivenName), Csv(r.FamilyName), r.State, r.ParticipationMode, Csv(r.AgeBand ?? ""), r.DateCreated.ToString("yyyy-MM-dd HH:mm")));
                if (includeHealthFlags) sb.Append(',').Append(r.HasHealthFlags ? "Yes" : "No");
                sb.Append("\r\n");
            }
            total += batch.Count;
            if (batch.Count == 0 || total >= rows.Count) break;
        }
        await auditPublisher.PublishAsync(new EntityAuditEvent
        {
            EntityType = nameof(Registration), RecordId = editionId.ToString(), DisplayName = "Registration export",
            Action = "registration.exported", Reason = $"{reason.Trim()} ({total} rows)", EditionId = editionId
        });
        return sb.ToString();
    }

    // Quotes every cell, and defuses a leading = + - @ so a spreadsheet never runs a name as a formula.
    private static string Csv(string value)
    {
        if (value.Length > 0 && "=+-@\t\r".Contains(value[0])) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    public async Task CancelAsync(Guid staffPersonId, Guid registrationId, string reason)
    {
        var registration = await registrationDao.RetrieveByIdAsync(registrationId) ?? throw new NotFoundException("Not found.");
        if (!RegistrationRules.Cancellable(registration.State))
            throw new AppException("This registration can no longer be cancelled.", isConflict: true, code: "registration_not_cancellable");
        var from = registration.State;
        registration.State = RegistrationState.Cancelled;
        await registrationDao.TransitionAsync(registration, from, reason.Trim(), staffPersonId);
        await auditPublisher.PublishAsync(new EntityAuditEvent
        {
            EntityType = nameof(Registration), RecordId = registration.Id.ToString(), DisplayName = registration.Reference,
            Action = "registration.cancelled_by_staff", Reason = reason.Trim(), EditionId = registration.EditionId
        });
    }

    public async Task<PagerEntity<StaffRegistrationDto>> ListAsync(Guid editionId, StaffRegistrationFilter filter, int page, int max, bool includeHealthFlags)
    {
        var rows = await registrationDao.RetrieveStaffPagedAsync(editionId, filter, page, max);
        return new PagerEntity<StaffRegistrationDto>
        {
            Page = rows.Page, Max = rows.Max, Count = rows.Count,
            Data = rows.Data.Select(r => new StaffRegistrationDto
            {
                Id = r.Id, Reference = r.Reference, PersonId = r.PersonId, GivenName = r.GivenName, FamilyName = r.FamilyName, State = r.State,
                ParticipationMode = r.ParticipationMode, AgeBand = r.AgeBand, DateCreated = r.DateCreated,
                // Flags-only roles see that something exists; roles without health access are not told even that.
                HasHealthFlags = includeHealthFlags ? r.HasHealthFlags : null
            }).ToList()
        };
    }

    public async Task<Guid> GetEditionIdAsync(Guid registrationId)
        => (await registrationDao.RetrieveByIdAsync(registrationId) ?? throw new NotFoundException("Not found.")).EditionId;

    public async Task<HealthDto?> RevealHealthAsync(Guid actorAccountId, Guid registrationId, string reason)
    {
        var registration = await registrationDao.RetrieveByIdAsync(registrationId) ?? throw new NotFoundException("Not found.");
        // Logged before the data leaves, so a failed response cannot hide that someone looked.
        await accessLogDao.InsertAsync(new AccessLog
        {
            Id = Guid.NewGuid(), ActorAccountId = actorAccountId, Resource = "health", SubjectPersonId = registration.PersonId,
            EditionId = registration.EditionId, Reason = reason.Trim()
        });
        var disclosure = await healthDao.RetrieveForRegistrationAsync(registrationId);
        return disclosure is null ? null : JsonSerializer.Deserialize<HealthDto>(encryptor.DecryptString(disclosure.DataEncrypted));
    }
}
