using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using nlwc.ems.Base.Core.Security;
using nlwc.ems.Base.Web.Common;
using Scrutor;

namespace nlwc.ems.Base.Web.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers every class under <paramref name="namespaceName"/> (in the assembly containing
    /// <typeparamref name="TAssemblyMarker"/>) against the interfaces it implements, so DAOs and services need no
    /// hand-written registration. Singletons and hosted workers live outside the scanned namespaces.
    /// </summary>
    public static IServiceCollection AddImplementationsFromNamespace<TAssemblyMarker>(
        this IServiceCollection services, string namespaceName, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        services.Scan(scan => scan
            .FromAssemblyOf<TAssemblyMarker>()
            .AddClasses(c => c.InNamespaces(namespaceName))
            .AsImplementedInterfaces()
            .WithLifetime(lifetime));
        return services;
    }

    /// <summary>The browser origins allowed to call the API (<c>Cors:AllowedOrigins</c>): web, admin and check-in locally.
    /// One list drives CORS, the CSRF origin check and the login origin check, so they cannot drift apart.</summary>
    public static string[] GetAllowedOrigins(this IConfiguration configuration) =>
        configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:3000", "http://localhost:3001", "http://localhost:3002"];

    /// <summary>CORS policy from <see cref="GetAllowedOrigins"/>, with credentials (the session cookie is cross-origin).</summary>
    public static IServiceCollection AddEmsCors(this IServiceCollection services, IConfiguration configuration, string policyName = "EmsWeb")
    {
        var origins = configuration.GetAllowedOrigins();
        services.AddCors(o => o.AddPolicy(policyName, p => p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
        return services;
    }

    /// <summary>The <see cref="RequirePermissionAttribute"/> pipeline: dynamic policy provider plus claim handler.</summary>
    public static IServiceCollection AddEmsPermissionAuthorization(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<IAuthorizationPolicyProvider, DynamicPermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthorization();
        return services;
    }

    /// <summary>
    /// Replaces the framework's ProblemDetails for model-binding/DataAnnotations failures with the
    /// <see cref="ApiResponse"/> envelope, so every error a client sees has one shape.
    /// </summary>
    public static IMvcBuilder AddEmsInvalidModelResponse(this IMvcBuilder mvc)
    {
        mvc.ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState.Where(e => e.Value?.Errors.Count > 0)
                .SelectMany(e => e.Value!.Errors.Select(x => string.IsNullOrWhiteSpace(x.ErrorMessage) ? $"{e.Key} is invalid." : x.ErrorMessage));
            return new BadRequestObjectResult(ApiResponse.Fail(string.Join(" ", errors), "validation_failed"));
        });
        return mvc;
    }
}
