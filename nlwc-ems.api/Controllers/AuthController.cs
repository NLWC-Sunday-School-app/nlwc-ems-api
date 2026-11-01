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
    ITokenGenerator tokens, OriginPolicy origins, IOptions<SecurityOptions> security) : ControllerBase
{
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

        var csrf = tokens.NewToken();
        var session = new CookieOptions { HttpOnly = true, Secure = security.Value.CookieSecure, SameSite = SameSiteMode.Lax, Path = "/" };
        Response.Cookies.Append(SessionAuthenticationHandler.SessionCookie, token, session);
        // Readable by the page so it can echo the value in X-CSRF-Token (double-submit).
        Response.Cookies.Append(SessionAuthenticationHandler.CsrfCookie, csrf,
            new CookieOptions { HttpOnly = false, Secure = security.Value.CookieSecure, SameSite = SameSiteMode.Lax, Path = "/" });

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
}
