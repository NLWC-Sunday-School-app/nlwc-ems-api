namespace nlwc.ems.Base.Core.Security;

public static class EmsClaimTypes
{
    public const string AccountId = "sub";
    public const string PersonId = "personid";
    public const string Surface = "surface";
    public const string SessionId = "sessionid";
    public const string MfaVerified = "mfa";
    public const string EmailVerified = "emailverified";

    /// <summary>Repeated claim. <c>resource:level</c> for a church-wide grant, <c>resource:level@{editionId}</c>
    /// for a grant scoped to one edition. A held level also carries every lower level of its resource.</summary>
    public const string Permissions = "permissions";
}
