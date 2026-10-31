using System.Net.Http.Json;
using System.Text.Json;
using Dapper;
using OtpNet;

namespace NlwcEms.Api.Tests;

/// <summary>Shared helpers for HTTP-level tests: users, staff with MFA, and requests with Origin and CSRF.</summary>
public sealed class Harness(TestApp app)
{
    public const string Origin = "http://localhost:3000";
    public const string Password = "correct horse battery";

    public sealed record Session(Guid AccountId, Guid PersonId, string Email, Dictionary<string, string> Headers);

    public Task<HttpResponseMessage> Send(HttpMethod m, string url, Dictionary<string, string>? headers = null, object? body = null, string origin = Origin)
    {
        var r = new HttpRequestMessage(m, url);
        if (body is not null) r.Content = JsonContent.Create(body);
        if (origin.Length > 0) r.Headers.Add("Origin", origin);
        foreach (var (k, v) in headers ?? []) r.Headers.TryAddWithoutValidation(k, v);
        return app.Client.SendAsync(r);
    }

    public static async Task<JsonElement> Json(HttpResponseMessage r)
    {
        var text = await r.Content.ReadAsStringAsync();
        try { return JsonDocument.Parse(text).RootElement.Clone(); }
        catch (JsonException) { throw new InvalidOperationException($"HTTP {(int)r.StatusCode} with a non-JSON body:\n{text[..Math.Min(text.Length, 1500)]}"); }
    }
    public static string Cookie(HttpResponseMessage r, string n) => r.Headers.GetValues("Set-Cookie").First(c => c.StartsWith(n + "=")).Split(';')[0][(n.Length + 1)..];

    /// <summary>A new account, signed in on the given surface.</summary>
    public async Task<Session> NewUser(string surface = "web", string tag = "u")
    {
        var email = $"{tag}-{Guid.NewGuid():N}@example.com";
        await Send(HttpMethod.Post, "/v1/auth/signup", null, new { email, password = Password, givenName = "Ada", familyName = "Obi" });
        var login = await Send(HttpMethod.Post, "/v1/auth/login", null, new { email, password = Password, surface });
        var s = Cookie(login, "nlwc_session"); var c = Cookie(login, "nlwc_csrf");
        await using var conn = await app.Db.OpenConnectionAsync();
        var (account, person) = await conn.QuerySingleAsync<(Guid, Guid)>("SELECT id, person_id FROM account WHERE email = @email", new { email });
        return new Session(account, person, email, new() { ["Cookie"] = $"nlwc_session={s}; nlwc_csrf={c}", ["X-CSRF-Token"] = c });
    }

    /// <summary>A signed-in, MFA-verified staff account holding the given roles church-wide.</summary>
    public async Task<Session> StaffWith(params string[] roles)
    {
        var u = await NewUser("admin", "s");
        await using var c = await app.Db.OpenConnectionAsync();
        foreach (var role in roles)
            await c.ExecuteAsync("INSERT INTO role_grant(id, account_id, role_id, granted_by, reason) SELECT gen_random_uuid(), @id, id, @id, 'test' FROM role WHERE key = @role", new { id = u.AccountId, role });
        var enrol = await Json(await Send(HttpMethod.Post, "/v1/auth/mfa/enroll", u.Headers, new { }));
        var secret = System.Web.HttpUtility.ParseQueryString(new Uri(enrol.GetProperty("otpauthUri").GetString()!).Query)["secret"]!;
        await Send(HttpMethod.Post, "/v1/auth/mfa/verify", u.Headers, new { code = new Totp(Base32Encoding.ToBytes(secret)).ComputeTotp() });
        return u;
    }
}
