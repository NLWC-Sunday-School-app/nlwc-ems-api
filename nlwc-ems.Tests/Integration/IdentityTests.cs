using System.Net;
using Dapper;

namespace nlwc.ems.Tests.Integration;

[Collection("app")]
public sealed class IdentityTests(TestApp app)
{
    private readonly Harness h = new(app);

    private static string Email(string tag) => $"{tag}-{Guid.NewGuid():N}@example.com";
    private static object SignUpBody(string email, string password = Harness.Password) => new { email, password, givenName = "Ada", familyName = "Obi" };

    [Fact]
    public async Task Signup_answers_the_same_whether_or_not_the_email_exists()
    {
        var email = Email("dup");
        var first = await h.Send(HttpMethod.Post, "/api/v1/auth/signup", null, SignUpBody(email));
        var second = await h.Send(HttpMethod.Post, "/api/v1/auth/signup", null, SignUpBody(email));

        first.StatusCode.Should().Be(HttpStatusCode.Accepted);
        second.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await second.Content.ReadAsStringAsync()).Should().Be(await first.Content.ReadAsStringAsync());
        app.Mail.Sent.Where(m => m.To == email).Select(m => m.Template).Should().Equal("verify_email", "account_exists");
    }

    [Theory]
    [InlineData("short")]
    [InlineData("")]
    public async Task Weak_passwords_are_rejected(string password)
    {
        var response = await h.Send(HttpMethod.Post, "/api/v1/auth/signup", null, SignUpBody(Email("weak"), password));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Harness.Code(response)).Should().Be("validation_failed");
    }

    [Fact]
    public async Task Login_sets_an_httponly_session_cookie_and_me_works()
    {
        var user = await h.NewUser();
        var login = await h.Send(HttpMethod.Post, "/api/v1/auth/login", null, new { email = user.Email, password = Harness.Password });
        var session = login.Headers.GetValues("Set-Cookie").First(c => c.StartsWith("nlwc_session="));
        session.Should().ContainEquivalentOf("httponly").And.ContainEquivalentOf("samesite=lax");

        var me = await h.Send(HttpMethod.Get, "/api/v1/auth/me", user.Headers);
        me.StatusCode.Should().Be(HttpStatusCode.OK);
        (await Harness.Data(me)).GetProperty("emailVerified").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task Email_verification_token_works_once()
    {
        var email = Email("v");
        await h.Send(HttpMethod.Post, "/api/v1/auth/signup", null, SignUpBody(email));
        var token = app.Mail.Sent.Single(m => m.To == email).Data["token"];

        (await h.Send(HttpMethod.Post, "/api/v1/auth/verify-email", null, new { token })).StatusCode.Should().Be(HttpStatusCode.OK);
        var again = await h.Send(HttpMethod.Post, "/api/v1/auth/verify-email", null, new { token });
        again.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Harness.Code(again)).Should().Be("invalid_token");
    }

    [Fact]
    public async Task Verifying_the_email_is_reflected_on_the_session()
    {
        var user = await h.NewUser();
        await h.Send(HttpMethod.Post, "/api/v1/auth/verify-email", null, new { token = app.Mail.Sent.Last(m => m.To == user.Email).Data["token"] });
        var me = await Harness.Data(await h.Send(HttpMethod.Get, "/api/v1/auth/me", user.Headers));
        me.GetProperty("emailVerified").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Wrong_password_and_unknown_email_give_the_same_error()
    {
        var email = Email("w");
        await h.Send(HttpMethod.Post, "/api/v1/auth/signup", null, SignUpBody(email));
        var wrong = await h.Send(HttpMethod.Post, "/api/v1/auth/login", null, new { email, password = "wrong wrong wrong" });
        var unknown = await h.Send(HttpMethod.Post, "/api/v1/auth/login", null, new { email = Email("nobody"), password = "wrong wrong wrong" });

        wrong.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await unknown.Content.ReadAsStringAsync()).Should().Be(await wrong.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task An_account_locks_after_repeated_failures()
    {
        var email = Email("lock");
        await h.Send(HttpMethod.Post, "/api/v1/auth/signup", null, SignUpBody(email));
        for (var i = 0; i < 5; i++) await h.Send(HttpMethod.Post, "/api/v1/auth/login", null, new { email, password = "bad bad bad bad" });

        var locked = await h.Send(HttpMethod.Post, "/api/v1/auth/login", null, new { email, password = Harness.Password });
        locked.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await Harness.Code(locked)).Should().Be("locked");
    }

    [Fact]
    public async Task Mutations_need_an_allowed_origin_and_the_csrf_token()
    {
        var user = await h.NewUser();
        var cookieOnly = new Dictionary<string, string> { ["Cookie"] = user.Headers["Cookie"] };

        var noCsrf = await h.Send(HttpMethod.Post, "/api/v1/auth/logout", cookieOnly, new { });
        noCsrf.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Harness.Code(noCsrf)).Should().Be("bad_csrf");

        var badOrigin = await h.Send(HttpMethod.Post, "/api/v1/auth/logout", user.Headers, new { }, origin: "https://evil.example");
        badOrigin.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Harness.Code(badOrigin)).Should().Be("bad_origin");

        (await h.Send(HttpMethod.Post, "/api/v1/auth/logout", user.Headers, new { })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await h.Send(HttpMethod.Get, "/api/v1/auth/me", user.Headers)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_from_a_non_allowed_origin_is_refused()
    {
        var email = Email("o");
        await h.Send(HttpMethod.Post, "/api/v1/auth/signup", null, SignUpBody(email));
        var response = await h.Send(HttpMethod.Post, "/api/v1/auth/login", null, new { email, password = Harness.Password }, origin: "https://evil.example");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Harness.Code(response)).Should().Be("bad_origin");
    }

    [Fact]
    public async Task A_revoked_session_stops_working_immediately()
    {
        var user = await h.NewUser();
        await using var c = await app.Db.OpenConnectionAsync();
        await c.ExecuteAsync("UPDATE auth_sessions SET revoked_at = now() WHERE account_id = @id", new { id = user.AccountId });
        (await h.Send(HttpMethod.Get, "/api/v1/auth/me", user.Headers)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Totp_enrol_verify_and_reject_a_bad_code()
    {
        var user = await h.NewUser();
        var enrol = await h.Send(HttpMethod.Post, "/api/v1/auth/mfa/enroll", user.Headers, new { });
        enrol.StatusCode.Should().Be(HttpStatusCode.OK);

        var bad = await h.Send(HttpMethod.Post, "/api/v1/auth/mfa/verify", user.Headers, new { code = "000000" });
        bad.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Harness.Code(bad)).Should().Be("invalid_code");

        await h.VerifyMfa(user);
        (await Harness.Data(await h.Send(HttpMethod.Get, "/api/v1/auth/me", user.Headers))).GetProperty("mfaVerified").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task The_mfa_secret_is_stored_encrypted()
    {
        var user = await h.NewUser();
        var uri = (await Harness.Data(await h.Send(HttpMethod.Post, "/api/v1/auth/mfa/enroll", user.Headers, new { }))).GetProperty("otpauthUri").GetString()!;
        var secret = System.Web.HttpUtility.ParseQueryString(new Uri(uri).Query)["secret"]!;

        await using var c = await app.Db.OpenConnectionAsync();
        var stored = await c.ExecuteScalarAsync<byte[]>("SELECT secret_encrypted FROM mfa_factors WHERE account_id = @id", new { id = user.AccountId });
        Convert.ToBase64String(stored!).Should().NotContain(secret);
        stored!.Length.Should().BeGreaterThan(28);   // nonce + tag + ciphertext
    }

    [Fact]
    public async Task Passwords_are_stored_as_argon2id()
    {
        var user = await h.NewUser();
        await using var c = await app.Db.OpenConnectionAsync();
        (await c.ExecuteScalarAsync<string>("SELECT password_hash FROM accounts WHERE id = @id", new { id = user.AccountId })).Should().StartWith("argon2id$");
    }
}
