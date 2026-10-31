using System.Net.Mail;
using System.Security.Cryptography;

namespace NlwcEms.Api.Identity;

public static class IdentityEndpoints
{
    public const string SessionCookie = "nlwc_session";
    public const string CsrfCookie = "nlwc_csrf";
    private const string SessionKey = "nlwc.session";

    public static SessionInfo Session(this HttpContext c) => (SessionInfo)c.Items[SessionKey]!;

    private static IResult Problem(int status, string code, object? extra = null) => Results.Json(new { code, extra }, statusCode: status);

    private static bool OriginAllowed(HttpContext c, AppConfig cfg) =>
        c.Request.Headers.Origin.ToString() is { Length: > 0 } o && cfg.AllowedOrigins.Contains(o);

    /// <summary>Authenticates the cookie session. On mutations also enforces the Origin allow-list and double-submit CSRF.</summary>
    public static RouteHandlerBuilder RequireSession(this RouteHandlerBuilder b) => b.AddEndpointFilter(async (ctx, next) =>
    {
        var http = ctx.HttpContext;
        var cfg = http.RequestServices.GetRequiredService<AppConfig>();
        var token = http.Request.Cookies[SessionCookie];
        var session = token is null ? null : await http.RequestServices.GetRequiredService<IdentityService>().ResolveAsync(token);
        if (session is null) return Results.Unauthorized();
        if (!HttpMethods.IsGet(http.Request.Method) && !HttpMethods.IsHead(http.Request.Method) && !HttpMethods.IsOptions(http.Request.Method))
        {
            if (!OriginAllowed(http, cfg)) return Problem(403, "bad_origin");
            var header = http.Request.Headers["X-CSRF-Token"].ToString();
            var cookie = http.Request.Cookies[CsrfCookie] ?? "";
            if (header.Length == 0 || !CryptographicOperations.FixedTimeEquals(System.Text.Encoding.UTF8.GetBytes(header), System.Text.Encoding.UTF8.GetBytes(cookie)))
                return Problem(403, "bad_csrf");
        }
        http.Items[SessionKey] = session;
        return await next(ctx);
    });

    private static List<object> Validate(params (string Field, bool Ok, string Message)[] checks) =>
        checks.Where(c => !c.Ok).Select(c => (object)new { path = c.Field, message = c.Message }).ToList();

    private static bool IsEmail(string? s) => s is { Length: <= 254 } && MailAddress.TryCreate(s, out var a) && a.Address == s;

    public sealed record SignUpBody(string? Email, string? Password, string? GivenName, string? FamilyName, string? Gender);
    public sealed record LogInBody(string? Email, string? Password, string? Surface);
    public sealed record TokenBody(string? Token);
    public sealed record CodeBody(string? Code);

    public static void MapIdentity(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/v1/auth");

        g.MapPost("/signup", async (SignUpBody b, IdentityService svc) =>
        {
            var issues = Validate(
                ("email", IsEmail(b.Email), "invalid email"),
                ("password", b.Password is { Length: >= 10 and <= 200 }, "10 to 200 characters"),
                ("givenName", b.GivenName is { Length: >= 1 and <= 100 }, "required"),
                ("familyName", b.FamilyName is { Length: >= 1 and <= 100 }, "required"),
                ("gender", b.Gender is null or "male" or "female", "male or female"));
            if (issues.Count > 0) return Problem(400, "validation_failed", issues);
            await svc.SignUpAsync(b.Email!, b.Password!, b.GivenName!, b.FamilyName!, b.Gender);
            return Results.Json(new { status = "check_your_email" }, statusCode: 202);
        });

        g.MapPost("/verify-email", async (TokenBody b, IdentityService svc) =>
        {
            if (b.Token is not { Length: >= 10 }) return Problem(400, "validation_failed");
            try { await svc.VerifyEmailAsync(b.Token); return Results.NoContent(); }
            catch (AuthException e) { return Problem(400, e.Code); }
        });

        g.MapPost("/login", async (LogInBody b, HttpContext http, IdentityService svc, AppConfig cfg) =>
        {
            if (!IsEmail(b.Email) || b.Password is not { Length: <= 200 }) return Problem(400, "validation_failed");
            if (!OriginAllowed(http, cfg)) return Problem(400, "bad_origin");
            if (!Enum.TryParse<Surface>(b.Surface ?? "web", true, out var surface)) return Problem(400, "validation_failed");
            try
            {
                var (token, s) = await svc.LogInAsync(b.Email!, b.Password, surface);
                var csrf = Crypto.NewToken();
                http.Response.Cookies.Append(SessionCookie, token, new CookieOptions { HttpOnly = true, Secure = cfg.CookieSecure, SameSite = SameSiteMode.Lax, Path = "/" });
                http.Response.Cookies.Append(CsrfCookie, csrf, new CookieOptions { HttpOnly = false, Secure = cfg.CookieSecure, SameSite = SameSiteMode.Lax, Path = "/" });
                return Results.Ok(new { csrfToken = csrf, mfaVerified = s.MfaVerified, emailVerified = s.EmailVerified });
            }
            catch (AuthException e) { return Problem(401, e.Code); }
        });

        g.MapPost("/logout", async (HttpContext http, IdentityService svc) =>
        {
            await svc.LogOutAsync(http.Session().SessionId);
            http.Response.Cookies.Delete(SessionCookie, new CookieOptions { Path = "/" });
            http.Response.Cookies.Delete(CsrfCookie, new CookieOptions { Path = "/" });
            return Results.NoContent();
        }).RequireSession();

        g.MapGet("/me", (HttpContext http) =>
        {
            var s = http.Session();
            return Results.Ok(new { personId = s.PersonId, surface = s.Surface.ToString().ToLowerInvariant(), mfaVerified = s.MfaVerified, emailVerified = s.EmailVerified });
        }).RequireSession();

        g.MapPost("/mfa/enroll", async (HttpContext http, IdentityService svc) =>
            Results.Ok(new { otpauthUri = await svc.MfaEnrollAsync(http.Session()) })).RequireSession();

        g.MapPost("/mfa/verify", async (CodeBody b, HttpContext http, IdentityService svc) =>
        {
            if (b.Code is not { Length: 6 } || !b.Code.All(char.IsAsciiDigit)) return Problem(400, "validation_failed");
            try { await svc.MfaVerifyAsync(http.Session(), b.Code); return Results.NoContent(); }
            catch (AuthException e) { return Problem(400, e.Code); }
        }).RequireSession();
    }
}
