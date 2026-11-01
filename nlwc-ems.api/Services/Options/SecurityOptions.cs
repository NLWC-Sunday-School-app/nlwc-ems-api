namespace nlwc.ems.Services.Options;

public class SecurityOptions
{
    public const string SectionName = "Security";

    /// <summary>32 bytes as 64 hex characters. Encrypts MFA secrets and collector/emergency phone numbers.</summary>
    public string MasterKey { get; set; } = string.Empty;

    /// <summary>Marks the session cookie Secure. Only turn off for plain-http local development and tests.</summary>
    public bool CookieSecure { get; set; } = true;

    public static bool IsValidMasterKey(string? key)
        => key is { Length: 64 } && key.All(Uri.IsHexDigit);

    public byte[] MasterKeyBytes()
    {
        var key = Convert.FromHexString(MasterKey);
        return key.Length == 32 ? key : throw new InvalidOperationException("Security:MasterKey must be 32 bytes (64 hex characters).");
    }
}
