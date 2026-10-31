namespace nlwc.ems.Base.Core.Audit;

/// <summary>Class-level: Create/Update/Delete of this entity are published as audit events by <c>BaseDao</c>.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class AuditableAttribute : Attribute;

/// <summary>Marks the property surfaced as the record's human-readable name in the audit trail.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class AuditDisplayNameAttribute : Attribute;

/// <summary>Keeps a property out of audit snapshots (secrets, encrypted blobs, personal data).</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class TrailableIgnoreAttribute : Attribute;
