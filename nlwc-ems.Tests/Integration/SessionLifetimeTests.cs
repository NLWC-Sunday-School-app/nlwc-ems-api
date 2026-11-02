using Dapper;

namespace nlwc.ems.Tests.Integration;

[Collection("app")]
public sealed class SessionLifetimeTests(TestApp app)
{
    private readonly Harness h = new(app);

    private async Task<(IEnumerable<string> Cookies, DateTime IdleExpires, DateTime AbsoluteExpires)> SignIn(string surface)
    {
        var email = $"life-{Guid.NewGuid():N}@example.com";
        await h.Send(HttpMethod.Post, "/api/v1/auth/signup", null, new { email, password = Harness.Password, givenName = "Ada", familyName = "Obi" });
        var login = await h.Send(HttpMethod.Post, "/api/v1/auth/login", null, new { email, password = Harness.Password, surface });
        login.IsSuccessStatusCode.Should().BeTrue();
        await using var c = await app.Db.OpenConnectionAsync();
        var (idle, absolute) = await c.QuerySingleAsync<(DateTime, DateTime)>(
            "SELECT s.idle_expires_at, s.absolute_expires_at FROM auth_sessions s JOIN accounts a ON a.id = s.account_id WHERE a.email = @email", new { email });
        return (login.Headers.GetValues("Set-Cookie").ToList(), idle, absolute);
    }

    [Fact]
    public async Task A_portal_sign_in_gets_persistent_cookies_and_a_thirty_day_session()
    {
        var (cookies, idle, absolute) = await SignIn("web");
        cookies.Where(c => c.StartsWith("nlwc_session=") || c.StartsWith("nlwc_csrf=")).Should().HaveCount(2).And.OnlyContain(c => c.Contains("max-age=2592000"));
        (idle - DateTime.UtcNow).Should().BeCloseTo(TimeSpan.FromDays(30), TimeSpan.FromMinutes(5));
        (absolute - DateTime.UtcNow).Should().BeCloseTo(TimeSpan.FromDays(30), TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task A_staff_sign_in_keeps_session_only_cookies_and_its_short_limits()
    {
        var (cookies, idle, absolute) = await SignIn("admin");
        cookies.Where(c => c.StartsWith("nlwc_session=") || c.StartsWith("nlwc_csrf=")).Should().HaveCount(2).And.OnlyContain(c => !c.Contains("max-age") && !c.Contains("expires"));
        (idle - DateTime.UtcNow).Should().BeCloseTo(TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(5));
        (absolute - DateTime.UtcNow).Should().BeCloseTo(TimeSpan.FromHours(12), TimeSpan.FromMinutes(5));
    }
}
