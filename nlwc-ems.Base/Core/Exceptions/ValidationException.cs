namespace nlwc.ems.Base.Core.Exceptions;

public class ValidationException(IReadOnlyList<string> errors, string? code = null)
    : Exception(string.Join("; ", errors))
{
    public IReadOnlyList<string> Errors { get; } = errors;
    public string? Code { get; } = code;
}
