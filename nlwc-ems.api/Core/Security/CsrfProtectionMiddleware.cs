using System.Security.Cryptography;
using System.Text;

namespace nlwc.ems.Core.Security;

/// <summary>
/// For a signed-in browser session, every state-changing request must come from an allowed origin and carry the
/// double-submit token (<c>X-CSRF-Token</c> matching the readable <c>nlwc_csrf</c> cookie). Anonymous calls (sign-up, login)
/// have no session to hijack and are exempt; login checks the origin itself.
/// </summary>
public class CsrfProtectionMiddleware(RequestDelegate next, OriginPolicy origins)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        var safe = HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method) || HttpMethods.IsTrace(method);

        if (!safe && context.User.Identity?.IsAuthenticated == true)
        {
            string? failure = null;
            if (!origins.Allows(context.Request.Headers.Origin.ToString())) failure = "bad_origin";
            else if (!TokenMatches(context)) failure = "bad_csrf";

            if (failure is not null)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(ApiResponse.Fail("This request was blocked for your protection. Reload and try again.", failure));
                return;
            }
        }
        await next(context);
    }

    private static bool TokenMatches(HttpContext context)
    {
        var header = context.Request.Headers["X-CSRF-Token"].ToString();
        var cookie = context.Request.Cookies[SessionAuthenticationHandler.CsrfCookie] ?? "";
        return header.Length > 0 && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(header), Encoding.UTF8.GetBytes(cookie));
    }
}
