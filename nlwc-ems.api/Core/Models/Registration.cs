using System.Text.Json.Serialization;

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

/// <summary>Append-only. A changed answer is a new row; the latest per (registration, kind) is the current one.</summary>
public class ConsentRecord : BaseEntity
{
    public Guid RegistrationId { get; set; }

    /// <summary>The person the consent is about (the child, or the adult themself).</summary>
    public Guid PersonId { get; set; }

    public ConsentKind Kind { get; set; }
    public bool Accepted { get; set; }

    /// <summary>e.g. <c>BB26-media</c>. Wording is versioned content; version 0 means no approved wording existed yet.</summary>
    public string DocumentId { get; set; } = string.Empty;

    public int DocumentVersion { get; set; }

    /// <summary>Who answered: the guardian for a child, or the person themself.</summary>
    public Guid AnsweredBy { get; set; }
}

/// <summary>Restricted. The answers are encrypted at rest; <see cref="HasFlags"/> is the only part broad roles may see.</summary>
public class HealthDisclosure : BaseEntity
{
    public Guid RegistrationId { get; set; }

    [JsonIgnore, TrailableIgnore]
    public byte[] DataEncrypted { get; set; } = [];

    /// <summary>True when any condition, allergy or note was given.</summary>
    public bool HasFlags { get; set; }
}
