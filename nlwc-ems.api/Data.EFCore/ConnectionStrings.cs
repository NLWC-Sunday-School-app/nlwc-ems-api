using System.Text.RegularExpressions;

namespace nlwc.ems.Data.EFCore;

public static partial class ConnectionStrings
{
    /// <summary>Connection strings copied from other MySQL apps (the Registrar uses MySql.Data) spell the SSL mode differently from
    /// MySqlConnector, which throws on an unknown value before the app can start. Maps <c>Require</c> to <c>Required</c> and
    /// <c>Disabled</c> to <c>None</c>; everything else passes through.</summary>
    public static string? Normalize(string? connectionString)
        => connectionString is null ? null : SslModeValue().Replace(connectionString, m => m.Groups["v"].Value.ToLowerInvariant() switch
        {
            "require" => $"{m.Groups["k"].Value}Required",
            "disabled" => $"{m.Groups["k"].Value}None",
            _ => m.Value
        });

    [GeneratedRegex(@"(?<k>ssl[-_ ]?mode\s*=\s*)(?<v>require|disabled)(?=\s*(;|$))", RegexOptions.IgnoreCase)]
    private static partial Regex SslModeValue();
}
