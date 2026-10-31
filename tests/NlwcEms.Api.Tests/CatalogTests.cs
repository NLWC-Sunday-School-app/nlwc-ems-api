using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dapper;
using Staff = NlwcEms.Api.Tests.Harness.Session;

namespace NlwcEms.Api.Tests;

[Collection("app")]
public sealed class CatalogTests(TestApp app)
{
    private readonly Harness h = new(app);
    private Task<HttpResponseMessage> Send(HttpMethod m, string url, Dictionary<string, string>? hd = null, object? body = null) => h.Send(m, url, hd, body);
    private static Task<JsonElement> Json(HttpResponseMessage r) => Harness.Json(r);
    private Task<Staff> StaffWith(params string[] roles) => h.StaffWith(roles);

    private static object NewEdition(string tag) => new
    {
        templateCode = "BB", slug = $"bb-{tag}", code = $"B{tag}".ToUpperInvariant(), name = "Bible Bootcamp 2026",
        startsOn = "2026-12-21", endsOn = "2026-12-23", venueName = "Faith Plaza", venueAddress = "Igbogunu Bus Stop, Bariga, Lagos",
        campMinAge = 10, campMaxAge = 16, pickupStart = "16:00", pickupEnd = "18:00",
    };

    private async Task<Guid> CreateEdition(Staff admin, string tag)
    {
        var r = await Send(HttpMethod.Post, "/v1/admin/editions", admin.Headers, NewEdition(tag));
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await Json(r)).GetProperty("id").GetGuid();
    }

    private static readonly object[] FreeRules =
    [
        new { ageMin = 3, ageMax = 16, participationMode = (string?)null, kind = "FREE" },
        new { ageMin = 17, ageMax = (int?)null, participationMode = (string?)null, kind = "FREE" },
    ];

    private async Task PublishFree(Guid ed, Staff drafter, Staff approver)
    {
        var d = await Send(HttpMethod.Post, $"/v1/admin/editions/{ed}/rates", drafter.Headers, new { note = "Bootcamp 2026 is free", rules = FreeRules });
        Assert.Equal(HttpStatusCode.Created, d.StatusCode);
        var v = (await Json(d)).GetProperty("versionId").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await Send(HttpMethod.Post, $"/v1/admin/editions/{ed}/rates/{v}/approve", approver.Headers, new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(HttpMethod.Post, $"/v1/admin/editions/{ed}/rates/{v}/publish", approver.Headers, new { })).StatusCode);
    }

    private Task<HttpResponseMessage> Stage(Guid ed, Staff s, string to) =>
        Send(HttpMethod.Post, $"/v1/admin/editions/{ed}/stage", s.Headers, new { to, reason = "test" });

    [Fact]
    public async Task Creating_an_edition_sets_days_capabilities_off_and_is_not_public()
    {
        var admin = await StaffWith("super_admin");
        var ed = await CreateEdition(admin, "a1b2");
        var e = await Json(await Send(HttpMethod.Get, $"/v1/admin/editions/{ed}", admin.Headers));
        Assert.Equal("draft", e.GetProperty("stage").GetString());
        Assert.Equal(3, e.GetProperty("days").GetArrayLength());
        Assert.Empty(e.GetProperty("capabilities").EnumerateArray());
        Assert.Equal("pending", e.GetProperty("price").GetProperty("state").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await Send(HttpMethod.Get, "/v1/editions/bb-a1b2")).StatusCode);   // drafts are never public
        Assert.Equal(HttpStatusCode.Conflict, (await Send(HttpMethod.Post, "/v1/admin/editions", admin.Headers, NewEdition("a1b2"))).StatusCode);
    }

    [Fact]
    public async Task Capabilities_are_per_edition_switches()
    {
        var admin = await StaffWith("super_admin");
        var ed = await CreateEdition(admin, "c4p1");
        foreach (var cap in new[] { "daily_checkin", "camp_review" })
            Assert.Equal(HttpStatusCode.NoContent, (await Send(HttpMethod.Put, $"/v1/admin/editions/{ed}/capabilities/{cap}", admin.Headers, new { enabled = true })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send(HttpMethod.Put, $"/v1/admin/editions/{ed}/capabilities/teleport", admin.Headers, new { enabled = true })).StatusCode);
        var e = await Json(await Send(HttpMethod.Get, $"/v1/admin/editions/{ed}", admin.Headers));
        Assert.Equal(["camp_review", "daily_checkin"], e.GetProperty("capabilities").EnumerateArray().Select(x => x.GetString()));
    }

    [Fact]
    public async Task Registration_cannot_open_without_a_published_price_and_free_renders_from_an_explicit_rule()
    {
        var admin = await StaffWith("super_admin");
        var drafter = await StaffWith("event_admin");
        var finance = await StaffWith("finance_officer");
        var ed = await CreateEdition(admin, "fr33");
        Assert.Equal(HttpStatusCode.NoContent, (await Stage(ed, admin, "preview")).StatusCode);

        var blocked = await Stage(ed, admin, "registration_open");
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Equal("pricing_not_published", (await Json(blocked)).GetProperty("code").GetString());

        await PublishFree(ed, drafter, finance);
        Assert.Equal(HttpStatusCode.NoContent, (await Stage(ed, admin, "registration_open")).StatusCode);

        var pub = await Json(await Send(HttpMethod.Get, "/v1/editions/bb-fr33"));
        Assert.Equal("free", pub.GetProperty("price").GetProperty("state").GetString());
        Assert.Equal("open", pub.GetProperty("registrationState").GetString());
        Assert.Equal("Faith Plaza", pub.GetProperty("venue").GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Null, pub.GetProperty("id").ValueKind);                       // internal ids stay out of public output
        Assert.False(pub.GetProperty("camp").TryGetProperty("capacity", out var cap) && cap.ValueKind != JsonValueKind.Null);
        Assert.Contains((await Json(await Send(HttpMethod.Get, "/v1/editions"))).EnumerateArray(), e => e.GetProperty("slug").GetString() == "bb-fr33");
    }

    [Fact]
    public async Task Lifecycle_only_moves_along_valid_transitions_and_needs_a_reason()
    {
        var admin = await StaffWith("super_admin");
        var ed = await CreateEdition(admin, "l1f3");
        Assert.Equal(HttpStatusCode.Conflict, (await Stage(ed, admin, "live")).StatusCode);       // draft -> live is not allowed
        Assert.Equal(HttpStatusCode.BadRequest, (await Stage(ed, admin, "nonsense")).StatusCode);
        var noReason = await Send(HttpMethod.Post, $"/v1/admin/editions/{ed}/stage", admin.Headers, new { to = "preview" });
        Assert.Equal(HttpStatusCode.BadRequest, noReason.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Stage(ed, admin, "preview")).StatusCode);
    }

    [Fact]
    public async Task A_drafter_cannot_approve_their_own_rates()
    {
        var both = await StaffWith("event_admin", "finance_officer");
        var other = await StaffWith("finance_officer");
        var admin = await StaffWith("super_admin");
        var ed = await CreateEdition(admin, "sod1");
        var d = await Send(HttpMethod.Post, $"/v1/admin/editions/{ed}/rates", both.Headers, new { rules = FreeRules });
        var v = (await Json(d)).GetProperty("versionId").GetGuid();
        var self = await Send(HttpMethod.Post, $"/v1/admin/editions/{ed}/rates/{v}/approve", both.Headers, new { });
        Assert.Equal(HttpStatusCode.Forbidden, self.StatusCode);
        Assert.Equal("separation_of_duties", (await Json(self)).GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await Send(HttpMethod.Post, $"/v1/admin/editions/{ed}/rates/{v}/approve", other.Headers, new { })).StatusCode);
        // cannot publish before approval, and the database refuses a self-approval even if the service were bypassed
        await using var c = await app.Db.OpenConnectionAsync();
        var d2 = await Send(HttpMethod.Post, $"/v1/admin/editions/{ed}/rates", both.Headers, new { rules = FreeRules });
        var v2 = (await Json(d2)).GetProperty("versionId").GetGuid();
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => c.ExecuteAsync("UPDATE rate_plan_version SET state='approved', approved_by = drafted_by WHERE id = @v2", new { v2 }));
        Assert.Equal(HttpStatusCode.Conflict, (await Send(HttpMethod.Post, $"/v1/admin/editions/{ed}/rates/{v2}/publish", other.Headers, new { })).StatusCode);
    }

    [Fact]
    public async Task Published_rates_are_immutable_and_a_new_publish_retires_the_old_one()
    {
        var admin = await StaffWith("super_admin");
        var drafter = await StaffWith("event_admin");
        var finance = await StaffWith("finance_officer");
        var ed = await CreateEdition(admin, "1mm7");
        await PublishFree(ed, drafter, finance);
        await using var c = await app.Db.OpenConnectionAsync();
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => c.ExecuteAsync("UPDATE rate_rule SET age_min = 4 WHERE version_id IN (SELECT v.id FROM rate_plan_version v JOIN rate_plan p ON p.id = v.plan_id WHERE p.edition_id = @ed)", new { ed }));
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => c.ExecuteAsync("UPDATE rate_plan_version SET note = 'x' WHERE state = 'published' AND plan_id IN (SELECT id FROM rate_plan WHERE edition_id = @ed)", new { ed }));
        await PublishFree(ed, drafter, finance);
        var states = (await c.QueryAsync<string>("SELECT v.state FROM rate_plan_version v JOIN rate_plan p ON p.id = v.plan_id WHERE p.edition_id = @ed ORDER BY v.version", new { ed })).ToList();
        Assert.Equal(["retired", "published"], states);
    }

    [Fact]
    public async Task Invalid_and_overlapping_rules_are_rejected()
    {
        var drafter = await StaffWith("event_admin");
        var admin = await StaffWith("super_admin");
        var ed = await CreateEdition(admin, "rul3");
        Task<HttpResponseMessage> Draft(params object[] rules) => Send(HttpMethod.Post, $"/v1/admin/editions/{ed}/rates", drafter.Headers, new { rules });
        Assert.Equal(HttpStatusCode.BadRequest, (await Draft(new { ageMin = 3, ageMax = 16, kind = "AMOUNT" })).StatusCode);              // amount missing
        Assert.Equal(HttpStatusCode.BadRequest, (await Draft(new { ageMin = 3, ageMax = 16, kind = "FREE", amountKobo = 500 })).StatusCode);   // free with an amount
        var overlap = await Draft(new { ageMin = 3, ageMax = 16, kind = "FREE" }, new { ageMin = 10, ageMax = 20, kind = "FREE" });
        Assert.Equal("overlapping_rules", (await Json(overlap)).GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await Draft()).StatusCode);                                                              // no rules is not "free"
    }

    [Fact]
    public async Task Admin_catalog_endpoints_respect_roles()
    {
        var usher = await StaffWith("volunteer_usher");
        var event_ = await StaffWith("event_admin");
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(HttpMethod.Get, "/v1/admin/editions", usher.Headers)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(HttpMethod.Post, "/v1/admin/editions", usher.Headers, NewEdition("us3r"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Send(HttpMethod.Get, "/v1/admin/editions")).StatusCode);
        // event admin may draft rates but never approve them
        var admin = await StaffWith("super_admin");
        var ed = await CreateEdition(admin, "r0l3");
        var v = (await Json(await Send(HttpMethod.Post, $"/v1/admin/editions/{ed}/rates", event_.Headers, new { rules = FreeRules }))).GetProperty("versionId").GetGuid();
        Assert.Equal(HttpStatusCode.Forbidden, (await Send(HttpMethod.Post, $"/v1/admin/editions/{ed}/rates/{v}/approve", event_.Headers, new { })).StatusCode);
    }
}
