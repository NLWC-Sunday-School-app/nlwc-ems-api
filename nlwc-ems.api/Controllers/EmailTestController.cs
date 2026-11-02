using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using nlwc.ems.Core.Security;
using nlwc.ems.Dtos;
using nlwc.ems.Services.Messaging;
using nlwc.ems.Services.Options;

namespace nlwc.ems.Controllers;

/// <summary>A staff-only check that outbound email works. It is never anonymous: an open "send to anyone" route would let strangers
/// spend the church's Mailgun account on spam. The recipient is one address in the request, and each use is audited.</summary>
[ApiController]
[Authorize]
[Route("api/v1/admin/email")]
public class EmailTestController(IEmailService email, IOptions<MailgunOptions> options, IAuditEventPublisher auditPublisher) : ControllerBase
{
    [HttpPost("test")]
    public async Task<ActionResult<ApiResponse<EmailTestDto>>> Test([FromBody] EmailTestRequest request)
    {
        if (!User.HasPermissionWithMfa(EmsPermissions.Staff.Edit))
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("You do not have permission to do this.", User.DenialCode([EmsPermissions.Staff.Edit])));

        var settings = options.Value;
        var to = request.To.Trim();
        var sent = settings.Enabled && await email.SendEmailAsync(new Mailer(to), null, "NLWC Events: email test",
            "<div style=\"font-family:Arial,sans-serif\"><h2>Email is working</h2><p>This is a test message from the NLWC Events API. If you can read it, outbound email is set up correctly.</p></div>");

        await auditPublisher.PublishAsync(new EntityAuditEvent
        {
            EntityType = "Email", RecordId = null, DisplayName = "Email test", Action = "email.test", Reason = sent ? "sent" : settings.Enabled ? "provider refused" : "not configured"
        });
        return Ok(ApiResponse<EmailTestDto>.Ok(
            new EmailTestDto { Configured = settings.Enabled, Sent = sent, To = to, From = settings.Enabled ? new Mailer(settings.ResolveSender().Address, settings.ResolveSender().Name).Format() : null },
            sent ? "Test email sent." : settings.Enabled ? "Mailgun did not accept the message. Check the API logs." : "Mailgun is not configured. Set Mailgun:Domain and Mailgun:ApiKey."));
    }
}
