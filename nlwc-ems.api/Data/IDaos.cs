using nlwc.ems.Core.Models;

namespace nlwc.ems.Data;

// ---- Identity ----
public interface IPersonDao : IBaseDao<Person>;

public interface IAccountDao : IBaseDao<Account>
{
    Task<Account?> RetrieveByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);

    /// <summary>The person, their account, the unverified email contact and its verification token, in one transaction.</summary>
    Task InsertWithPersonAsync(Person person, Account account, VerifiedContact contact, EmailVerification verification);

    /// <summary>Atomic <c>failed_logins + 1</c>; once it reaches <paramref name="maxFailures"/> the account is locked until
    /// <paramref name="lockUntil"/>. Set-based so concurrent bad guesses cannot undercount.</summary>
    Task RegisterFailedLoginAsync(Guid accountId, int maxFailures, DateTime lockUntil);

    Task ResetFailedLoginsAsync(Guid accountId);
}

public interface IEmailVerificationDao : IBaseDao<EmailVerification>
{
    /// <summary>Uses the token once: marks it used and the account's email contact verified, in one transaction.
    /// False when the token is unknown, already used or expired.</summary>
    Task<bool> ConsumeAsync(string tokenHash);
}

public class ResolvedSession
{
    public AuthSession Session { get; set; } = null!;
    public Guid PersonId { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
}

public interface IAuthSessionDao : IBaseDao<AuthSession>
{
    /// <summary>The live session for a token hash (not revoked, inside both timeouts), with its person and email state.
    /// Null when there is none.</summary>
    Task<ResolvedSession?> RetrieveActiveAsync(string tokenHash);

    /// <summary>Slides the idle timeout forward.</summary>
    Task TouchAsync(Guid sessionId, DateTime idleExpiresAt);

    Task RevokeAsync(Guid sessionId);
    Task MarkMfaVerifiedAsync(Guid sessionId);
}

public interface IMfaFactorDao : IBaseDao<MfaFactor>
{
    /// <summary>The confirmed factor if any, else the newest pending one.</summary>
    Task<MfaFactor?> RetrieveLatestAsync(Guid accountId);

    /// <summary>Discards any unconfirmed factor and stores <paramref name="factor"/>, in one transaction.</summary>
    Task ReplacePendingAsync(MfaFactor factor);

    Task ConfirmAsync(Guid factorId);
}

// ---- Access and audit ----
public interface IRoleDao : IBaseDao<Role>
{
    Task<Role?> RetrieveByKeyAsync(string key);
}

public interface IRoleGrantDao : IBaseDao<RoleGrant>
{
    /// <summary>Grants that are neither revoked nor expired, with their role and its permissions.</summary>
    Task<List<RoleGrant>> RetrieveActiveForAccountAsync(Guid accountId);

    /// <summary>True when an active grant existed and is now revoked.</summary>
    Task<bool> RevokeAsync(Guid grantId);
}

public interface IAuditEventDao
{
    /// <summary>Appends one row, chained to the previous row's hash under an advisory lock so the chain stays linear.</summary>
    Task AppendAsync(AuditEvent auditEvent);

    Task<PagerEntity<AuditEvent>> RetrievePagedAsync(int page, int max, string? action);

    /// <summary>Recomputes the chain. The seq of the first broken row, or null when intact.</summary>
    Task<long?> VerifyChainAsync();
}

// ---- Family ----
public class DependantRow
{
    public Guid PersonId { get; set; }
    public string GivenName { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string Relationship { get; set; } = string.Empty;
}

public interface IGuardianAuthorizationDao : IBaseDao<GuardianAuthorization>
{
    Task<GuardianAuthorization?> RetrieveActiveAsync(Guid guardianPersonId, Guid dependantPersonId);
    Task<List<DependantRow>> RetrieveDependantsAsync(Guid guardianPersonId);

    /// <summary>The dependant, the guardian's authority over them and the relationship row, in one transaction.</summary>
    Task InsertDependantAsync(Person dependant, GuardianAuthorization authorization, PersonRelationship relationship);
}

public interface IAuthorizedCollectorDao : IBaseDao<AuthorizedCollector>
{
    Task<List<AuthorizedCollector>> RetrieveForChildAsync(Guid childPersonId);
}

public interface IEmergencyContactDao : IBaseDao<EmergencyContact>
{
    Task<EmergencyContact?> RetrieveForPersonAsync(Guid personId);

    /// <summary>One contact per person: deletes any existing and inserts <paramref name="contact"/>, in one transaction.</summary>
    Task ReplaceAsync(EmergencyContact contact);
}

// ---- Catalog and pricing ----
public interface IEditionDao : IBaseDao<EventEdition>
{
    Task<List<EventEdition>> RetrievePublicAsync();
    Task<EventEdition?> RetrievePublicBySlugAsync(string slug);
    Task<EventTemplate?> RetrieveTemplateByCodeAsync(string code);
    Task<bool> SlugOrCodeExistsAsync(string slug, string code);

    /// <summary>The edition with its venue, days, off-by-default capabilities and reference counter, in one transaction.</summary>
    Task InsertWithChildrenAsync(EventEdition edition, Venue? venue, IReadOnlyList<EventDay> days,
        IReadOnlyList<EditionCapability> capabilities, ReferenceCounter counter);

    /// <summary>True when the edition has that capability row (every edition gets all of them at creation).</summary>
    Task<bool> SetCapabilityAsync(Guid editionId, Capability capability, bool enabled);

    Task<bool> IsCapabilityEnabledAsync(Guid editionId, Capability capability);
}

public interface IRatePlanDao : IBaseDao<RatePlan>
{
    /// <summary>The kinds of the rules in the edition's published version; empty when nothing is published.</summary>
    Task<List<RateKind>> RetrievePublishedKindsAsync(Guid editionId);
}

public interface IRatePlanVersionDao : IBaseDao<RatePlanVersion>
{
    /// <summary>Creates the edition's plan if it has none, then the next draft version with its rules, in one transaction.
    /// Null when the edition does not exist.</summary>
    Task<RatePlanVersion?> InsertDraftAsync(Guid editionId, Guid draftedBy, string? note, IReadOnlyList<RateRule> rules);

    /// <summary>The version if it belongs to the edition's plan, with its rules.</summary>
    Task<RatePlanVersion?> RetrieveForEditionAsync(Guid editionId, Guid versionId);

    /// <summary>Retires the plan's published version (if any) and publishes this one, in one transaction.</summary>
    Task PublishAsync(RatePlanVersion version);
}

// ---- Registration ----
public class StaffRegistrationRow
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid PersonId { get; set; }
    public string GivenName { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public RegistrationState State { get; set; }
    public ParticipationMode ParticipationMode { get; set; }
    public string? AgeBand { get; set; }
    public bool HasHealthFlags { get; set; }
    public DateTime DateCreated { get; set; }
}

public interface IRegistrationDao : IBaseDao<Registration>
{
    /// <summary>Registrations the person is an actor on (as registrant or beneficiary), newest first.</summary>
    Task<List<Registration>> RetrieveForActorAsync(Guid personId);

    /// <summary>The registration if the person is its registrant or beneficiary; null otherwise (so it answers 404).</summary>
    Task<Registration?> RetrieveForActorAsync(Guid personId, Guid registrationId);

    /// <summary>The person's non-cancelled registration for the edition, if any.</summary>
    Task<Registration?> RetrieveActiveAsync(Guid personId, Guid editionId);

    /// <summary>Takes the next reference from the edition's counter, then inserts the registration, its actors and its first
    /// history row, in one transaction. False when the one-active-registration index refuses it.</summary>
    Task<bool> InsertNewAsync(Registration registration, string editionCode, IReadOnlyList<RegistrationActor> actors);

    /// <summary>One edition's registrations for the staff queue, newest first. Health is never read here, only whether flags exist.</summary>
    Task<PagerEntity<StaffRegistrationRow>> RetrieveStaffPagedAsync(Guid editionId, RegistrationState? state, int page, int max);

    /// <summary>Updates the registration and appends the history row, in one transaction.</summary>
    Task TransitionAsync(Registration registration, RegistrationState from, string? reason, Guid actorPersonId);
}

public interface IConsentRecordDao : IBaseDao<ConsentRecord>
{
    /// <summary>The latest answer per kind.</summary>
    Task<List<ConsentRecord>> RetrieveCurrentAsync(Guid registrationId);

    /// <summary>Latest approved wording for the document, or null.</summary>
    Task<PolicyVersion?> RetrieveApprovedPolicyAsync(string documentId);
}

public interface IHealthDisclosureDao : IBaseDao<HealthDisclosure>
{
    Task<HealthDisclosure?> RetrieveForRegistrationAsync(Guid registrationId);

    /// <summary>One disclosure per registration: replaces any existing, in one transaction.</summary>
    Task ReplaceAsync(HealthDisclosure disclosure);
}

/// <summary>Reads of restricted data. Append-only; written before the data is returned.</summary>
public interface IAccessLogDao : IBaseDao<AccessLog>;
