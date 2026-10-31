namespace nlwc.ems.Core.Models;

public class RatePlan : BaseEntity
{
    public Guid EditionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<RatePlanVersion> Versions { get; set; } = [];
}

/// <summary>Draft, approved, published, retired. Published and retired versions are immutable (a database trigger
/// enforces it), and the drafter cannot be the approver.</summary>
[Auditable]
public class RatePlanVersion : BaseEntity
{
    public Guid PlanId { get; set; }
    public int Version { get; set; }
    public RateVersionState State { get; set; } = RateVersionState.Draft;
    public Guid DraftedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? PublishedAt { get; set; }

    [AuditDisplayName]
    public string? Note { get; set; }

    public ICollection<RateRule> Rules { get; set; } = [];
}

public class RateRule : BaseEntity
{
    public Guid VersionId { get; set; }
    public int AgeMin { get; set; }

    /// <summary>Null = no upper bound.</summary>
    public int? AgeMax { get; set; }

    /// <summary>Null = any mode.</summary>
    public ParticipationMode? ParticipationMode { get; set; }

    public string? AccommodationCategory { get; set; }
    public RateKind Kind { get; set; }

    /// <summary>Integer kobo. Set exactly when <see cref="Kind"/> is <see cref="RateKind.Amount"/>.</summary>
    public long? AmountKobo { get; set; }
}
