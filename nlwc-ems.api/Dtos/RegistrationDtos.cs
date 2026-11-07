using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace nlwc.ems.Dtos;

/// <summary>Several of a family's registrations submitted together, so the parent gets one confirmation email listing all of them.</summary>
public class SubmitManyRequest
{
    [Required, MinLength(1), MaxLength(20)]
    public List<Guid> RegistrationIds { get; set; } = [];
}

/// <summary>What happened to one registration in a <see cref="SubmitManyRequest"/>. A failure for one never stops the others.</summary>
public class SubmitOutcomeDto
{
    public Guid RegistrationId { get; set; }
    public bool Ok { get; set; }
    public RegistrationState? State { get; set; }
    public string? Code { get; set; }
    public string? Message { get; set; }
}

public class CreateRegistrationRequest
{
    [Required]
    public Guid? EditionId { get; set; }

    /// <summary>Omit to register yourself; otherwise one of your dependants.</summary>
    public Guid? PersonId { get; set; }

    [Required]
    public ParticipationMode? ParticipationMode { get; set; }
}

public class SaveDraftRequest
{
    /// <summary>The wizard's progress as a JSON object. Opaque to the API; a family resumes from it on another device.</summary>
    [Required]
    public JsonElement Draft { get; set; }
}

public class RegistrationDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid EditionId { get; set; }
    public Guid PersonId { get; set; }
    public RegistrationState State { get; set; }
    public ParticipationMode ParticipationMode { get; set; }
    public int? AgeOnReferenceDate { get; set; }
    public string? AgeBand { get; set; }
    public JsonElement Draft { get; set; }
    public DateTime DateCreated { get; set; }
}

public class HealthRequest
{
    /// <summary>Exclusive: when true, no conditions may be listed.</summary>
    public bool None { get; set; }

    [MaxLength(20)]
    public List<string>? Conditions { get; set; }

    [MaxLength(1000)]
    public string? Allergies { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class HealthDto
{
    public bool None { get; set; }
    public List<string> Conditions { get; set; } = [];
    public string? Allergies { get; set; }
    public string? Notes { get; set; }
}

public class ConsentRequest
{
    /// <summary>Required: an unanswered consent is not the same as a No.</summary>
    [Required]
    public bool? Accepted { get; set; }
}

public class ConsentDto
{
    public ConsentKind Kind { get; set; }
    public bool Accepted { get; set; }
    public string DocumentId { get; set; } = string.Empty;
    public int DocumentVersion { get; set; }
    public DateTime AnsweredAt { get; set; }
}

public class ReviewTaskDto
{
    /// <summary>Stable token the frontends branch on, e.g. <c>health</c>, <c>consent_terms</c>.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>A blocking task stops submission. Media is never blocking.</summary>
    public bool Blocking { get; set; }
}

public class ReviewDto
{
    public Guid RegistrationId { get; set; }
    public RegistrationState State { get; set; }
    public List<ReviewTaskDto> OpenTasks { get; set; } = [];
    public bool CanSubmit { get; set; }

    /// <summary>True while media permission is unanswered (open question Q20: the pass stays Pending).</summary>
    public bool HoldsPass { get; set; }
}

public class StaffRegistrationDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid PersonId { get; set; }
    public string GivenName { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public RegistrationState State { get; set; }
    public ParticipationMode ParticipationMode { get; set; }
    public string? AgeBand { get; set; }

    /// <summary>Null unless the caller holds <c>health:flags</c>. Says only that something was declared, never what.</summary>
    public bool? HasHealthFlags { get; set; }

    public DateTime DateCreated { get; set; }
}

public class StaffRegistrationDetailDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid EditionId { get; set; }
    public RegistrationState State { get; set; }
    public ParticipationMode ParticipationMode { get; set; }
    public string? AgeBand { get; set; }
    public int? AgeOnReferenceDate { get; set; }
    public DateTime DateCreated { get; set; }
    public StaffPersonDto Person { get; set; } = new();

    /// <summary>Null unless the caller holds <c>health:flags</c>.</summary>
    public bool? HasHealthFlags { get; set; }

    public List<StaffActorDto> Actors { get; set; } = [];
    public List<StaffConsentDto> Consents { get; set; } = [];
    public List<StaffEmergencyContactDto> EmergencyContacts { get; set; } = [];
    public List<StaffHistoryDto> History { get; set; } = [];
    public bool Cancellable { get; set; }
}

public class StaffPersonDto
{
    public Guid PersonId { get; set; }
    public string GivenName { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
}

public class StaffActorDto { public Guid PersonId { get; set; } public string Name { get; set; } = string.Empty; public RegistrationRole Role { get; set; } }

public class StaffConsentDto { public ConsentKind Kind { get; set; } public bool Accepted { get; set; } public int DocumentVersion { get; set; } public DateTime At { get; set; } }

/// <summary>Name and relationship only. The phone number stays encrypted and is not part of the staff view.</summary>
public class StaffEmergencyContactDto { public string FullName { get; set; } = string.Empty; public string Relationship { get; set; } = string.Empty; }

public class StaffHistoryDto
{
    public RegistrationState? From { get; set; }
    public RegistrationState To { get; set; }
    public string? Actor { get; set; }
    public string? Reason { get; set; }
    public DateTime At { get; set; }
}

public class StaffDashboardDto
{
    public int Total { get; set; }
    public Dictionary<RegistrationState, int> ByState { get; set; } = [];
    public Dictionary<ParticipationMode, int> ByMode { get; set; } = [];
    public Dictionary<string, int> ByAgeBand { get; set; } = [];

    /// <summary>Things a person should look at: registrations that need action, and drafts untouched for a week.</summary>
    public int NeedsAction { get; set; }
    public int StaleDrafts { get; set; }
}

// ---- Staff: camp review ----
public class CampQueueDto
{
    /// <summary>Null until staff set the edition's camp capacity; approvals are refused until then.</summary>
    public int? Capacity { get; set; }
    public int Approved { get; set; }
    public List<CampQueueItemDto> Rows { get; set; } = [];
}

public class CampQueueItemDto
{
    public Guid RegistrationId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid PersonId { get; set; }
    public string GivenName { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public int? Age { get; set; }
    public RegistrationState State { get; set; }
    public bool GuardianConsentGiven { get; set; }

    /// <summary>Null unless the caller holds <c>health:flags</c>.</summary>
    public bool? HasHealthFlags { get; set; }

    /// <summary>Null while pending.</summary>
    public CampDecisionKind? Decision { get; set; }
    public string? DecisionReason { get; set; }
    public DateTime? DecidedAt { get; set; }
}

public class CampDecisionRequest
{
    [Required] public CampDecisionKind? Decision { get; set; }
    [Required, MaxLength(500)] public string Reason { get; set; } = string.Empty;
}
