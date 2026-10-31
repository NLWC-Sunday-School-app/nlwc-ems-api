using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using nlwc.ems.Base.Core.Exceptions;
using nlwc.ems.Base.Web.Common;

namespace nlwc.ems.Base.Web.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var (statusCode, message, code) = Map(ex);

            if (statusCode == StatusCodes.Status500InternalServerError)
                logger.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
            else
                logger.LogWarning("Handled exception processing {Method} {Path}: {Message}", context.Request.Method, context.Request.Path, ex.Message);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(ApiResponse.Fail(message, code));
        }
    }

    private static (int StatusCode, string Message, string? Code) Map(Exception ex) => ex switch
    {
        ValidationException ve => (StatusCodes.Status400BadRequest, string.Join("; ", ve.Errors), ve.Code),
        NotFoundException nfe => (StatusCodes.Status404NotFound, nfe.Message, null),
        UnauthorizedException ue => (StatusCodes.Status401Unauthorized, ue.Message, ue.Code),
        ForbiddenException fe => (StatusCodes.Status403Forbidden, fe.Message, fe.Code),
        AppException { IsConflict: true } ae => (StatusCodes.Status409Conflict, ae.Message, ae.Code),
        AppException ae => (StatusCodes.Status400BadRequest, ae.Message, ae.Code),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null)
    };
}
