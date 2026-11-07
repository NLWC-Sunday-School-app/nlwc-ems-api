using System.Text.Json.Serialization;

namespace nlwc.ems.Core.Models;

public class PersonRelationship : BaseEntity
{
    public Guid FromPersonId { get; set; }
    public Guid ToPersonId { get; set; }
    public RelationshipKind Kind { get; set; }
}

/// <summary>A guardian's authority over a dependant. Separate from who pays and from who may collect.</summary>
[Auditable]
public class GuardianAuthorization : BaseEntity
{
    public Guid GuardianPersonId { get; set; }
    public Guid DependantPersonId { get; set; }

    [AuditDisplayName]
    public string DeclaredRelationship { get; set; } = string.Empty;

    public DateTime? RevokedAt { get; set; }
}

/// <summary>An adult named to collect a child. The phone is encrypted at rest.</summary>
[Auditable]
public class AuthorizedCollector : BaseEntity
{
    public Guid ChildPersonId { get; set; }

    [AuditDisplayName]
    public string FullName { get; set; } = string.Empty;

    public string Relationship { get; set; } = string.Empty;

    [JsonIgnore, TrailableIgnore]
    public byte[] PhoneEncrypted { get; set; } = [];

    /// <summary>Null = valid on every event day.</summary>
    public List<DateOnly>? ValidDays { get; set; }

    public DateTime? RemovedAt { get; set; }
}

/// <summary>Someone to call about any child in a family. <see cref="PersonId"/> is the account holder the family hangs off.</summary>
public class EmergencyContact : BaseEntity
{
    public Guid PersonId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;

    [JsonIgnore, TrailableIgnore]
    public byte[] PhoneEncrypted { get; set; } = [];
}
