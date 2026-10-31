namespace NlwcEms.Api;

public sealed record AppConfig(
    string DatabaseUrl,
    byte[] MasterKey,
    string[] AllowedOrigins,
    bool CookieSecure)
{
    /// <summary>DATABASE_URL is an Npgsql connection string. MASTER_KEY is 32 bytes, hex.</summary>
    public static AppConfig FromEnvironment(Func<string, string?> get)
    {
        string Req(string k) => get(k) is { Length: > 0 } v ? v : throw new InvalidOperationException($"{k} is required");
        var key = Convert.FromHexString(Req("MASTER_KEY"));
        if (key.Length != 32) throw new InvalidOperationException("MASTER_KEY must be 32 bytes (64 hex chars)");
        return new AppConfig(
            Req("DATABASE_URL"),
            key,
            (get("ALLOWED_ORIGINS") ?? "http://localhost:3000,http://localhost:3001")
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
            (get("COOKIE_SECURE") ?? "true") == "true");
    }
}
