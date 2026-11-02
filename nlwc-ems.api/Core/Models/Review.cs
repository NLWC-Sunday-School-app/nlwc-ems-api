namespace nlwc.ems.Core.Models;

/// <summary>A person's decision on a camp request. Each decision is a new row; the latest one is current, so the trail stays.</summary>
[Auditable]
public class CampDecision : BaseEntity
{
    public Guid RegistrationId { get; set; }
    public CampDecisionKind Decision { get; set; }

    [AuditDisplayName]
    public string Reason { get; set; } = string.Empty;

    /// <summary>The staff member's person id.</summary>
    public Guid DecidedBy { get; set; }
}

/// <summary>Staff looked at two people who resemble each other and decided they are different. Stops the pair being offered again.</summary>
[Auditable]
public class DuplicateDismissal : BaseEntity
{
    /// <summary>Stored with the smaller id first so a pair has one row whichever way round it was reviewed.</summary>
    public Guid PersonAId { get; set; }
    public Guid PersonBId { get; set; }

    [AuditDisplayName]
    public string Reason { get; set; } = string.Empty;

    public Guid DismissedBy { get; set; }
}
