using System.Linq.Expressions;
using nlwc.ems.Core.Filters;

namespace nlwc.ems.Core.Models;

public class Organization : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}

public class EventTemplate : BaseEntity
{
    public Guid OrganizationId { get; set; }

    /// <summary><c>BB</c>, <c>SS</c>, <c>CP</c>.</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string Config { get; set; } = "{}";
}

public class Venue : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
}

[Auditable]
public class EventEdition : BaseEntity, IFilterable<EventEdition, EditionFilter>
{
    public Guid TemplateId { get; set; }
    public Guid? VenueId { get; set; }
    public string Slug { get; set; } = string.Empty;

    /// <summary>Short code used in references, e.g. <c>BB26</c>.</summary>
    public string Code { get; set; } = string.Empty;

    [AuditDisplayName]
    public string Name { get; set; } = string.Empty;

    public string Timezone { get; set; } = "Africa/Lagos";
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }

    /// <summary>Age bands and eligibility are evaluated on this date. Defaults to <see cref="StartsOn"/>.</summary>
    public DateOnly AgeReferenceDate { get; set; }

    public int? CampMinAge { get; set; }
    public int? CampMaxAge { get; set; }
    public int? CampCapacity { get; set; }
    public TimeOnly? PickupWindowStart { get; set; }
    public TimeOnly? PickupWindowEnd { get; set; }

    public EditionVisibility Visibility { get; set; } = EditionVisibility.Draft;
    public RegistrationWindow RegistrationWindow { get; set; } = RegistrationWindow.NotOpen;
    public EditionLiveState LiveState { get; set; } = EditionLiveState.NotLive;

    public Venue? Venue { get; set; }
    public ICollection<EventDay> Days { get; set; } = [];
    public ICollection<EditionCapability> Capabilities { get; set; } = [];

    /// <summary>The admin-facing lifecycle stage derived from the three stored fields.</summary>
    public EditionStage? Stage => EditionLifecycle.StageOf(Visibility, RegistrationWindow, LiveState);

    public static Expression<Func<EventEdition, bool>>? BuildPredicate(EditionFilter? filter)
    {
        if (filter is null) return null;
        Expression<Func<EventEdition, bool>>? predicate = null;
        void Add(Expression<Func<EventEdition, bool>> expr) => predicate = (predicate ?? PredicateBuilder.True<EventEdition>()).And(expr);

        if (PredicateBuilder.HasText(filter.Code)) { var c = filter.Code!.Trim().ToLower(); Add(e => e.Code.ToLower().Contains(c)); }
        if (PredicateBuilder.HasText(filter.Name)) { var n = filter.Name!.Trim().ToLower(); Add(e => e.Name.ToLower().Contains(n)); }
        if (filter.Visibility.HasValue) Add(e => e.Visibility == filter.Visibility.Value);
        if (filter.RegistrationWindow.HasValue) Add(e => e.RegistrationWindow == filter.RegistrationWindow.Value);
        return predicate;
    }

    public static Expression<Func<EventEdition, bool>>? BuildPredicate(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText)) return null;
        var text = searchText.Trim().ToLower();
        return e => e.Code.ToLower().Contains(text) || e.Name.ToLower().Contains(text) || e.Slug.ToLower().Contains(text);
    }
}

/// <summary>Per-edition switch. A disabled capability removes its screens and its API for that edition.</summary>
public class EditionCapability : BaseEntity
{
    public Guid EditionId { get; set; }
    public Capability Capability { get; set; }
    public bool Enabled { get; set; }
}

public class EventDay : BaseEntity
{
    public Guid EditionId { get; set; }
    public DateOnly Day { get; set; }
    public string? Label { get; set; }
}

public class EditionContent : BaseEntity
{
    public Guid EditionId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Body { get; set; } = "{}";
    public DateTime? PublishedAt { get; set; }
}

/// <summary>A versioned legal document. An unapproved version blocks public launch.</summary>
public class PolicyVersion : BaseEntity
{
    public string DocumentId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime? ApprovedAt { get; set; }
}

/// <summary>Per-edition counter behind human-readable references; never derived from a primary key.</summary>
public class ReferenceCounter : BaseEntity
{
    public Guid EditionId { get; set; }
    public long LastValue { get; set; }
}
