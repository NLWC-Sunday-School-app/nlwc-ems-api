using Dapper;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using NlwcEms.Api;
using NlwcEms.Api.Access;
using NlwcEms.Api.Catalog;
using NlwcEms.Api.Db;
using NlwcEms.Api.Identity;

DefaultTypeMap.MatchNamesWithUnderscores = true;

var builder = WebApplication.CreateBuilder(args);

// Tests replace AppConfig before this point; production reads the environment.
if (!builder.Services.Any(s => s.ServiceType == typeof(AppConfig)))
    builder.Services.AddSingleton(AppConfig.FromEnvironment(Environment.GetEnvironmentVariable));

builder.Services.AddSingleton(sp => new NpgsqlDataSourceBuilder(sp.GetRequiredService<AppConfig>().DatabaseUrl).Build());
builder.Services.AddSingleton<IdentityService>();
builder.Services.AddSingleton<AuditService>();
builder.Services.AddSingleton<AccessService>();
builder.Services.AddSingleton<CatalogService>();
builder.Services.TryAddSingleton<IMailer, ConsoleMailer>();
builder.Services.AddOpenApi();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => { }));
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 100_000);

var app = builder.Build();
var cfg = app.Services.GetRequiredService<AppConfig>();

app.UseCors(p => p.WithOrigins(cfg.AllowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials());

app.MapOpenApi("/v1/openapi.json");
app.MapGet("/v1/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }));
app.MapIdentity();
app.MapAccess();
app.MapCatalog();

var bootstrap = Array.IndexOf(args, "--bootstrap-admin");
if (bootstrap >= 0)
{
    // First super admin: no one else can grant the first role. Audited, church-wide, granted by the account itself.
    var email = args[bootstrap + 1].Trim().ToLowerInvariant();
    var ds = app.Services.GetRequiredService<NpgsqlDataSource>();
    await using var c = await ds.OpenConnectionAsync();
    var id = await Dapper.SqlMapper.ExecuteScalarAsync<Guid?>(c, "SELECT id FROM account WHERE email = @email", new { email })
        ?? throw new InvalidOperationException("no account with that email; sign up first");
    await app.Services.GetRequiredService<AccessService>().GrantAsync(id, id, "super_admin", null, "bootstrap", null);
    Console.WriteLine($"granted super_admin to {email}");
    return;
}
if (args.Contains("--migrate"))
{
    Console.WriteLine("applied: " + string.Join(", ", await Migrator.RunAsync(app.Services.GetRequiredService<NpgsqlDataSource>())));
    return;
}
app.Run();

public partial class Program;
