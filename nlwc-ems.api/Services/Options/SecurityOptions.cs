namespace nlwc.ems.Services.Options;

public class SecurityOptions
{
    public const string SectionName = "Security";

    /// <summary>32 bytes as 64 hex characters. Encrypts MFA secrets and collector/emergency phone numbers.</summary>
    public string MasterKey { get; set; } = string.Empty;

    /// <summary>Marks the session cookie Secure. Only turn off for plain-http local development and tests.</summary>
    public bool CookieSecure { get; set; } = true;

    /// <summary>Staff permissions only work on a session that has entered an authenticator code. Turning this off treats every
    /// session as MFA-verified, so it is for local development only and must stay on in production.</summary>
    public bool RequireMfa { get; set; } = true;

    /// <summary>When on, the person who drafted a rate plan cannot approve it, so two people are always involved in a price change.
    /// Off by default: a small team may draft and approve its own prices. Every approval is still audited with who did it.</summary>
    public bool RequireSeparationOfDuties { get; set; } = false;

    public static bool IsValidMasterKey(string? key)
        => key is { Length: 64 } && key.All(Uri.IsHexDigit);

    public byte[] MasterKeyBytes()
    {
        var key = Convert.FromHexString(MasterKey);
        return key.Length == 32 ? key : throw new InvalidOperationException("Security:MasterKey must be 32 bytes (64 hex characters).");
    }
}
