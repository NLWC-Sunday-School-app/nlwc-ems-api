namespace nlwc.ems.Base.Core.Exceptions;

/// <summary>The caller is authenticated but not allowed (e.g. a separation-of-duties rule).</summary>
public class ForbiddenException(string message, string? code = null) : Exception(message)
{
    public string? Code { get; } = code;
}
