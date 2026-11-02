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

/// <summary>Stands in for Google: <see cref="Identity"/> is who the next callback signs in.</summary>
public sealed class FakeGoogle : IGoogleOidcClient
{
    public GoogleIdentity? Identity { get; set; }

    public GoogleAuthorization Begin()
    {
        var state = Guid.NewGuid().ToString("N");
        return new GoogleAuthorization($"https://accounts.google.com/o/oauth2/v2/auth?state={state}&code_challenge=abc", state, "nonce", "verifier");
    }

    public Task<GoogleIdentity> CompleteAsync(string code, string codeVerifier, string nonce)
        => code == "good" && codeVerifier == "verifier" && nonce == "nonce" && Identity is not null
            ? Task.FromResult(Identity)
            : throw new UnauthorizedException("no", "google_failed");
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
    public FakeGoogle Google { get; } = new();

    /// <summary>For tests that need to see a redirect rather than follow it.</summary>
    public HttpClient NoRedirectClient { get; private set; } = null!;
    public IServiceProvider Services => _factory.Services;

    public async Task InitializeAsync()
    {
        await _pg.StartAsync();
        Environment.SetEnvironmentVariable("ConnectionStrings__EmsDatabase", _pg.GetConnectionString());
        Environment.SetEnvironmentVariable("Security__MasterKey", new string('a', 64));
        Environment.SetEnvironmentVariable("Security__CookieSecure", "false");
        Environment.SetEnvironmentVariable("Google__ClientId", "test-client");
        Environment.SetEnvironmentVariable("Google__ClientSecret", "test-secret");
        Environment.SetEnvironmentVariable("Google__RedirectUri", "http://localhost:3000/api/v1/auth/google/callback");
        Environment.SetEnvironmentVariable("Google__WebBaseUrl", "http://localhost:3000");
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
            b.ConfigureTestServices(s =>
            {
                s.RemoveAll<IMailer>(); s.AddSingleton<IMailer>(Mail);
                s.RemoveAll<IGoogleOidcClient>(); s.AddSingleton<IGoogleOidcClient>(Google);
            }));
        NoRedirectClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false, AllowAutoRedirect = false });
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
