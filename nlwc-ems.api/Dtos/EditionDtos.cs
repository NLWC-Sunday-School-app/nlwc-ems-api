using System.ComponentModel.DataAnnotations;

namespace nlwc.ems.Dtos;

public class CreateEditionRequest
{
    /// <summary>The template's code: <c>BB</c>, <c>SS</c> or <c>CP</c>.</summary>
    [Required, MaxLength(10)]
    public string TemplateCode { get; set; } = string.Empty;

    [Required, MaxLength(100), RegularExpression("^[a-z0-9]+(-[a-z0-9]+)*$", ErrorMessage = "Slug must be lower-case words joined by hyphens.")]
    public string Slug { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateOnly? StartsOn { get; set; }

    [Required]
    public DateOnly? EndsOn { get; set; }

    [MaxLength(200)]
    public string? VenueName { get; set; }

    [MaxLength(500)]
    public string? VenueAddress { get; set; }

    [Range(0, 120)]
    public int? CampMinAge { get; set; }

    [Range(0, 120)]
    public int? CampMaxAge { get; set; }

    [Range(0, 100000)]
    public int? CampCapacity { get; set; }

    public TimeOnly? PickupWindowStart { get; set; }
    public TimeOnly? PickupWindowEnd { get; set; }
}

public class UpdateEditionRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Range(0, 120)]
    public int? CampMinAge { get; set; }

    [Range(0, 120)]
    public int? CampMaxAge { get; set; }

    [Range(0, 100000)]
    public int? CampCapacity { get; set; }

    public TimeOnly? PickupWindowStart { get; set; }
    public TimeOnly? PickupWindowEnd { get; set; }
}

public class SetCapabilityRequest
{
    [Required]
    public bool? Enabled { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }
}

public class TransitionEditionRequest
{
    [Required]
    public EditionStage? To { get; set; }

    /// <summary>Every lifecycle change is a dangerous action and records why.</summary>
    [Required, MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public class RateRuleInput
{
    [Range(0, 120)]
    public int AgeMin { get; set; }

    /// <summary>Null = no upper bound.</summary>
    [Range(0, 120)]
    public int? AgeMax { get; set; }

    /// <summary>Null = any mode.</summary>
    public ParticipationMode? ParticipationMode { get; set; }

    [MaxLength(60)]
    public string? AccommodationCategory { get; set; }

    public RateKind Kind { get; set; }

    /// <summary>Integer kobo. Required for <c>Amount</c>, forbidden for <c>Free</c>.</summary>
    public long? AmountKobo { get; set; }
}

public class DraftRatesRequest
{
    [MaxLength(500)]
    public string? Note { get; set; }

    [Required]
    public List<RateRuleInput>? Rules { get; set; }
}

public class RateNoteRequest
{
    [MaxLength(500)]
    public string? Note { get; set; }
}

public class RateVersionCreatedDto
{
    public Guid VersionId { get; set; }
}

public class PublicVenueDto
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
}

public class PublicCampDto
{
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
}

public class PublicPickupWindowDto
{
    public TimeOnly Start { get; set; }
    public TimeOnly End { get; set; }
}

public class PublicPriceDto
{
    public PriceState State { get; set; }
}

public class PublicDayDto
{
    public DateOnly Day { get; set; }
    public string? Label { get; set; }
}

/// <summary>What the public may see of an edition: no ids, no camp capacity, no internal state.</summary>
public class PublicEditionDto
{
    /// <summary>Needed to start a registration; the public pages are addressed by slug.</summary>
    public Guid Id { get; set; }

    public string Slug { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Timezone { get; set; } = string.Empty;
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    public PublicVenueDto? Venue { get; set; }
    public PublicCampDto? Camp { get; set; }
    public PublicPickupWindowDto? PickupWindow { get; set; }
    public EditionStage? Stage { get; set; }
    public RegistrationWindow RegistrationWindow { get; set; }
    public IReadOnlyList<Capability> Capabilities { get; set; } = [];
    public PublicPriceDto Price { get; set; } = new();
    public IReadOnlyList<PublicDayDto> Days { get; set; } = [];
}

public class EditionSummaryDto
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    public EditionStage? Stage { get; set; }
}

public class RateVersionDto
{
    public Guid VersionId { get; set; }
    public int Version { get; set; }
    public RateVersionState State { get; set; }
    public Guid DraftedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? Note { get; set; }
    public List<RateRuleInput> Rules { get; set; } = [];
}

public class ComingSoonEventDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
