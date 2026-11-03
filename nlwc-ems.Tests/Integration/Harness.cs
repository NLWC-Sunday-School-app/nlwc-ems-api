using System.Net.Http.Json;
using System.Text.Json;
using Dapper;
using OtpNet;

namespace nlwc.ems.Tests.Integration;

/// <summary>Shared helpers for HTTP-level tests: users, staff with MFA, and requests with Origin and CSRF.</summary>
public sealed class Harness(TestApp app)
{
    public const string Origin = "http://localhost:3000";
    public const string Password = "correct horse battery";

    public sealed record Session(Guid AccountId, Guid PersonId, string Email, Dictionary<string, string> Headers);

    public Task<HttpResponseMessage> Send(HttpMethod method, string url, Dictionary<string, string>? headers = null, object? body = null, string origin = Origin)
    {
        var request = new HttpRequestMessage(method, url);
        if (body is not null) request.Content = JsonContent.Create(body);
        if (origin.Length > 0) request.Headers.Add("Origin", origin);
        foreach (var (k, v) in headers ?? []) request.Headers.TryAddWithoutValidation(k, v);
        return app.Client.SendAsync(request);
    }

    /// <summary>The response body as JSON. A server error comes back as text, so surface it rather than a parse failure.</summary>
    public static async Task<JsonElement> Json(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        try { return JsonDocument.Parse(text).RootElement.Clone(); }
        catch (JsonException) { throw new InvalidOperationException($"HTTP {(int)response.StatusCode} with a non-JSON body:\n{text[..Math.Min(text.Length, 1500)]}"); }
    }

    /// <summary>The <c>data</c> of the standard <c>ApiResponse</c> envelope.</summary>
    public static async Task<JsonElement> Data(HttpResponseMessage response) => (await Json(response)).GetProperty("data");

    /// <summary>The machine-readable <c>code</c> of a failed response.</summary>
    public static async Task<string?> Code(HttpResponseMessage response) =>
        (await Json(response)).TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() : null;

    public static string Cookie(HttpResponseMessage response, string name) =>
        response.Headers.GetValues("Set-Cookie").First(c => c.StartsWith(name + "=")).Split(';')[0][(name.Length + 1)..];

    /// <summary>A new account, signed in on the given surface.</summary>
    public async Task<Session> NewUser(string surface = "web", string tag = "u")
    {
        var email = $"{tag}-{Guid.NewGuid():N}@example.com";
        await Send(HttpMethod.Post, "/api/v1/auth/signup", null, new { email, password = Password, givenName = "Ada", familyName = "Obi" });
        var login = await Send(HttpMethod.Post, "/api/v1/auth/login", null, new { email, password = Password, surface });
        var session = Cookie(login, "nlwc_session");
        var csrf = Cookie(login, "nlwc_csrf");
        await using var conn = await app.Db.OpenConnectionAsync();
        var (account, person) = await conn.QuerySingleAsync<(Guid, Guid)>("SELECT id, person_id FROM accounts WHERE email = @email", new { email });
        return new Session(account, person, email, new() { ["Cookie"] = $"nlwc_session={session}; nlwc_csrf={csrf}", ["X-CSRF-Token"] = csrf });
    }

    /// <summary>Gives an account a role church-wide, straight in the database (the API can't bootstrap the first grant).</summary>
    public async Task Grant(Guid accountId, string role, Guid? editionId = null)
    {
        await using var conn = await app.Db.OpenConnectionAsync();
        await conn.ExecuteAsync(
            """
            INSERT INTO role_grants(id, account_id, role_id, edition_id, granted_by, reason, status, date_created, date_modified)
            SELECT UUID(), @accountId, id, @editionId, @accountId, 'test', 'Active', UTC_TIMESTAMP(6), UTC_TIMESTAMP(6) FROM roles WHERE `key` = @role
            """, new { accountId, editionId, role });
    }

    /// <summary>Completes TOTP on the session, as a staff member does at sign-in.</summary>
    public async Task VerifyMfa(Session session)
    {
        var enrol = await Data(await Send(HttpMethod.Post, "/api/v1/auth/mfa/enroll", session.Headers, new { }));
        var secret = System.Web.HttpUtility.ParseQueryString(new Uri(enrol.GetProperty("otpauthUri").GetString()!).Query)["secret"]!;
        var verify = await Send(HttpMethod.Post, "/api/v1/auth/mfa/verify", session.Headers, new { code = new Totp(Base32Encoding.ToBytes(secret)).ComputeTotp() });
        verify.IsSuccessStatusCode.Should().BeTrue(await verify.Content.ReadAsStringAsync());
    }

    /// <summary>A signed-in, MFA-verified staff account holding the given roles church-wide.</summary>
    public async Task<Session> StaffWith(params string[] roles)
    {
        var user = await NewUser("admin", "s");
        foreach (var role in roles) await Grant(user.AccountId, role);
        await VerifyMfa(user);
        return user;
    }
}
