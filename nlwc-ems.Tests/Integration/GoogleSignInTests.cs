using System.Net;
using Dapper;

namespace nlwc.ems.Tests.Integration;

[Collection("app")]
public sealed class GoogleSignInTests(TestApp app)
{
    private readonly Harness h = new(app);

    private static string Email(string tag) => $"{tag}-{Guid.NewGuid():N}@example.com";

    private Task<HttpResponseMessage> Get(string url, string? cookie = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (cookie is not null) request.Headers.Add("Cookie", cookie);
        return app.NoRedirectClient.SendAsync(request);
    }

    /// <summary>Starts a sign-in and returns the state cookie plus the state Google would echo back.</summary>
    private async Task<(string Cookie, string State, string Location)> Start(string? next = null)
    {
        var response = await Get("/api/v1/auth/google/start" + (next is null ? "" : "?next=" + Uri.EscapeDataString(next)));
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var location = response.Headers.Location!.ToString();
        var state = System.Web.HttpUtility.ParseQueryString(new Uri(location).Query)["state"]!;
        return ($"nlwc_google_state={Harness.Cookie(response, "nlwc_google_state")}", state, location);
    }

    private static string? SessionFrom(HttpResponseMessage r)
        => r.Headers.TryGetValues("Set-Cookie", out var all) ? all.FirstOrDefault(c => c.StartsWith("nlwc_session="))?.Split(';')[0] : null;

    [Fact]
    public async Task Providers_says_google_is_on()
    {
        var response = await h.Send(HttpMethod.Get, "/api/v1/auth/providers");
        (await Harness.Data(response)).GetProperty("google").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Start_goes_to_google_with_pkce_and_keeps_the_secrets_in_an_httponly_cookie()
    {
        var response = await Get("/api/v1/auth/google/start");
        response.Headers.Location!.Host.Should().Be("accounts.google.com");
        var cookie = response.Headers.GetValues("Set-Cookie").First(c => c.StartsWith("nlwc_google_state="));
        cookie.Should().ContainEquivalentOf("httponly").And.ContainEquivalentOf("samesite=lax");
    }

    [Theory]
    [InlineData("//evil.example/x")]
    [InlineData("https://evil.example")]
    [InlineData("/\\evil.example")]
    public async Task A_next_that_leaves_the_site_is_replaced_by_the_dashboard(string next)
    {
        var (cookie, state, _) = await Start(next);
        app.Google.Identity = new GoogleIdentity("sub-" + Guid.NewGuid(), Email("next"), true, "Ada", "Obi");

        var callback = await Get($"/api/v1/auth/google/callback?code=good&state={state}", cookie);

        callback.Headers.Location!.ToString().Should().Be("http://localhost:3000/dashboard");
    }

    [Fact]
    public async Task A_new_google_user_gets_a_passwordless_verified_account_and_a_working_session()
    {
        var email = Email("new");
        var sub = "sub-" + Guid.NewGuid();
        var (cookie, state, _) = await Start("/events/bb-2026");
        app.Google.Identity = new GoogleIdentity(sub, email, true, "Ada", "Obi");

        var callback = await Get($"/api/v1/auth/google/callback?code=good&state={state}", cookie);

        callback.StatusCode.Should().Be(HttpStatusCode.Redirect);
        callback.Headers.Location!.ToString().Should().Be("http://localhost:3000/events/bb-2026");
        var session = SessionFrom(callback);
        session.Should().NotBeNull();
        callback.Headers.GetValues("Set-Cookie").Should().Contain(c => c.StartsWith("nlwc_session=") && c.Contains("httponly", StringComparison.OrdinalIgnoreCase));

        var me = await h.Send(HttpMethod.Get, "/api/v1/auth/me", new() { ["Cookie"] = session! });
        me.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Harness.Data(me)).GetProperty("emailVerified").GetBoolean().Should().BeTrue();

        await using var conn = await app.Db.OpenConnectionAsync();
        var row = await conn.QuerySingleAsync<(string? Hash, string Sub)>("SELECT password_hash, google_sub FROM accounts WHERE email = @email", new { email });
        row.Hash.Should().BeNull();
        row.Sub.Should().Be(sub);
    }

    [Fact]
    public async Task Signing_in_again_finds_the_same_account()
    {
        var email = Email("again");
        var sub = "sub-" + Guid.NewGuid();
        app.Google.Identity = new GoogleIdentity(sub, email, true, "Ada", "Obi");
        for (var i = 0; i < 2; i++)
        {
            var (cookie, state, _) = await Start();
            (await Get($"/api/v1/auth/google/callback?code=good&state={state}", cookie)).Headers.Location!.ToString().Should().Be("http://localhost:3000/dashboard");
        }

        await using var conn = await app.Db.OpenConnectionAsync();
        (await conn.ExecuteScalarAsync<int>("SELECT count(*) FROM accounts WHERE google_sub = @sub", new { sub })).Should().Be(1);
    }

    [Fact]
    public async Task A_password_account_is_linked_only_once_its_email_is_verified_and_google_vouches_for_it()
    {
        var user = await h.NewUser(); // signed up by password, email not yet confirmed
        var sub = "sub-" + Guid.NewGuid();

        // Unconfirmed on our side: refused.
        app.Google.Identity = new GoogleIdentity(sub, user.Email, true, "Ada", "Obi");
        var (cookie, state, _) = await Start();
        var refused = await Get($"/api/v1/auth/google/callback?code=good&state={state}", cookie);
        refused.Headers.Location!.Query.Should().Contain("error=google_link_needs_password");
        SessionFrom(refused).Should().BeNull();

        await using var conn = await app.Db.OpenConnectionAsync();
        await conn.ExecuteAsync("UPDATE verified_contacts SET verified_at = now() WHERE person_id = @p AND kind = 'Email'", new { p = user.PersonId });

        // Confirmed, but Google does not vouch for the address: refused.
        app.Google.Identity = new GoogleIdentity(sub, user.Email, false, "Ada", "Obi");
        (cookie, state, _) = await Start();
        (await Get($"/api/v1/auth/google/callback?code=good&state={state}", cookie)).Headers.Location!.Query.Should().Contain("error=google_email_unverified");

        // Both sides vouch: linked to the same person.
        app.Google.Identity = new GoogleIdentity(sub, user.Email, true, "Ada", "Obi");
        (cookie, state, _) = await Start();
        var linked = await Get($"/api/v1/auth/google/callback?code=good&state={state}", cookie);
        SessionFrom(linked).Should().NotBeNull();
        (await conn.ExecuteScalarAsync<string>("SELECT google_sub FROM accounts WHERE id = @id", new { id = user.AccountId })).Should().Be(sub);
    }

    [Fact]
    public async Task A_new_google_user_whose_email_google_has_not_verified_is_refused()
    {
        var email = Email("unv");
        app.Google.Identity = new GoogleIdentity("sub-" + Guid.NewGuid(), email, false, "Ada", "Obi");
        var (cookie, state, _) = await Start();

        var callback = await Get($"/api/v1/auth/google/callback?code=good&state={state}", cookie);

        callback.Headers.Location!.Query.Should().Contain("error=google_email_unverified");
        await using var conn = await app.Db.OpenConnectionAsync();
        (await conn.ExecuteScalarAsync<int>("SELECT count(*) FROM accounts WHERE email = @email", new { email })).Should().Be(0);
    }

    [Theory]
    [InlineData("wrong-state", true)]
    [InlineData("", false)]
    public async Task A_callback_without_the_matching_state_never_signs_anyone_in(string state, bool withCookie)
    {
        var (cookie, _, _) = await Start();
        app.Google.Identity = new GoogleIdentity("sub-" + Guid.NewGuid(), Email("bad"), true, "Ada", "Obi");

        var callback = await Get($"/api/v1/auth/google/callback?code=good&state={state}", withCookie ? cookie : null);

        callback.Headers.Location!.Query.Should().Contain("error=google_failed");
        SessionFrom(callback).Should().BeNull();
    }

    [Fact]
    public async Task Cancelling_at_google_comes_back_to_login_with_a_friendly_code()
    {
        var (cookie, _, _) = await Start();
        var callback = await Get("/api/v1/auth/google/callback?error=access_denied", cookie);
        callback.Headers.Location!.Query.Should().Contain("error=google_cancelled");
    }

    [Fact]
    public async Task The_state_cookie_is_cleared_once_the_callback_has_used_it()
    {
        var (cookie, state, _) = await Start();
        app.Google.Identity = new GoogleIdentity("sub-" + Guid.NewGuid(), Email("reuse"), true, "Ada", "Obi");
        var first = await Get($"/api/v1/auth/google/callback?code=good&state={state}", cookie);
        SessionFrom(first).Should().NotBeNull();
        first.Headers.GetValues("Set-Cookie").Should().Contain(c => c.StartsWith("nlwc_google_state=") && c.Contains("expires=", StringComparison.OrdinalIgnoreCase));
    }
}
