using System.Linq.Expressions;
using nlwc.ems.Core.Filters;

namespace nlwc.ems.Core.Models;

public class Role : BaseEntity, IFilterable<Role, RoleFilter>
{
    public string Key { get; set; } = string.Empty;

    [AuditDisplayName]
    public string Name { get; set; } = string.Empty;

    public ICollection<RolePermission> Permissions { get; set; } = [];

    public static Expression<Func<Role, bool>>? BuildPredicate(RoleFilter? filter)
    {
        if (filter is null) return null;
        Expression<Func<Role, bool>>? predicate = null;
        void Add(Expression<Func<Role, bool>> expr) => predicate = (predicate ?? PredicateBuilder.True<Role>()).And(expr);

        if (PredicateBuilder.HasText(filter.Key)) { var k = filter.Key!.Trim().ToLower(); Add(r => r.Key.ToLower().Contains(k)); }
        if (PredicateBuilder.HasText(filter.Name)) { var n = filter.Name!.Trim().ToLower(); Add(r => r.Name.ToLower().Contains(n)); }
        return predicate;
    }

    public static Expression<Func<Role, bool>>? BuildPredicate(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText)) return null;
        var text = searchText.Trim().ToLower();
        return r => r.Key.ToLower().Contains(text) || r.Name.ToLower().Contains(text);
    }
}

/// <summary>A role holds at most one level per resource; a higher level includes the lower ones.</summary>
public class RolePermission : BaseEntity
{
    public Guid RoleId { get; set; }
    public string Resource { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
}

/// <summary>A role given to an account, church-wide (<see cref="EditionId"/> null) or for one edition.</summary>
[Auditable]
public class RoleGrant : BaseEntity, IFilterable<RoleGrant, RoleGrantFilter>
{
    public Guid AccountId { get; set; }
    public Guid RoleId { get; set; }
    public Guid? EditionId { get; set; }
    public Guid GrantedBy { get; set; }

    [AuditDisplayName]
    public string Reason { get; set; } = string.Empty;

    public DateTime? ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public Role? Role { get; set; }

    public static Expression<Func<RoleGrant, bool>>? BuildPredicate(RoleGrantFilter? filter)
    {
        if (filter is null) return null;
        Expression<Func<RoleGrant, bool>>? predicate = null;
        void Add(Expression<Func<RoleGrant, bool>> expr) => predicate = (predicate ?? PredicateBuilder.True<RoleGrant>()).And(expr);

        if (filter.AccountId.HasValue) Add(g => g.AccountId == filter.AccountId.Value);
        if (filter.EditionId.HasValue) Add(g => g.EditionId == filter.EditionId.Value);
        if (filter.ActiveOnly == true) Add(g => g.RevokedAt == null && (g.ExpiresAt == null || g.ExpiresAt > DateTime.UtcNow));
        return predicate;
    }

    public static Expression<Func<RoleGrant, bool>>? BuildPredicate(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText)) return null;
        var text = searchText.Trim().ToLower();
        return g => g.Reason.ToLower().Contains(text);
    }
}

/// <summary>A registered check-in device, revocable when a phone is lost.</summary>
public class Device : BaseEntity
{
    public Guid AccountId { get; set; }
    public string Label { get; set; } = string.Empty;
    public DateTime? RevokedAt { get; set; }
}

/// <summary>Reads of sensitive data (health, collector lists, minors' contact details). Append-only.</summary>
public class AccessLog : BaseEntity
{
    public Guid ActorAccountId { get; set; }
    public string Resource { get; set; } = string.Empty;
    public Guid? SubjectPersonId { get; set; }
    public Guid? EditionId { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Append-only, hash-chained audit row. Deliberately not a <see cref="BaseEntity"/>: it is immutable, so it carries no
/// modified/status stamps, and its order is the identity <see cref="Seq"/> the chain is verified along.
/// </summary>
public class AuditEvent
{
    public long Seq { get; set; }
    public Guid Id { get; set; }
    public DateTime OccurredAt { get; set; }
    public Guid? ActorAccountId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? RecordId { get; set; }
    public string? DisplayName { get; set; }
    public Guid? EditionId { get; set; }
    public string? Reason { get; set; }

    /// <summary>The exact JSON text that was hashed (old/new values and any detail).</summary>
    public string Detail { get; set; } = "{}";

    public string PrevHash { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
}
