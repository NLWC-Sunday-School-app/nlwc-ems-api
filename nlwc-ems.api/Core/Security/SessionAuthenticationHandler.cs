using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace nlwc.ems.Core.Security;

/// <summary>
/// Authenticates the opaque session cookie. Resolves the server-side session, then exposes the caller (account, person,
/// surface, MFA state) and their expanded permission codes as claims, so <c>[Authorize]</c>, <see cref="RequirePermissionAttribute"/>
/// and the base controllers' permission checks all read the same thing. Revoking a session takes effect on the next request.
/// </summary>
public class SessionAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder,
    ISessionService sessionService) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "EmsSession";
    public const string SessionCookie = "nlwc_session";
    public const string CsrfCookie = "nlwc_csrf";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue(SessionCookie, out var token) || string.IsNullOrEmpty(token)) return AuthenticateResult.NoResult();

        var p = await sessionService.ResolveAsync(token);
        if (p is null) return AuthenticateResult.NoResult();

        var claims = new List<Claim>
        {
            new(EmsClaimTypes.AccountId, p.AccountId.ToString()),
            new(EmsClaimTypes.PersonId, p.PersonId.ToString()),
            new(EmsClaimTypes.SessionId, p.SessionId.ToString()),
            new(EmsClaimTypes.Surface, p.Surface.ToString().ToLowerInvariant()),
            new(EmsClaimTypes.MfaVerified, p.MfaVerified ? "true" : "false"),
            new(EmsClaimTypes.EmailVerified, p.EmailVerified ? "true" : "false"),
        };
        claims.AddRange(p.Permissions.Select(code => new Claim(EmsClaimTypes.Permissions, code)));

        var identity = new ClaimsIdentity(claims, SchemeName);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Response.WriteAsJsonAsync(ApiResponse.Fail("Sign in to continue.", "unauthenticated"));
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return Response.WriteAsJsonAsync(ApiResponse.Fail("You do not have permission to do this.", "forbidden"));
    }
}
