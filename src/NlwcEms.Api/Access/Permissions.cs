namespace NlwcEms.Api.Access;

/// <summary>Each resource has ordered levels; a higher level includes the lower ones.</summary>
public static class Permissions
{
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
}
