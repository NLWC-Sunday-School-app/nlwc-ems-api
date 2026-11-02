namespace nlwc.ems.Services.Messaging;

/// <summary>An email participant: an address and an optional display name.</summary>
public record Mailer(string Address, string? Name = null)
{
    /// <summary><c>Name &lt;address&gt;</c>, or just the address when there is no name. Quotes and line breaks are stripped so a name can't break the header.</summary>
    public string Format()
    {
        var name = Name?.Replace("\"", "").Replace("\r", " ").Replace("\n", " ").Trim();
        return string.IsNullOrEmpty(name) ? Address.Trim() : $"{name} <{Address.Trim()}>";
    }
}

public interface IEmailService
{
    /// <summary>Sends one email. False means it was not sent (not configured, or the provider refused it); the reason is logged.
    /// <paramref name="emailfrom"/> may be null to use the configured sender. Attachments are file name to bytes.</summary>
    Task<bool> SendEmailAsync(Mailer emailto, Mailer? emailfrom, string subject, string htmlcontent, Dictionary<string, byte[]>? attachments = null);

    /// <summary>Sends the same email to each recipient separately, so nobody sees the others' addresses.</summary>
    Task SendEmailAsync(List<Mailer> emailto, Mailer? emailfrom, string subject, string htmlcontent, Dictionary<string, byte[]>? attachments = null);
}
