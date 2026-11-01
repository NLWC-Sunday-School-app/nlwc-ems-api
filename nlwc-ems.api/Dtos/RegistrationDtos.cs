using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace nlwc.ems.Dtos;

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
