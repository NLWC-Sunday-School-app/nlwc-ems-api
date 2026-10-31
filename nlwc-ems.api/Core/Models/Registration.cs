namespace nlwc.ems.Core.Models;

public class Registration : BaseEntity
{
    /// <summary>Human-readable, e.g. <c>BB26-0412-A7</c>; generated from <see cref="ReferenceCounter"/>.</summary>
    public string Reference { get; set; } = string.Empty;

    public Guid EditionId { get; set; }
    public Guid PersonId { get; set; }
    public RegistrationState State { get; set; } = RegistrationState.Draft;
    public ParticipationMode ParticipationMode { get; set; }

    /// <summary>Snapshots taken when the registration is made, so later rule changes cannot rewrite history.</summary>
    public int? AgeOnReferenceDate { get; set; }

    public string? AgeBand { get; set; }

    /// <summary>The wizard's saved progress, kept server-side so a family can resume on another device.</summary>
    public string Draft { get; set; } = "{}";
}

public class RegistrationActor : BaseEntity
{
    public Guid RegistrationId { get; set; }
    public Guid PersonId { get; set; }
    public RegistrationRole Role { get; set; }
}

/// <summary>Append-only transition log.</summary>
public class RegistrationStatusHistory : BaseEntity
{
    public Guid RegistrationId { get; set; }
    public RegistrationState? FromState { get; set; }
    public RegistrationState ToState { get; set; }
    public Guid? ActorPersonId { get; set; }
    public string? Reason { get; set; }
}

public class Invitation : BaseEntity
{
    public Guid RegistrationId { get; set; }
    public Guid InvitedBy { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
}
