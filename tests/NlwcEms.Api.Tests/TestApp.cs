using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using NlwcEms.Api.Db;
using NlwcEms.Api.Identity;
using Testcontainers.PostgreSql;

namespace NlwcEms.Api.Tests;

public sealed class CapturingMailer : IMailer
{
    public readonly List<(string To, string Template, IReadOnlyDictionary<string, string> Data)> Sent = [];
    public Task SendAsync(string to, string template, IReadOnlyDictionary<string, string> data)
    {
        lock (Sent) Sent.Add((to, template, data));
        return Task.CompletedTask;
    }
}

/// <summary>Real Postgres 16 in a container, migrations applied, one app instance shared by all tests.</summary>
public sealed class TestApp : IAsyncLifetime
{
    private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder("postgres:16").Build();
    private WebApplicationFactory<Program> _factory = null!;
    public CapturingMailer Mail { get; } = new();
    public NpgsqlDataSource Db => _factory.Services.GetRequiredService<NpgsqlDataSource>();
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _pg.StartAsync();
        Environment.SetEnvironmentVariable("DATABASE_URL", _pg.GetConnectionString());
        Environment.SetEnvironmentVariable("MASTER_KEY", new string('a', 64));
        Environment.SetEnvironmentVariable("COOKIE_SECURE", "false");
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
            b.ConfigureTestServices(s => { s.RemoveAll<IMailer>(); s.AddSingleton<IMailer>(Mail); }));
        Client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        await Migrator.RunAsync(Db);
    }

    public async Task DisposeAsync()
    {
        _factory.Dispose();
        await _pg.DisposeAsync();
    }
}

[CollectionDefinition("app")]
public sealed class AppCollection : ICollectionFixture<TestApp>;
