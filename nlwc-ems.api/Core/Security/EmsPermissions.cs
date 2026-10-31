namespace nlwc.ems.Core.Security;

/// <summary>
/// Permission codes (<c>resource:level</c>) for <see cref="RequirePermissionAttribute"/> and the controllers' permission
/// overrides. Each resource has ordered levels and a higher level includes the lower ones, so a role holds one level per
/// resource and <see cref="Expand"/> lists every code that level carries.
/// </summary>
public static class EmsPermissions
{
    public static class Pricing { public const string Draft = "pricing:draft"; public const string Approve = "pricing:approve"; }
    public static class Money { public const string View = "money:view"; public const string Edit = "money:edit"; }
    public static class Health { public const string Flags = "health:flags"; public const string View = "health:view"; public const string Full = "health:full"; }
    public static class Custody { public const string Scan = "custody:scan"; public const string Act = "custody:act"; public const string View = "custody:view"; }
    public static class Config { public const string View = "config:view"; public const string Edit = "config:edit"; }
    public static class Registrations { public const string View = "registrations:view"; public const string Edit = "registrations:edit"; }
    public static class People { public const string View = "people:view"; public const string Edit = "people:edit"; }
    public static class Staff { public const string View = "staff:view"; public const string Edit = "staff:edit"; }
    public static class Audit { public const string View = "audit:view"; }
    public static class Communications { public const string View = "communications:view"; public const string Edit = "communications:edit"; }

    private static readonly Dictionary<string, string[]> Levels = new()
    {
        ["pricing"] = ["draft", "approve"],
        ["money"] = ["view", "edit"],
        ["health"] = ["flags", "view", "full"],
        ["custody"] = ["scan", "act", "view"],
        ["config"] = ["view", "edit"],
        ["registrations"] = ["view", "edit"],
        ["people"] = ["view", "edit"],
        ["staff"] = ["view", "edit"],
        ["audit"] = ["view"],
        ["communications"] = ["view", "edit"],
    };

    public static bool Known(string resource, string level) => Levels.TryGetValue(resource, out var l) && l.Contains(level);

    /// <summary>True when <paramref name="held"/> is at least <paramref name="required"/> for the resource.</summary>
    public static bool Satisfies(string resource, string held, string required)
    {
        if (!Levels.TryGetValue(resource, out var l)) return false;
        var h = Array.IndexOf(l, held);
        var r = Array.IndexOf(l, required);
        return h >= 0 && r >= 0 && h >= r;
    }

    /// <summary>Every <c>resource:level</c> code a held level carries: <c>health:full</c> gives <c>health:full</c>,
    /// <c>health:view</c> and <c>health:flags</c>.</summary>
    public static IEnumerable<string> Expand(string resource, string level)
    {
        if (!Levels.TryGetValue(resource, out var l)) yield break;
        var held = Array.IndexOf(l, level);
        for (var i = 0; i <= held; i++) yield return $"{resource}:{l[i]}";
    }
}
