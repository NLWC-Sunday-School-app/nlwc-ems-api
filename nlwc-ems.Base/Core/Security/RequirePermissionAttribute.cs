using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace nlwc.ems.Base.Core.Security;

/// <summary>
/// Gates an action behind a <c>resource:level</c> permission (see <see cref="EmsClaimTypes.Permissions"/>). The edition
/// scope is read from the <c>editionId</c> route value, then the <c>editionId</c> query string. Staff permissions only
/// count on an MFA-verified session. Resolved through <see cref="DynamicPermissionPolicyProvider"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "RequirePermission:";

    public RequirePermissionAttribute(string code) => Policy = PolicyPrefix + code;
}

public sealed record PermissionRequirement(string Code) : IAuthorizationRequirement;

public sealed class DynamicPermissionPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(RequirePermissionAttribute.PolicyPrefix, StringComparison.Ordinal))
            return base.GetPolicyAsync(policyName);

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(policyName[RequirePermissionAttribute.PolicyPrefix.Length..]))
            .Build();
        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}

public sealed class PermissionAuthorizationHandler(IHttpContextAccessor accessor) : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var http = accessor.HttpContext;
        var raw = http?.Request.RouteValues["editionId"]?.ToString() ?? http?.Request.Query["editionId"].ToString();
        Guid? edition = Guid.TryParse(raw, out var id) ? id : null;
        if (context.User.HasPermissionWithMfa(requirement.Code, edition)) context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
