namespace nlwc.ems.Base.Core.Audit;

public static class AuditAction
{
    public const string Create = "Create";
    public const string Update = "Update";
    public const string Delete = "Delete";
}

/// <summary>One audit record. <see cref="Action"/> is <see cref="AuditAction"/> for generic entity changes, or a
/// dotted domain action (<c>edition.stage</c>, <c>access.denied</c>) for events that carry a reason.</summary>
public class EntityAuditEvent
{
    public string EntityType { get; set; } = string.Empty;
    public string? RecordId { get; set; }
    public string? DisplayName { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid? EditionId { get; set; }
    public string? Reason { get; set; }
    public string? UserId { get; set; }
    public string? IpAddress { get; set; }
    public IReadOnlyDictionary<string, object?>? OldValues { get; set; }
    public IReadOnlyDictionary<string, object?>? NewValues { get; set; }
    public object? Detail { get; set; }
}
