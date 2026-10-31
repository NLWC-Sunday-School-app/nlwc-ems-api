namespace NlwcEms.Api.Catalog;

/// <summary>
/// Admin-facing lifecycle (Draft, Preview, Registration open, Closed, Live, Completed, Archived) mapped onto
/// three separate stored fields: visibility, registration window and live state (GAP-02).
/// </summary>
public static class Lifecycle
{
    public sealed record Fields(string Visibility, string Registration, string Live);

    public static readonly IReadOnlyDictionary<string, Fields> Stages = new Dictionary<string, Fields>
    {
        ["draft"] = new("draft", "not_open", "not_live"),
        ["preview"] = new("preview", "not_open", "not_live"),
        ["registration_open"] = new("public", "open", "not_live"),
        ["closed"] = new("public", "closed", "not_live"),
        ["live"] = new("public", "closed", "live"),
        ["completed"] = new("public", "closed", "completed"),
        ["archived"] = new("archived", "closed", "completed"),
    };

    private static readonly Dictionary<string, string[]> Allowed = new()
    {
        ["draft"] = ["preview"],
        ["preview"] = ["draft", "registration_open"],
        ["registration_open"] = ["closed"],
        ["closed"] = ["registration_open", "live"],
        ["live"] = ["completed"],
        ["completed"] = ["archived"],
        ["archived"] = [],
    };

    public static string? StageOf(string visibility, string registration, string live) =>
        Stages.FirstOrDefault(s => s.Value == new Fields(visibility, registration, live)).Key;

    public static bool CanMove(string from, string to) => Allowed.TryGetValue(from, out var n) && n.Contains(to);
}
