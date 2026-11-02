using System.Text;
using System.Net;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using nlwc.ems.Services.Messaging;
using nlwc.ems.Services.Options;

namespace nlwc.ems.Tests.Services;

public class EmailTests
{
    private sealed class FakeHandler(HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }
        public string Body { get; private set; } = "";
        public int Calls { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            Request = request;
            Body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent("{\"message\":\"Queued\"}") };
        }
    }

    private static readonly MailgunOptions On = new() { Domain = "mails.test", ApiKey = "key-123", FromName = "NLWC Events" };

    private static MailgunEmailService Service(FakeHandler handler, MailgunOptions options)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(handler));
        return new MailgunEmailService(factory.Object, Options.Create(options), NullLogger<MailgunEmailService>.Instance);
    }

    [Fact]
    public async Task Nothing_is_sent_while_mailgun_is_not_configured()
    {
        var handler = new FakeHandler();
        (await Service(handler, new MailgunOptions()).SendEmailAsync(new Mailer("a@example.com"), null, "Hi", "<p>x</p>")).Should().BeFalse();
        handler.Calls.Should().Be(0);
    }

    [Fact]
    public async Task A_message_goes_to_the_domain_with_basic_auth_and_the_default_sender()
    {
        var handler = new FakeHandler();
        (await Service(handler, On).SendEmailAsync(new Mailer("ada@example.com", "Ada Obi"), null, "Subject line", "<p>Hello</p>")).Should().BeTrue();

        handler.Request!.RequestUri!.ToString().Should().Be("https://api.mailgun.net/v3/mails.test/messages");
        handler.Request.Method.Should().Be(HttpMethod.Post);
        handler.Request.Headers.Authorization!.Scheme.Should().Be("Basic");
        Encoding.ASCII.GetString(Convert.FromBase64String(handler.Request.Headers.Authorization.Parameter!)).Should().Be("api:key-123");
        handler.Body.Should().Contain("NLWC Events <noreply@mails.test>").And.Contain("Ada Obi <ada@example.com>").And.Contain("Subject line").And.Contain("<p>Hello</p>");
    }

    [Fact]
    public async Task A_named_sender_overrides_the_default_and_attachments_keep_their_type()
    {
        var handler = new FakeHandler();
        await Service(handler, On).SendEmailAsync(new Mailer("a@example.com"), new Mailer("events@mails.test", "Events Team"), "S", "<p>x</p>",
            new Dictionary<string, byte[]> { ["pass.pdf"] = [1, 2, 3], ["notes.bin"] = [4] });
        handler.Body.Should().Contain("Events Team <events@mails.test>").And.Contain("application/pdf").And.Contain("application/octet-stream").And.Contain("pass.pdf");
    }

    [Fact]
    public async Task A_refusal_from_mailgun_is_reported_as_not_sent() => (await Service(new FakeHandler(HttpStatusCode.Forbidden), On)
        .SendEmailAsync(new Mailer("a@example.com"), null, "S", "<p>x</p>")).Should().BeFalse();

    [Fact]
    public async Task A_list_is_sent_one_message_per_recipient()
    {
        var handler = new FakeHandler();
        await Service(handler, On).SendEmailAsync([new Mailer("a@example.com"), new Mailer("b@example.com")], null, "S", "<p>x</p>");
        handler.Calls.Should().Be(2);
    }

    [Fact]
    public void A_display_name_cannot_inject_header_lines() => new Mailer("a@example.com", "Ada\r\nBcc: x@evil.test \"Obi\"").Format().Should().NotContain("\r").And.NotContain("\n").And.NotContain("\"");

    // ---- templates ----
    private static (EmailTemplateMailer Sut, Mock<IEmailService> Email) Templates(MailgunOptions options)
    {
        var email = new Mock<IEmailService>();
        email.Setup(e => e.SendEmailAsync(It.IsAny<Mailer>(), It.IsAny<Mailer?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Dictionary<string, byte[]>?>())).ReturnsAsync(true);
        var env = new Mock<IHostEnvironment>();
        env.SetupGet(e => e.EnvironmentName).Returns("Production");
        return (new EmailTemplateMailer(email.Object, Options.Create(options), env.Object, NullLogger<EmailTemplateMailer>.Instance), email);
    }

    [Fact]
    public async Task The_verification_email_links_to_the_portal_with_the_token_encoded()
    {
        var (sut, email) = Templates(new MailgunOptions { Domain = "d", ApiKey = "k", WebBaseUrl = "https://events.test/" });
        await sut.SendAsync("ada@example.com", "verify_email", new Dictionary<string, string> { ["token"] = "a+b/c=" });
        email.Verify(e => e.SendEmailAsync(It.Is<Mailer>(m => m.Address == "ada@example.com"), null, "Confirm your email address",
            It.Is<string>(html => html.Contains("https://events.test/verify-email?token=a%2Bb%2Fc%3D")), null), Times.Once);
    }

    [Fact]
    public async Task The_account_exists_email_points_to_sign_in()
    {
        var (sut, email) = Templates(new MailgunOptions { Domain = "d", ApiKey = "k", WebBaseUrl = "https://events.test" });
        await sut.SendAsync("ada@example.com", "account_exists", new Dictionary<string, string>());
        email.Verify(e => e.SendEmailAsync(It.IsAny<Mailer>(), null, It.IsAny<string>(), It.Is<string>(h => h.Contains("https://events.test/login")), null), Times.Once);
    }

    [Fact]
    public async Task Nothing_is_sent_for_an_unknown_template_or_while_mailgun_is_off()
    {
        var (on, onEmail) = Templates(new MailgunOptions { Domain = "d", ApiKey = "k" });
        await on.SendAsync("a@example.com", "no_such_template", new Dictionary<string, string>());
        var (off, offEmail) = Templates(new MailgunOptions());
        await off.SendAsync("a@example.com", "verify_email", new Dictionary<string, string> { ["token"] = "t" });
        onEmail.VerifyNoOtherCalls();
        offEmail.VerifyNoOtherCalls();
    }
}

public class MailgunSenderTests
{
    [Theory]
    [InlineData("", "noreply@mails.test", "NLWC Events")]
    [InlineData("events@nlwc.church", "events@nlwc.church", "NLWC Events")]
    [InlineData("NLWC Events <events@nlwc.church>", "events@nlwc.church", "NLWC Events")]
    [InlineData("Events Team <events@nlwc.church>", "events@nlwc.church", "Events Team")]
    public void The_sender_is_one_address_and_one_name_whichever_form_the_setting_takes(string setting, string address, string name)
        => new MailgunOptions { Domain = "mails.test", FromAddress = setting, FromName = "NLWC Events" }.ResolveSender().Should().Be((address, name));
}
