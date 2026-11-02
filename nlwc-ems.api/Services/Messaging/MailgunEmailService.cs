using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;
using nlwc.ems.Services.Options;

namespace nlwc.ems.Services.Messaging;

public class MailgunEmailService(IHttpClientFactory httpClientFactory, IOptions<MailgunOptions> options, ILogger<MailgunEmailService> logger) : IEmailService
{
    public async Task<bool> SendEmailAsync(Mailer emailto, Mailer? emailfrom, string subject, string htmlcontent, Dictionary<string, byte[]>? attachments = null)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogWarning("Email to {To} was not sent: Mailgun is not configured (Mailgun:Domain and Mailgun:ApiKey).", emailto.Address);
            return false;
        }

        var from = emailfrom is { Address.Length: > 0 } ? emailfrom : ToMailer(settings.ResolveSender());
        using var multipart = new MultipartFormDataContent
        {
            { new StringContent(from.Format()), "from" },
            { new StringContent(emailto.Format()), "to" },
            { new StringContent(subject), "subject" },
            { new StringContent(htmlcontent), "html" }
        };
        foreach (var (fileName, bytes) in attachments ?? [])
        {
            var file = new ByteArrayContent(bytes);
            file.Headers.ContentType = new MediaTypeHeaderValue(ContentTypeFor(fileName));
            multipart.Add(file, "attachment", fileName);
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{settings.BaseUrl.TrimEnd('/')}/{settings.Domain}/messages") { Content = multipart };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"api:{settings.ApiKey}")));

        try
        {
            using var response = await httpClientFactory.CreateClient().SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Mailgun refused the email to {To}: {StatusCode} {Body}", emailto.Address, (int)response.StatusCode, await response.Content.ReadAsStringAsync());
                return false;
            }
            logger.LogInformation("Email sent to {To}", emailto.Address);
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "Could not reach Mailgun to send the email to {To}", emailto.Address);
            return false;
        }
    }

    public async Task SendEmailAsync(List<Mailer> emailto, Mailer? emailfrom, string subject, string htmlcontent, Dictionary<string, byte[]>? attachments = null)
    {
        foreach (var recipient in emailto) await SendEmailAsync(recipient, emailfrom, subject, htmlcontent, attachments);
    }

    private static Mailer ToMailer((string Address, string Name) sender) => new(sender.Address, sender.Name);

    private static string ContentTypeFor(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".csv" => "text/csv",
        ".txt" => "text/plain",
        _ => "application/octet-stream"
    };
}
