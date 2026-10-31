using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dapper;
using OtpNet;
using NlwcEms.Api.Access;

namespace NlwcEms.Api.Tests;

[Collection("app")]
public sealed class AccessTests(TestApp app)
{
    private const string Origin = "http://localhost:3000";
    private const string Password = "correct horse battery";

    private sealed record Staff(Guid AccountId, string Email, Dictionary<string, string> Headers, Totp? Totp = null);

    private HttpRequestMessage Req(HttpMethod m, string url, Dictionary<string, string>? h, object? body = null)
    {
        var r = new HttpRequestMessage(m, url);
        if (body is not null) r.Content = JsonContent.Create(body);
        r.Headers.Add("Origin", Origin);
        foreach (var (k, v) in h ?? []) r.Headers.TryAddWithoutValidation(k, v);
        return r;
    }

    private Task<HttpResponseMessage> Send(HttpMethod m, string url, Dictionary<string, string>? h, object? body = null) => app.Client.SendAsync(Req(m, url, h, body));
    private static async Task<JsonElement> Json(HttpResponseMessage r) => await r.Content.ReadFromJsonAsync<JsonElement>();
    private static string Cookie(HttpResponseMessage r, string n) => r.Headers.GetValues("Set-Cookie").First(c => c.StartsWith(n + "=")).Split(';')[0][(n.Length + 1)..];

    private async Task<Staff> NewAccount(string tag)
    {
        var email = $"{tag}-{Guid.NewGuid():N}@example.com";
        await Send(HttpMethod.Post, "/v1/auth/signup", null, new { email, password = Password, givenName = "S", familyName = "T" });
        var login = await Send(HttpMethod.Post, "/v1/auth/login", null, new { email, password = Password, surface = "admin" });
        var s = Cookie(login, "nlwc_session"); var c = Cookie(login, "nlwc_csrf");
        await using var conn = await app.Db.OpenConnectionAsync();
        var id = await conn.ExecuteScalarAsync<Guid>("SELECT id FROM account WHERE email = @email", new { email });
        return new Staff(id, email, new() { ["Cookie"] = $"nlwc_session={s}; nlwc_csrf={c}", ["X-CSRF-Token"] = c });
    }

    private async Task<Staff> WithMfa(Staff s)
    {
        var enrol = await Json(await Send(HttpMethod.Post, "/v1/auth/mfa/enroll", s.Headers, new { }));
        var secret = System.Web.HttpUtility.ParseQueryString(new Uri(enrol.GetProperty("otpauthUri").GetString()!).Query)["secret"]!;
        var totp = new Totp(Base32Encoding.ToBytes(secret));
        Assert.Equal(HttpStatusCode.NoContent, (await Send(HttpMethod.Post, "/v1/auth/mfa/verify", s.Headers, new { code = totp.ComputeTotp() })).StatusCode);
        return s with { Totp = totp };
    }

    private async Task Grant(Staff s, string role, Guid? edition = null)
    {
        await using var conn = await app.Db.OpenConnectionAsync();
        await conn.ExecuteAsync(
            "INSERT INTO role_grant(id, account_id, role_id, edition_id, granted_by, reason) SELECT gen_random_uuid(), @a, id, @e, @a, 'test' FROM role WHERE key = @role",
            new { a = s.AccountId, e = edition, role });
    }

    private async Task<Guid> NewEdition()
    {
        Guid org = Guid.NewGuid(), tpl = Guid.NewGuid(), ed = Guid.NewGuid();
        var code = "E" + Guid.NewGuid().ToString("N")[..8];
        await using var c = await app.Db.OpenConnectionAsync();
        await c.ExecuteAsync("INSERT INTO organization VALUES (@org,'NLWC')", new { org });
        await c.ExecuteAsync("INSERT INTO event_template VALUES (@tpl,@org,@code,'T','{}')", new { tpl, org, code });
        await c.ExecuteAsync("INSERT INTO event_edition(id,template_id,slug,code,name,starts_on,ends_on,age_reference_date) VALUES (@ed,@tpl,@code,@code,'E','2026-12-21','2026-12-23','2026-12-21')", new { ed, tpl, code });
        return ed;
    }

    [Fact]
    public async Task Without_a_grant_access_is_denied_and_audited()
    {
        var s = await WithMfa(await NewAccount("nogrant"));
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(HttpMethod.Get, "/v1/admin/audit", s.Headers)).StatusCode);
        await using var c = await app.Db.OpenConnectionAsync();
        Assert.Equal(1, await c.ExecuteScalarAsync<int>("SELECT count(*) FROM audit_event WHERE action = 'access.denied' AND actor_account_id = @id", new { id = s.AccountId }));
    }

    [Fact]
    public async Task Staff_need_mfa_even_with_the_right_role()
    {
        var s = await NewAccount("nomfa");
        await Grant(s, "super_admin");
        var r = await Send(HttpMethod.Get, "/v1/admin/audit", s.Headers);
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
        Assert.Equal("mfa_required", (await Json(r)).GetProperty("code").GetString());
        s = await WithMfa(s);
        Assert.Equal(HttpStatusCode.OK, (await Send(HttpMethod.Get, "/v1/admin/audit", s.Headers)).StatusCode);
    }

    [Fact]
    public async Task A_low_role_cannot_read_audit_or_grant_roles()
    {
        var s = await WithMfa(await NewAccount("usher"));
        await Grant(s, "volunteer_usher");
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(HttpMethod.Get, "/v1/admin/audit", s.Headers)).StatusCode);
        var g = await Send(HttpMethod.Post, "/v1/admin/staff/grants", s.Headers, new { accountId = s.AccountId, role = "super_admin", reason = "self" });
        Assert.Equal(HttpStatusCode.Forbidden, g.StatusCode);
    }

    [Fact]
    public async Task Granting_needs_a_reason_and_is_audited_and_edition_scoped()
    {
        var admin = await WithMfa(await NewAccount("admin"));
        await Grant(admin, "super_admin");
        var target = await NewAccount("target");
        var ed = await NewEdition();

        Assert.Equal(HttpStatusCode.BadRequest, (await Send(HttpMethod.Post, "/v1/admin/staff/grants", admin.Headers, new { accountId = target.AccountId, role = "event_admin", editionId = ed })).StatusCode);
        var ok = await Send(HttpMethod.Post, "/v1/admin/staff/grants", admin.Headers, new { accountId = target.AccountId, role = "event_admin", editionId = ed, reason = "Bootcamp lead" });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        var mine = await WithMfa(target);
        var inEdition = await Json(await Send(HttpMethod.Get, $"/v1/access/me?editionId={ed}", mine.Headers));
        Assert.Contains(inEdition.GetProperty("permissions").EnumerateArray(), p => p.GetProperty("resource").GetString() == "config" && p.GetProperty("level").GetString() == "edit");
        var elsewhere = await Json(await Send(HttpMethod.Get, $"/v1/access/me?editionId={Guid.NewGuid()}", mine.Headers));
        Assert.Empty(elsewhere.GetProperty("permissions").EnumerateArray());
        var noEdition = await Json(await Send(HttpMethod.Get, "/v1/access/me", mine.Headers));
        Assert.Empty(noEdition.GetProperty("permissions").EnumerateArray());

        await using var c = await app.Db.OpenConnectionAsync();
        Assert.Equal(1, await c.ExecuteScalarAsync<int>("SELECT count(*) FROM audit_event WHERE action = 'role.granted' AND reason = 'Bootcamp lead'"));
    }

    [Fact]
    public async Task Expired_and_revoked_grants_stop_working()
    {
        var s = await WithMfa(await NewAccount("exp"));
        await Grant(s, "auditor");
        Assert.Equal(HttpStatusCode.OK, (await Send(HttpMethod.Get, "/v1/admin/audit", s.Headers)).StatusCode);
        await using var c = await app.Db.OpenConnectionAsync();
        await c.ExecuteAsync("UPDATE role_grant SET expires_at = now() - interval '1 minute' WHERE account_id = @id", new { id = s.AccountId });
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(HttpMethod.Get, "/v1/admin/audit", s.Headers)).StatusCode);

        await c.ExecuteAsync("UPDATE role_grant SET expires_at = NULL, revoked_at = now() WHERE account_id = @id", new { id = s.AccountId });
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(HttpMethod.Get, "/v1/admin/audit", s.Headers)).StatusCode);
    }

    [Fact]
    public void Higher_levels_include_lower_ones()
    {
        Assert.True(Permissions.Satisfies("health", "full", "flags"));
        Assert.True(Permissions.Satisfies("custody", "view", "act"));
        Assert.False(Permissions.Satisfies("custody", "scan", "act"));
        Assert.False(Permissions.Satisfies("pricing", "draft", "approve"));
        Assert.False(Permissions.Satisfies("nope", "x", "x"));
    }

    [Fact]
    public async Task Medical_and_finance_are_never_bundled_into_other_roles()
    {
        await using var c = await app.Db.OpenConnectionAsync();
        var health = (await c.QueryAsync<string>("SELECT r.key FROM role r JOIN role_permission p ON p.role_id = r.id WHERE p.resource = 'health' AND p.level IN ('view','full')")).ToList();
        var money = (await c.QueryAsync<string>("SELECT r.key FROM role r JOIN role_permission p ON p.role_id = r.id WHERE p.resource = 'money' AND p.level = 'edit'")).ToList();
        Assert.Equal(["medical_welfare"], health);
        Assert.Equal(["finance_officer"], money);
        // every stored permission is a known (resource, level) pair
        var all = await c.QueryAsync<(string R, string L)>("SELECT resource, level FROM role_permission");
        Assert.All(all, p => Assert.True(Permissions.Known(p.R, p.L), $"{p.R}:{p.L}"));
    }

    [Fact]
    public async Task Audit_chain_verifies_and_detects_tampering()
    {
        var audit = new AuditService(app.Db);
        await audit.WriteAsync(null, "test.one", "config");
        await audit.WriteAsync(null, "test.two", "config", null, "because", new { a = 1 });
        Assert.Null(await audit.VerifyChainAsync());

        await using var c = await app.Db.OpenConnectionAsync();
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => c.ExecuteAsync("UPDATE audit_event SET action = 'x' WHERE action = 'test.one'"));
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => c.ExecuteAsync("DELETE FROM audit_event WHERE action = 'test.one'"));

        // A privileged user who disables the trigger and edits a row is caught by the hash chain.
        await c.ExecuteAsync("ALTER TABLE audit_event DISABLE TRIGGER audit_event_append_only");
        await c.ExecuteAsync("UPDATE audit_event SET action = 'tampered' WHERE action = 'test.one'");
        await c.ExecuteAsync("ALTER TABLE audit_event ENABLE TRIGGER audit_event_append_only");
        Assert.NotNull(await audit.VerifyChainAsync());
        // restore so other tests see an intact chain
        await c.ExecuteAsync("ALTER TABLE audit_event DISABLE TRIGGER audit_event_append_only");
        await c.ExecuteAsync("UPDATE audit_event SET action = 'test.one' WHERE action = 'tampered'");
        await c.ExecuteAsync("ALTER TABLE audit_event ENABLE TRIGGER audit_event_append_only");
        Assert.Null(await audit.VerifyChainAsync());
    }
}
