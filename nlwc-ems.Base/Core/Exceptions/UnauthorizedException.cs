namespace nlwc.ems.Base.Core.Exceptions;

/// <summary>Authentication failed (bad credentials, locked account). Distinct from <see cref="ForbiddenException"/>,
/// where the caller is known but not allowed.</summary>
public class UnauthorizedException(string message, string? code = null) : Exception(message)
{
    public string? Code { get; } = code;
}
