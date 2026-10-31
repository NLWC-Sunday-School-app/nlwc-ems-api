using Dapper;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using NlwcEms.Api;
using NlwcEms.Api.Db;
using NlwcEms.Api.Identity;

DefaultTypeMap.MatchNamesWithUnderscores = true;

var builder = WebApplication.CreateBuilder(args);

// Tests replace AppConfig before this point; production reads the environment.
if (!builder.Services.Any(s => s.ServiceType == typeof(AppConfig)))
    builder.Services.AddSingleton(AppConfig.FromEnvironment(Environment.GetEnvironmentVariable));

builder.Services.AddSingleton(sp => new NpgsqlDataSourceBuilder(sp.GetRequiredService<AppConfig>().DatabaseUrl).Build());
builder.Services.AddSingleton<IdentityService>();
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

if (args.Contains("--migrate"))
{
    Console.WriteLine("applied: " + string.Join(", ", await Migrator.RunAsync(app.Services.GetRequiredService<NpgsqlDataSource>())));
    return;
}
app.Run();

public partial class Program;
