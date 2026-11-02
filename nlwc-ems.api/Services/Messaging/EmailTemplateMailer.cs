using System.Net;
using Microsoft.Extensions.Options;
using nlwc.ems.Services.Interfaces;
using nlwc.ems.Services.Options;

namespace nlwc.ems.Services.Messaging;

/// <summary>Turns the app's named messages (<c>verify_email</c>, <c>account_exists</c>) into emails and sends them through <see cref="IEmailService"/>.
/// With Mailgun not configured it only logs, so local development and CI never need a provider account.</summary>
public class EmailTemplateMailer(IEmailService email, IOptions<MailgunOptions> options, IHostEnvironment environment, ILogger<EmailTemplateMailer> logger) : IMailer
{
    public async Task SendAsync(string to, string template, IReadOnlyDictionary<string, string> data)
    {
        var message = Render(template, data);
        if (message is null)
        {
            logger.LogWarning("No email template named {Template}; nothing was sent to {To}.", template, to);
            return;
        }
        if (!options.Value.Enabled)
        {
            if (environment.IsDevelopment()) logger.LogInformation("[mail] {Template} -> {To} {Data}", template, to, data);
            return;
        }
        await email.SendEmailAsync(new Mailer(to), null, message.Value.Subject, message.Value.Html);
    }

    private (string Subject, string Html)? Render(string template, IReadOnlyDictionary<string, string> data)
    {
        var web = options.Value.WebBaseUrl.TrimEnd('/');
        switch (template)
        {
            case "verify_email" when data.TryGetValue("token", out var token):
                var link = $"{web}/verify-email?token={Uri.EscapeDataString(token)}";
                return ("Confirm your email address", Layout("Confirm your email",
                    $"<p>Thanks for signing up for NLWC Events. Please confirm your email address to finish setting up your account.</p>{Button(link, "Confirm my email")}" +
                    $"<p style=\"color:#666;font-size:13px\">Or paste this link into your browser:<br>{WebUtility.HtmlEncode(link)}</p>" +
                    "<p style=\"color:#666;font-size:13px\">If you didn't sign up, you can ignore this email.</p>"));
            case "account_exists":
                return ("You already have an NLWC Events account", Layout("You already have an account",
                    $"<p>Someone just tried to sign up with this email address, but it already has an account.</p>{Button($"{web}/login", "Sign in")}" +
                    "<p style=\"color:#666;font-size:13px\">If this wasn't you, you don't need to do anything.</p>"));
            default:
                return null;
        }
    }

    private static string Button(string url, string label)
        => $"<p><a href=\"{WebUtility.HtmlEncode(url)}\" style=\"display:inline-block;background:#b83a20;color:#fff;text-decoration:none;padding:12px 20px;border-radius:6px;font-weight:600\">{WebUtility.HtmlEncode(label)}</a></p>";

    private static string Layout(string heading, string body)
        => $"<div style=\"font-family:Arial,Helvetica,sans-serif;max-width:520px;margin:0 auto;padding:24px;color:#1a1a1a\"><h2 style=\"margin:0 0 16px\">{WebUtility.HtmlEncode(heading)}</h2>{body}" +
           "<hr style=\"border:0;border-top:1px solid #e5e0da;margin:24px 0\"><p style=\"color:#888;font-size:12px\">The New and Living Way Church</p></div>";
}
