using System.Security.Claims;

namespace nlwc.ems.Base.Core.Security;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetAccountIdOrNull(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(EmsClaimTypes.AccountId), out var id) ? id : null;

    public static Guid GetAccountId(this ClaimsPrincipal user) =>
        user.GetAccountIdOrNull() ?? throw new InvalidOperationException("No authenticated account.");

    public static Guid GetPersonId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(EmsClaimTypes.PersonId), out var id) ? id : throw new InvalidOperationException("No authenticated person.");

    public static Guid GetSessionId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(EmsClaimTypes.SessionId), out var id) ? id : throw new InvalidOperationException("No session.");

    public static string? GetUserIdOrNull(this ClaimsPrincipal user) => user.FindFirstValue(EmsClaimTypes.AccountId);

    public static bool IsMfaVerified(this ClaimsPrincipal user) => user.FindFirstValue(EmsClaimTypes.MfaVerified) == "true";

    public static bool IsEmailVerified(this ClaimsPrincipal user) => user.FindFirstValue(EmsClaimTypes.EmailVerified) == "true";

    public static string? GetSurface(this ClaimsPrincipal user) => user.FindFirstValue(EmsClaimTypes.Surface);

    /// <summary>True when the caller holds <paramref name="code"/> (<c>resource:level</c>) church-wide, or — when an
    /// <paramref name="editionId"/> is given — for that edition. Does not check MFA; see
    /// <see cref="HasPermissionWithMfa"/>.</summary>
    public static bool HasPermission(this ClaimsPrincipal user, string code, Guid? editionId = null) =>
        user.FindAll(EmsClaimTypes.Permissions).Any(c =>
            c.Value == code || (editionId is not null && c.Value == $"{code}@{editionId}"));

    /// <summary>Every permissioned role is a staff role, so a staff permission only counts on an MFA-verified session.</summary>
    public static bool HasPermissionWithMfa(this ClaimsPrincipal user, string code, Guid? editionId = null) =>
        user.IsMfaVerified() && user.HasPermission(code, editionId);

    /// <summary>Why a permission check failed: <c>mfa_required</c> when the caller holds one of the codes but has not
    /// completed MFA on this session, otherwise <c>forbidden</c>. Lets a client prompt for the second factor only when
    /// it would actually help.</summary>
    public static string DenialCode(this ClaimsPrincipal user, IEnumerable<string> codes, Guid? editionId = null) =>
        !user.IsMfaVerified() && codes.Any(c => user.HasPermission(c, editionId)) ? "mfa_required" : "forbidden";
}
