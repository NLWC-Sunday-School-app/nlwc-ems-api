namespace nlwc.ems.Core;

/// <summary>Human-readable references such as <c>BB26-0412-A7</c>: edition code, per-edition sequence, two check characters.
/// The check characters catch a mistyped reference at the desk; they are not a secret.</summary>
public static class RegistrationReference
{
    // No I or O: they read as 1 and 0 on a printed pass.
    private const string Letters = "ABCDEFGHJKLMNPQRSTUVWXYZ";

    public static string Format(string editionCode, long sequence)
    {
        var body = $"{editionCode}-{sequence:0000}";
        var sum = 0;
        for (var i = 0; i < body.Length; i++) sum += (i + 1) * body[i];
        return $"{body}-{Letters[sum % Letters.Length]}{sum / Letters.Length % 10}";
    }
}
