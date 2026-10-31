namespace nlwc.ems.Base.Services;

/// <summary>
/// Service-layer outcome for a mutation, distinct from <see cref="nlwc.ems.Base.Web.Common.ApiResponse{T}"/> (the wire
/// envelope). Lets an expected business-rule failure flow back to the controller as data instead of an exception.
/// </summary>
public class ServiceResponse
{
    public bool IsSuccessful { get; }
    public string? Message { get; set; }

    protected ServiceResponse(bool isSuccessful, string? message)
    {
        IsSuccessful = isSuccessful;
        Message = message;
    }

    public static ServiceResponse Success(string? message = null) => new(true, message);
    public static ServiceResponse Fail(string message) => new(false, message);

    public static ServiceResponse<T> Success<T>(T value, string? message = null) => new(value, true, message);
    public static ServiceResponse<T> Fail<T>(string message, T? value = default) => new(value, false, message);
}

public class ServiceResponse<T>(T? value, bool isSuccessful, string? message) : ServiceResponse(isSuccessful, message)
{
    public T? Value { get; } = value;

    public static implicit operator ServiceResponse<T>(T value) => Success(value);
}
