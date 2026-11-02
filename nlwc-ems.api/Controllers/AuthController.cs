using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using nlwc.ems.Dtos;
using nlwc.ems.Services.Options;

namespace nlwc.ems.Controllers;

/// <summary>Sign-up, sign-in and session. Cookie-based: the session token is httpOnly, so scripts never see it.</summary>
[ApiController]
[Route("api/v1/[controller]")]
public class AuthController(IAccountService accountService, ISessionService sessionService, IMfaService mfaService,
    ITokenGenerator tokens, OriginPolicy origins, IOptions<SecurityOptions> security,
    IGoogleOidcClient google, IGoogleSignInService googleSignIn, IFieldEncryptor encryptor, IOptions<GoogleOptions> googleOptions) : ControllerBase
{
    private const string GoogleStateCookie = "nlwc_google_state";
    private static readonly TimeSpan GoogleStateLifetime = TimeSpan.FromMinutes(10);

    [AllowAnonymous]
    [HttpPost("signup")]
    public async Task<ActionResult<ApiResponse>> SignUp([FromBody] SignUpRequest request)
    {
        await accountService.SignUpAsync(request);
        // The same answer whether or not the email already had an account.
        return Accepted(ApiResponse.Ok("Check your email to confirm your address."));
    }

    [AllowAnonymous]
    [HttpPost("verify-email")]
    public async Task<ActionResult<ApiResponse>> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        await accountService.VerifyEmailAsync(request.Token);
        return Ok(ApiResponse.Ok("Email confirmed."));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResultDto>>> LogIn([FromBody] LogInRequest request)
    {
        if (!origins.Allows(Request.Headers.Origin.ToString()))
            return BadRequest(ApiResponse<LoginResultDto>.Fail("This request was blocked for your protection.", "bad_origin"));

        var (token, principal) = await accountService.LogInAsync(request.Email, request.Password, request.Surface);

        var csrf = StartSession(token);

        return Ok(ApiResponse<LoginResultDto>.Ok(new LoginResultDto { CsrfToken = csrf, MfaVerified = principal.MfaVerified, EmailVerified = principal.EmailVerified }));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse>> LogOut()
    {
        await sessionService.LogOutAsync(User.GetSessionId());
        Response.Cookies.Delete(SessionAuthenticationHandler.SessionCookie, new CookieOptions { Path = "/" });
        Response.Cookies.Delete(SessionAuthenticationHandler.CsrfCookie, new CookieOptions { Path = "/" });
        return Ok(ApiResponse.Ok("Signed out."));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<MeDto>>> Me() => Ok(ApiResponse<MeDto>.Ok(new MeDto
    {
        PersonId = User.GetPersonId(), Surface = User.GetSurface() ?? "web", MfaVerified = User.IsMfaVerified(), EmailVerified = User.IsEmailVerified(),
        MfaEnrolled = await mfaService.IsEnrolledAsync(User.GetAccountId())
    }));

    [Authorize]
    [HttpPost("mfa/enroll")]
    public async Task<ActionResult<ApiResponse<MfaEnrollmentDto>>> EnrollMfa()
        => Ok(ApiResponse<MfaEnrollmentDto>.Ok(new MfaEnrollmentDto { OtpauthUri = await mfaService.EnrollAsync(User.GetAccountId()) }));

    [Authorize]
    [HttpPost("mfa/verify")]
    public async Task<ActionResult<ApiResponse>> VerifyMfa([FromBody] MfaCodeRequest request)
    {
        await mfaService.VerifyAsync(User.GetAccountId(), User.GetSessionId(), request.Code);
        return Ok(ApiResponse.Ok("Verified."));
    }

    /// <summary>Sets the httpOnly session cookie and the readable CSRF cookie. Returns the CSRF value.</summary>
    private string StartSession(string token)
    {
        var csrf = tokens.NewToken();
        var session = new CookieOptions { HttpOnly = true, Secure = security.Value.CookieSecure, SameSite = SameSiteMode.Lax, Path = "/" };
        Response.Cookies.Append(SessionAuthenticationHandler.SessionCookie, token, session);
        // Readable by the page so it can echo the value in X-CSRF-Token (double-submit).
        Response.Cookies.Append(SessionAuthenticationHandler.CsrfCookie, csrf,
            new CookieOptions { HttpOnly = false, Secure = security.Value.CookieSecure, SameSite = SameSiteMode.Lax, Path = "/" });
        return csrf;
    }

    /// <summary>Which sign-in methods the web app should offer.</summary>
    [AllowAnonymous]
    [HttpGet("providers")]
    public ActionResult<ApiResponse<ProvidersDto>> Providers() => Ok(ApiResponse<ProvidersDto>.Ok(new ProvidersDto { Google = googleOptions.Value.Enabled }));

    /// <summary>Sends the browser to Google. The state, nonce and PKCE verifier wait in a short-lived encrypted cookie.</summary>
    [AllowAnonymous]
    [HttpGet("google/start")]
    public IActionResult GoogleStart([FromQuery] string? next)
    {
        if (!googleOptions.Value.Enabled) return NotFound();

        var begin = google.Begin();
        var saved = JsonSerializer.Serialize(new GoogleState(begin.State, begin.Nonce, begin.CodeVerifier, SafeNext(next), DateTimeOffset.UtcNow.Add(GoogleStateLifetime).ToUnixTimeSeconds()));
        Response.Cookies.Append(GoogleStateCookie, Convert.ToBase64String(encryptor.EncryptString(saved)), new CookieOptions
        {
            HttpOnly = true, Secure = security.Value.CookieSecure, SameSite = SameSiteMode.Lax, Path = "/api/v1/auth/google", MaxAge = GoogleStateLifetime
        });
        return Redirect(begin.Url);
    }

    /// <summary>Google sends the browser back here. Every outcome is a redirect to the web app; failures carry only a short code.</summary>
    [AllowAnonymous]
    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback([FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error)
    {
        if (!googleOptions.Value.Enabled) return NotFound();

        var saved = ReadGoogleState();
        Response.Cookies.Delete(GoogleStateCookie, new CookieOptions { Path = "/api/v1/auth/google" });
        var next = saved?.Next ?? "/dashboard";

        if (error is not null) return Back("/login", error == "access_denied" ? "google_cancelled" : "google_failed", next);
        if (saved is null || code is null || state is null || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(state), Encoding.UTF8.GetBytes(saved.State)))
            return Back("/login", "google_failed", next);

        try
        {
            var identity = await google.CompleteAsync(code, saved.Verifier, saved.Nonce);
            var (token, _) = await googleSignIn.SignInAsync(identity);
            StartSession(token);
            return Redirect(googleOptions.Value.WebBaseUrl.TrimEnd('/') + next);
        }
        catch (UnauthorizedException e)
        {
            return Back("/login", e.Code ?? "google_failed", next);
        }
    }

    private RedirectResult Back(string path, string error, string next)
        => Redirect($"{googleOptions.Value.WebBaseUrl.TrimEnd('/')}{path}?error={Uri.EscapeDataString(error)}&next={Uri.EscapeDataString(next)}");

    private GoogleState? ReadGoogleState()
    {
        try
        {
            if (!Request.Cookies.TryGetValue(GoogleStateCookie, out var raw)) return null;
            var state = JsonSerializer.Deserialize<GoogleState>(encryptor.DecryptString(Convert.FromBase64String(raw)));
            return state is not null && state.ExpiresAt > DateTimeOffset.UtcNow.ToUnixTimeSeconds() ? state : null;
        }
        catch (Exception e) when (e is FormatException or JsonException or CryptographicException)
        {
            return null;
        }
    }

    /// <summary>Only a path on this site: starts with one slash, so never another host.</summary>
    private static string SafeNext(string? next)
        => next is { Length: > 0 and <= 300 } && next[0] == '/' && !next.StartsWith("//") && !next.Contains('\\') && !next.Contains('\r') && !next.Contains('\n') ? next : "/dashboard";

    private sealed record GoogleState(string State, string Nonce, string Verifier, string Next, long ExpiresAt);
}
