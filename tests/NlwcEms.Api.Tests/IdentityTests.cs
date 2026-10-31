using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using OtpNet;

namespace NlwcEms.Api.Tests;

[Collection("app")]
public sealed class IdentityTests(TestApp app)
{
    private const string Origin = "http://localhost:3000";
    private const string Password = "correct horse battery";

    private static string Email(string tag) => $"{tag}-{Guid.NewGuid():N}@example.com";

    private Task<HttpResponseMessage> Post(string url, object body, Dictionary<string, string>? headers = null, string origin = Origin)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        if (origin.Length > 0) req.Headers.Add("Origin", origin);
        foreach (var (k, v) in headers ?? []) req.Headers.TryAddWithoutValidation(k, v);
        return app.Client.SendAsync(req);
    }

    private Task<HttpResponseMessage> Get(string url, Dictionary<string, string> headers)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        foreach (var (k, v) in headers) req.Headers.TryAddWithoutValidation(k, v);
        return app.Client.SendAsync(req);
    }

    private static async Task<JsonElement> Json(HttpResponseMessage r) => (await r.Content.ReadFromJsonAsync<JsonElement>());

    private static string CookieValue(HttpResponseMessage r, string name) =>
        r.Headers.GetValues("Set-Cookie").First(c => c.StartsWith(name + "=")).Split(';')[0][(name.Length + 1)..];

    private object SignUpBody(string email, string password = Password) => new { email, password, givenName = "Ada", familyName = "Obi" };

    private async Task<(HttpResponseMessage Login, Dictionary<string, string> Headers)> SignUpAndLogin(string tag)
    {
        var email = Email(tag);
        await Post("/v1/auth/signup", SignUpBody(email));
        var login = await Post("/v1/auth/login", new { email, password = Password });
        var s = CookieValue(login, "nlwc_session");
        var c = CookieValue(login, "nlwc_csrf");
        return (login, new() { ["Cookie"] = $"nlwc_session={s}; nlwc_csrf={c}", ["X-CSRF-Token"] = c });
    }

    [Fact]
    public async Task Signup_answers_the_same_whether_or_not_the_email_exists()
    {
        var email = Email("dup");
        var a = await Post("/v1/auth/signup", SignUpBody(email));
        var b = await Post("/v1/auth/signup", SignUpBody(email));
        Assert.Equal(HttpStatusCode.Accepted, a.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, b.StatusCode);
        Assert.Equal(await a.Content.ReadAsStringAsync(), await b.Content.ReadAsStringAsync());
        Assert.Equal(["verify_email", "account_exists"], app.Mail.Sent.Where(m => m.To == email).Select(m => m.Template));
    }

    [Fact]
    public async Task Weak_passwords_are_rejected() =>
        Assert.Equal(HttpStatusCode.BadRequest, (await Post("/v1/auth/signup", SignUpBody(Email("weak"), "short"))).StatusCode);

    [Fact]
    public async Task Login_sets_httponly_session_cookie_and_me_works()
    {
        var (login, headers) = await SignUpAndLogin("me");
        var session = login.Headers.GetValues("Set-Cookie").First(c => c.StartsWith("nlwc_session="));
        Assert.Contains("httponly", session, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", session, StringComparison.OrdinalIgnoreCase);
        var me = await Get("/v1/auth/me", headers);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.False((await Json(me)).GetProperty("emailVerified").GetBoolean());
    }

    [Fact]
    public async Task Email_verification_token_works_once()
    {
        var email = Email("v");
        await Post("/v1/auth/signup", SignUpBody(email));
        var token = app.Mail.Sent.Single(m => m.To == email).Data["token"];
        Assert.Equal(HttpStatusCode.NoContent, (await Post("/v1/auth/verify-email", new { token })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Post("/v1/auth/verify-email", new { token })).StatusCode);
    }

    [Fact]
    public async Task Wrong_password_and_unknown_email_give_the_same_error()
    {
        var email = Email("w");
        await Post("/v1/auth/signup", SignUpBody(email));
        var a = await Post("/v1/auth/login", new { email, password = "wrong wrong wrong" });
        var b = await Post("/v1/auth/login", new { email = Email("nobody"), password = "wrong wrong wrong" });
        Assert.Equal(HttpStatusCode.Unauthorized, a.StatusCode);
        Assert.Equal(await a.Content.ReadAsStringAsync(), await b.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Account_locks_after_repeated_failures()
    {
        var email = Email("lock");
        await Post("/v1/auth/signup", SignUpBody(email));
        for (var i = 0; i < 5; i++) await Post("/v1/auth/login", new { email, password = "bad bad bad bad" });
        var r = await Post("/v1/auth/login", new { email, password = Password });
        Assert.Equal("locked", (await Json(r)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Mutations_need_origin_and_csrf_token()
    {
        var (_, headers) = await SignUpAndLogin("csrf");
        var noCsrf = await Post("/v1/auth/logout", new { }, new() { ["Cookie"] = headers["Cookie"] });
        Assert.Equal(HttpStatusCode.Forbidden, noCsrf.StatusCode);
        var badOrigin = await Post("/v1/auth/logout", new { }, headers, origin: "https://evil.example");
        Assert.Equal(HttpStatusCode.Forbidden, badOrigin.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Post("/v1/auth/logout", new { }, headers)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Get("/v1/auth/me", headers)).StatusCode);
    }

    [Fact]
    public async Task Login_from_a_non_allowed_origin_is_refused()
    {
        var email = Email("o");
        await Post("/v1/auth/signup", SignUpBody(email));
        var r = await Post("/v1/auth/login", new { email, password = Password }, origin: "https://evil.example");
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Totp_enrol_verify_and_reject_a_bad_code()
    {
        var (_, headers) = await SignUpAndLogin("mfa");
        var er = await Post("/v1/auth/mfa/enroll", new { }, headers);
        Assert.True(er.IsSuccessStatusCode, await er.Content.ReadAsStringAsync());
        var enrol = await Json(er);
        var uri = new Uri(enrol.GetProperty("otpauthUri").GetString()!);
        var secret = System.Web.HttpUtility.ParseQueryString(uri.Query)["secret"]!;
        var totp = new Totp(Base32Encoding.ToBytes(secret));

        Assert.Equal(HttpStatusCode.BadRequest, (await Post("/v1/auth/mfa/verify", new { code = "000000" }, headers)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Post("/v1/auth/mfa/verify", new { code = totp.ComputeTotp() }, headers)).StatusCode);
        Assert.True((await Json(await Get("/v1/auth/me", headers))).GetProperty("mfaVerified").GetBoolean());
    }
}
