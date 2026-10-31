namespace nlwc.ems.Base.Web.Common;

public class ApiResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }

    /// <summary>Stable machine-readable reason for a failure (<c>mfa_required</c>, <c>dob_locked</c>…); null for most.</summary>
    public string? Code { get; init; }

    public static ApiResponse Ok(string? message = null) => new() { Success = true, Message = message };
    public static ApiResponse Fail(string message, string? code = null) => new() { Success = false, Message = message, Code = code };
}

public class ApiResponse<T>
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public string? Code { get; init; }
    public T? Data { get; init; }

    public static ApiResponse<T> Ok(T data, string? message = null) => new() { Success = true, Data = data, Message = message };
    public static ApiResponse<T> Fail(string message, string? code = null) => new() { Success = false, Message = message, Code = code };
}
