namespace nlwc.ems.Services.Options;

/// <summary>Outbound email through Mailgun. The API key is a secret: set it with an environment variable or user-secrets, never in a committed file.</summary>
public class MailgunOptions
{
    public const string SectionName = "Mailgun";

    /// <summary>The sending domain verified in Mailgun, e.g. <c>mails.nlwc.church</c>.</summary>
    public string Domain { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Defaults to <c>noreply@{Domain}</c> when empty.</summary>
    public string FromAddress { get; set; } = string.Empty;

    public string FromName { get; set; } = "NLWC Events";

    /// <summary>Use <c>https://api.eu.mailgun.net/v3</c> for a domain created in Mailgun's EU region.</summary>
    public string BaseUrl { get; set; } = "https://api.mailgun.net/v3";

    /// <summary>Where the portal lives, for the links inside emails (no trailing slash).</summary>
    public string WebBaseUrl { get; set; } = "https://events.nlwc.church";

    public bool Enabled => !string.IsNullOrWhiteSpace(Domain) && !string.IsNullOrWhiteSpace(ApiKey);

    /// <summary>The sender to use. <see cref="FromAddress"/> may be a bare address (<c>events@nlwc.church</c>) or already carry a name
    /// (<c>NLWC Events &lt;events@nlwc.church&gt;</c>); either way the result is one address plus one display name.</summary>
    public (string Address, string Name) ResolveSender()
    {
        if (string.IsNullOrWhiteSpace(FromAddress)) return ($"noreply@{Domain}", FromName);
        return System.Net.Mail.MailAddress.TryCreate(FromAddress.Trim(), out var parsed)
            ? (parsed.Address, string.IsNullOrWhiteSpace(parsed.DisplayName) ? FromName : parsed.DisplayName)
            : (FromAddress.Trim(), FromName);
    }
}
