using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using nlwc.ems.Core.Security;
using nlwc.ems.Data.EFCore;
using nlwc.ems.Dtos;
using nlwc.ems.Services.Implementations;
using nlwc.ems.Services.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseEmsSerilog();

builder.Services
    .AddControllers(options => options.Filters.Add<AuditDenialFilter>())
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .AddEmsInvalidModelResponse();

builder.Services.AddDbContext<EmsDbContext>(options => options
    .UseNpgsql(builder.Configuration.GetConnectionString("EmsDatabase"))
    .UseSnakeCaseNamingConvention());

// Read by BaseDao to stamp CreatedBy/CreatedByIp/ModifiedBy/ModifiedByIp on every save.
builder.Services.AddHttpContextAccessor();

builder.Services.AddEmsCors(builder.Configuration);
builder.Services.AddSingleton(new OriginPolicy(builder.Configuration.GetAllowedOrigins()));
// Checked before anything else (including migrations) so a wrong key stops the app with a clear message, not a 500 at first sign-up.
if (!SecurityOptions.IsValidMasterKey(builder.Configuration[$"{SecurityOptions.SectionName}:MasterKey"]))
    throw new InvalidOperationException("Security:MasterKey must be 64 hex characters (32 bytes). Generate one with: openssl rand -hex 32");
builder.Services.Configure<SecurityOptions>(builder.Configuration.GetSection(SecurityOptions.SectionName));

builder.Services.Configure<GoogleOptions>(builder.Configuration.GetSection(GoogleOptions.SectionName));
builder.Services.AddHttpClient();

// Everything under these namespaces is registered against the interfaces it implements.
builder.Services
    .AddImplementationsFromNamespace<EmsDbContext>("nlwc.ems.Data.EFCore.Implementations")
    .AddImplementationsFromNamespace<EditionService>("nlwc.ems.Services.Implementations");

// Cookie-session authentication; permissions arrive as claims, so RequirePermission and the base controllers read one thing.
builder.Services.AddAuthentication(SessionAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(SessionAuthenticationHandler.SchemeName, null);
builder.Services.AddEmsPermissionAuthorization();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, EmsAuthorizationResultHandler>();

builder.Services.AddHealthChecks();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "nlwc-ems-api", Version = "v1" });
    options.AddSecurityDefinition("Session", new OpenApiSecurityScheme
    {
        Name = SessionAuthenticationHandler.SessionCookie,
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Cookie,
        Description = "The httpOnly session cookie set by POST /api/v1/auth/login. Mutations also need X-CSRF-Token."
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<EmsDbContext>().Database.Migrate();
}

// First super admin: no one else can grant the first role. Sign up first, then run once with the account's email.
var bootstrap = Array.IndexOf(args, "--bootstrap-admin");
if (bootstrap >= 0)
{
    var email = args[bootstrap + 1].Trim().ToLowerInvariant();
    using var scope = app.Services.CreateScope();
    var account = await scope.ServiceProvider.GetRequiredService<IAccountDao>().RetrieveByEmailAsync(email)
        ?? throw new InvalidOperationException("No account with that email. Sign up first.");
    await scope.ServiceProvider.GetRequiredService<IRoleGrantService>()
        .GrantAsync(account.Id, new GrantRoleRequest { AccountId = account.Id, Role = "super_admin", Reason = "bootstrap" });
    Console.WriteLine($"Granted super_admin to {email}");
    return;
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseEmsForwardedHeaders();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseCors("EmsWeb");

app.UseAuthentication();
app.UseMiddleware<CsrfProtectionMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Lets the test project boot the real app.
public partial class Program;
