namespace nlwc.ems.Base.Core.Exceptions;

/// <summary>A known, expected domain-rule violation (duplicate, invalid state transition, locked value).
/// <paramref name="code"/> is a stable machine-readable token the frontends can branch on.</summary>
public class AppException(string message, bool isConflict = false, string? code = null) : Exception(message)
{
    public bool IsConflict { get; } = isConflict;
    public string? Code { get; } = code;
}
