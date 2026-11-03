using nlwc.ems.Core.Models;
using nlwc.ems.Dtos;

namespace nlwc.ems.Services.Interfaces;

// ---- Identity ----

/// <summary>Who is calling, resolved from the session cookie. <see cref="Permissions"/> are the expanded
/// <c>resource:level[@editionId]</c> codes from the account's active grants.</summary>
public record SessionPrincipal(Guid SessionId, Guid AccountId, Guid PersonId, Surface Surface, bool MfaVerified, bool EmailVerified,
    IReadOnlyList<string> Permissions);

public interface IAccountService : IBaseService<Account>
{
    /// <summary>Always completes the same way, so sign-up cannot reveal whether an email already has an account.</summary>
    Task SignUpAsync(SignUpRequest request);

    Task VerifyEmailAsync(string token);

    /// <summary>Checks the credentials and opens a session. Throws <see cref="UnauthorizedException"/> with the same
    /// message for an unknown email and a wrong password.</summary>
    Task<(string Token, SessionPrincipal Principal)> LogInAsync(string email, string password, Surface surface);
}

public interface ISessionService : IBaseService<AuthSession>
{
    Task<(string Token, SessionPrincipal Principal)> CreateAsync(Guid accountId, Surface surface);

    /// <summary>The live session for a cookie token, with the caller's permissions. Slides the idle timeout. Null when
    /// the token is unknown, revoked or expired.</summary>
    Task<SessionPrincipal?> ResolveAsync(string token);

    Task LogOutAsync(Guid sessionId);
    Task MarkMfaVerifiedAsync(Guid sessionId);
}

public interface IMfaService : IBaseService<MfaFactor>
{
    /// <summary>Starts (or restarts) TOTP enrolment and returns the <c>otpauth://</c> URI for the authenticator app.</summary>
    /// <summary>Refused with <c>mfa_already_enrolled</c> once a factor is confirmed: replacing it would let a hijacked session
    /// take over the second factor. A lost device is reset by an administrator.</summary>
    Task<string> EnrollAsync(Guid accountId);

    Task<bool> IsEnrolledAsync(Guid accountId);

    /// <summary>Confirms a pending factor, or step-up verifies an existing one, and marks the session MFA-verified.</summary>
    Task VerifyAsync(Guid accountId, Guid sessionId, string code);
}

// ---- Access and audit ----

public interface IRoleService : IBaseService<Role>
{
    Task<Role?> RetrieveByKeyAsync(string key);
}

public interface IRoleGrantService : IBaseService<RoleGrant>
{
    /// <summary>The expanded permission codes for an account's active grants: <c>health:full</c> yields <c>health:full</c>,
    /// <c>health:view</c> and <c>health:flags</c>; an edition-scoped grant yields <c>code@editionId</c>.</summary>
    Task<IReadOnlyList<string>> GetPermissionCodesAsync(Guid accountId);

    Task<RoleGrant> GrantAsync(Guid actorAccountId, GrantRoleRequest request);
    Task RevokeAsync(Guid actorAccountId, Guid grantId, string reason);

    /// <summary>Finds an account by exact email so a grant can name it. Null when there is none.</summary>
    Task<StaffLookupDto?> LookupAsync(string email);
}

public interface IAuditService
{
    Task<PagerEntity<AuditEvent>> RetrievePagedAsync(int page, int max, string? action);
    Task<long?> VerifyChainAsync();
}

// ---- People ----

public interface IProfileService : IBaseService<Person>
{
    Task<ProfileDto> GetAsync(Guid accountId, Guid personId, bool emailVerified);
    Task UpdateAsync(Guid accountId, Guid personId, UpdateProfileRequest request);
}

public interface IDependantService : IBaseService<GuardianAuthorization>
{
    Task<IReadOnlyList<DependantDto>> ListAsync(Guid guardianPersonId);
    Task<Guid> AddAsync(Guid guardianPersonId, AddDependantRequest request);
    Task UpdateAsync(Guid guardianPersonId, Guid dependantPersonId, UpdateDependantRequest request);
    Task RemoveAsync(Guid guardianPersonId, Guid dependantPersonId, string reason);
}

public interface ICollectorService : IBaseService<AuthorizedCollector>
{
    Task<IReadOnlyList<CollectorDto>> ListAsync(Guid guardianPersonId, Guid childPersonId);
    Task<Guid> AddAsync(Guid guardianPersonId, Guid childPersonId, AddCollectorRequest request);
    Task RemoveAsync(Guid guardianPersonId, Guid childPersonId, Guid collectorId);
}

public interface IEmergencyContactService : IBaseService<EmergencyContact>
{
    /// <summary>For yourself (<paramref name="subjectPersonId"/> = your own person id) or one of your dependants.</summary>
    Task SetAsync(Guid callerPersonId, Guid subjectPersonId, EmergencyContactRequest request);

    Task<ContactDto?> GetAsync(Guid callerPersonId, Guid subjectPersonId);
}

// ---- Catalog and pricing ----

public interface IEditionService : IBaseService<EventEdition>
{
    /// <summary>Validates and creates an edition from a template, with its venue, days, every capability switched off,
    /// and its reference counter.</summary>
    Task<ServiceResponse<EventEdition>> CreateAsync(EventEdition edition, CreateEditionRequest request);

    /// <summary>Applies the editable details (web address, dates, venue and their days) of an update onto the edition.</summary>
    Task ApplyDetailsAsync(EventEdition edition, UpdateEditionRequest request);

    Task<IReadOnlyList<PublicEditionDto>> ListPublicAsync();
    Task<PublicEditionDto?> GetPublicAsync(string slug);

    /// <summary>Programmes with no public edition yet. Names only: dates and prices appear once an edition is public.</summary>
    Task<IReadOnlyList<ComingSoonEventDto>> ListComingSoonAsync();

    /// <summary>The editions a staff member can work in: all of them with a church-wide grant, otherwise those named by an
    /// edition-scoped grant (<paramref name="scopedEditionIds"/>).</summary>
    Task<IReadOnlyList<EditionSummaryDto>> ListForStaffAsync(bool churchWide, IReadOnlyCollection<Guid> scopedEditionIds);

    Task SetCapabilityAsync(Guid editionId, Capability capability, bool enabled, string? reason);
    Task TransitionAsync(Guid editionId, EditionStage to, string reason);
}

public interface IRatePlanService : IBaseService<RatePlan>
{
    /// <summary>"free" only from an explicit published <c>Free</c> rule set; "pending" when nothing is published.</summary>
    Task<PriceState> GetPriceStateAsync(Guid editionId);

    Task<IReadOnlyList<RateVersionDto>> ListVersionsAsync(Guid editionId);
    Task<Guid> DraftAsync(Guid actorAccountId, Guid editionId, string? note, IReadOnlyList<RateRuleInput> rules);
    Task ApproveAsync(Guid actorAccountId, Guid editionId, Guid versionId, string? note);
    Task PublishAsync(Guid actorAccountId, Guid editionId, Guid versionId, string? note);
}

// ---- Registration ----

/// <summary>Owner-scoped: <c>callerPersonId</c> must be the registrant (or the person themself). Anything else answers 404.</summary>
public interface IRegistrationService : IBaseService<Registration>
{
    Task<IReadOnlyList<RegistrationDto>> ListAsync(Guid callerPersonId);
    Task<RegistrationDto> GetAsync(Guid callerPersonId, Guid registrationId);
    Task<RegistrationDto> CreateAsync(Guid callerPersonId, CreateRegistrationRequest request);
    Task SaveDraftAsync(Guid callerPersonId, Guid registrationId, SaveDraftRequest request);
    Task<ReviewDto> ReviewAsync(Guid callerPersonId, Guid registrationId);
    Task<ReviewDto> SubmitAsync(Guid callerPersonId, Guid registrationId);
    Task CancelAsync(Guid callerPersonId, Guid registrationId, string reason);
}

public interface IConsentService : IBaseService<ConsentRecord>
{
    Task<IReadOnlyList<ConsentDto>> ListAsync(Guid callerPersonId, Guid registrationId);

    /// <summary>Appends a new answer (consents are never edited in place).</summary>
    Task<ConsentDto> RecordAsync(Guid callerPersonId, Guid registrationId, ConsentKind kind, bool accepted);
}

public interface IHealthService : IBaseService<HealthDisclosure>
{
    Task<HealthDto?> GetAsync(Guid callerPersonId, Guid registrationId);
    Task SetAsync(Guid callerPersonId, Guid registrationId, HealthRequest request);
}

/// <summary>Staff views of registrations. The controller checks permissions against the registration's edition.</summary>
public interface IStaffRegistrationService : IBaseService<Registration>
{
    Task<PagerEntity<StaffRegistrationDto>> ListAsync(Guid editionId, StaffRegistrationFilter filter, int page, int max, bool includeHealthFlags);

    Task<StaffDashboardDto> GetDashboardAsync(Guid editionId);

    /// <summary>The whole filtered list as CSV (no health content; flags only when allowed). Cells that start like a formula are neutralised.</summary>
    Task<string> ExportCsvAsync(Guid editionId, StaffRegistrationFilter filter, bool includeHealthFlags, string reason);

    /// <summary>Everything staff need to understand one registration, without health answers or phone numbers.</summary>
    Task<StaffRegistrationDetailDto> GetDetailAsync(Guid registrationId, bool includeHealthFlags);

    /// <summary>Cancels on staff's behalf: the reason is kept in the history and the audit log.</summary>
    Task CancelAsync(Guid staffPersonId, Guid registrationId, string reason);

    /// <summary>The edition a registration belongs to, or 404.</summary>
    Task<Guid> GetEditionIdAsync(Guid registrationId);

    /// <summary>Writes the access-log row first, then returns the decrypted answers (null when none were given).</summary>
    Task<HealthDto?> RevealHealthAsync(Guid actorAccountId, Guid registrationId, string reason);
}

/// <summary>Emails the parent or guardian once a child's registration is in. It never throws: a missing or failed email must not undo a registration.</summary>
public interface IRegistrationNotifier
{
    Task NotifyRegisteredAsync(Guid guardianPersonId, Guid registrationId);
}

public interface IStaffPeopleService : IBaseService<PersonRelationship>
{
    Task<PagerEntity<StaffPersonSummaryDto>> SearchAsync(string? text, int page, int max);
    Task<StaffPersonDetailDto> GetDetailAsync(Guid personId);
    Task<IReadOnlyList<DuplicatePairDto>> GetDuplicatesAsync();

    /// <summary>"These are different people": the pair is not offered again. The reason is audited.</summary>
    Task DismissDuplicateAsync(Guid staffPersonId, Guid personAId, Guid personBId, string reason);

    /// <summary>Both must be adults and unmarried in our records; the link goes both ways.</summary>
    Task PairSpousesAsync(Guid staffPersonId, Guid personId, Guid spouseId, string reason);

    Task UnpairSpousesAsync(Guid staffPersonId, Guid personId, string reason);
}

public interface ICampReviewService : IBaseService<CampDecision>
{
    Task<CampQueueDto> GetQueueAsync(Guid editionId, bool includeHealthFlags);

    /// <summary>A human decision. Approval needs a camp capacity and room in it.</summary>
    Task DecideAsync(Guid staffPersonId, Guid registrationId, CampDecisionKind decision, string reason);
}
