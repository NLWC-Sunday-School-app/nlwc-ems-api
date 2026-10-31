using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;

namespace nlwc.ems.Core.Security;

/// <summary>Records that someone was refused. Sensitive-access attempts are part of the safeguarding trail.</summary>
public static class AccessDenialAudit
{
    public static Task RecordAsync(HttpContext http, string code, string? permission = null)
    {
        var publisher = http.RequestServices.GetRequiredService<IAuditEventPublisher>();
        var raw = http.Request.RouteValues["editionId"]?.ToString() ?? http.Request.Query["editionId"].ToString();
        return publisher.PublishAsync(new EntityAuditEvent
        {
            EntityType = "Access", Action = "access.denied", UserId = http.User.GetUserIdOrNull(), IpAddress = http.Connection.RemoteIpAddress?.ToString(),
            EditionId = Guid.TryParse(raw, out var edition) ? edition : null,
            Detail = new { code, permission, method = http.Request.Method, path = http.Request.Path.Value }
        });
    }
}

/// <summary>Audits refusals made by <see cref="RequirePermissionAttribute"/> (the authorization middleware, before an action runs)
/// and answers with the standard envelope, naming MFA only when MFA is what is missing.</summary>
public class EmsAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Forbidden)
        {
            var codes = policy.Requirements.OfType<PermissionRequirement>().Select(r => r.Code).ToList();
            var raw = context.Request.RouteValues["editionId"]?.ToString() ?? context.Request.Query["editionId"].ToString();
            var denial = context.User.DenialCode(codes, Guid.TryParse(raw, out var edition) ? edition : null);

            await AccessDenialAudit.RecordAsync(context, denial, string.Join(",", codes));
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(ApiResponse.Fail(
                denial == "mfa_required" ? "Confirm your authenticator code to continue." : "You do not have permission to do this.", denial));
            return;
        }
        await _default.HandleAsync(next, context, policy, authorizeResult);
    }
}

/// <summary>Audits refusals made by a controller's own permission checks (<c>AuthorizeView</c>/<c>AuthorizePermission</c>).</summary>
public class AuditDenialFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();
        if (executed.Result is ObjectResult { StatusCode: StatusCodes.Status403Forbidden, Value: ApiResponse body })
            await AccessDenialAudit.RecordAsync(context.HttpContext, body.Code ?? "forbidden");
    }
}
