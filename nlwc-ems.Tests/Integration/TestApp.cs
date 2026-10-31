using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Testcontainers.PostgreSql;

namespace nlwc.ems.Tests.Integration;

public sealed class CapturingMailer : IMailer
{
    public readonly List<(string To, string Template, IReadOnlyDictionary<string, string> Data)> Sent = [];

    public Task SendAsync(string to, string template, IReadOnlyDictionary<string, string> data)
    {
        lock (Sent) Sent.Add((To: to, Template: template, Data: data));
        return Task.CompletedTask;
    }
}

/// <summary>The real app on a real Postgres 16 in a container. The app applies its own EF migrations at startup, exactly as in
/// production, so these tests also prove the migrations.</summary>
public sealed class TestApp : IAsyncLifetime
{
    private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder("postgres:16").Build();
    private WebApplicationFactory<Program> _factory = null!;

    public CapturingMailer Mail { get; } = new();
    public NpgsqlDataSource Db { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;
    public IServiceProvider Services => _factory.Services;

    public async Task InitializeAsync()
    {
        await _pg.StartAsync();
        Environment.SetEnvironmentVariable("ConnectionStrings__EmsDatabase", _pg.GetConnectionString());
        Environment.SetEnvironmentVariable("Security__MasterKey", new string('a', 64));
        Environment.SetEnvironmentVariable("Security__CookieSecure", "false");
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
            b.ConfigureTestServices(s => { s.RemoveAll<IMailer>(); s.AddSingleton<IMailer>(Mail); }));
        Client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        Db = NpgsqlDataSource.Create(_pg.GetConnectionString());
    }

    public async Task DisposeAsync()
    {
        await Db.DisposeAsync();
        _factory.Dispose();
        await _pg.DisposeAsync();
    }
}

[CollectionDefinition("app")]
public sealed class AppCollection : ICollectionFixture<TestApp>;
