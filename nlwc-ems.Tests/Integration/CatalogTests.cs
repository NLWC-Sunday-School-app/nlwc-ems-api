using System.Net;
using System.Text.Json;
using Dapper;
using Staff = nlwc.ems.Tests.Integration.Harness.Session;

namespace nlwc.ems.Tests.Integration;

[Collection("app")]
public sealed class CatalogTests(TestApp app)
{
    private readonly Harness h = new(app);

    private static object NewEdition(string tag) => new
    {
        templateCode = "BB", slug = $"bb-{tag}", code = $"B{tag}", name = "Bible Bootcamp 2026",
        startsOn = "2026-12-21", endsOn = "2026-12-23", venueName = "Faith Plaza", venueAddress = "Igbogunu Bus Stop, Bariga, Lagos",
        campMinAge = 10, campMaxAge = 16, pickupWindowStart = "16:00:00", pickupWindowEnd = "18:00:00",
    };

    private async Task<Guid> CreateEdition(Staff admin, string tag)
    {
        var response = await h.Send(HttpMethod.Post, "/api/v1/edition", admin.Headers, NewEdition(tag));
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await Harness.Data(response)).GetProperty("id").GetGuid();
    }

    private static readonly object[] FreeRules =
    [
        new { ageMin = 3, ageMax = 16, kind = "Free" },
        new { ageMin = 17, kind = "Free" },
    ];

    private async Task PublishFree(Guid edition, Staff drafter, Staff approver)
    {
        var draft = await h.Send(HttpMethod.Post, $"/api/v1/edition/{edition}/rates", drafter.Headers, new { note = "Bootcamp 2026 is free", rules = FreeRules });
        draft.StatusCode.Should().Be(HttpStatusCode.Created, await draft.Content.ReadAsStringAsync());
        var version = (await Harness.Data(draft)).GetProperty("versionId").GetGuid();
        (await h.Send(HttpMethod.Post, $"/api/v1/edition/{edition}/rates/{version}/approve", approver.Headers, new { })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await h.Send(HttpMethod.Post, $"/api/v1/edition/{edition}/rates/{version}/publish", approver.Headers, new { })).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private Task<HttpResponseMessage> Stage(Guid edition, Staff staff, string to) =>
        h.Send(HttpMethod.Post, $"/api/v1/edition/{edition}/stage", staff.Headers, new { to, reason = "test" });

    [Fact]
    public async Task Creating_an_edition_sets_days_capabilities_off_and_is_not_public()
    {
        var admin = await h.StaffWith("super_admin");
        var id = await CreateEdition(admin, "a1b2");
        var edition = await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/edition/{id}", admin.Headers));

        edition.GetProperty("stage").GetString().Should().Be("Draft");
        edition.GetProperty("days").GetArrayLength().Should().Be(3);
        edition.GetProperty("capabilities").EnumerateArray().Should().HaveCount(6).And.OnlyContain(c => !c.GetProperty("enabled").GetBoolean());
        edition.GetProperty("ageReferenceDate").GetString().Should().Be("2026-12-21");   // defaults to the first day

        (await h.Send(HttpMethod.Get, "/api/v1/public/edition/bb-a1b2")).StatusCode.Should().Be(HttpStatusCode.NotFound);   // drafts are never public
        var duplicate = await h.Send(HttpMethod.Post, "/api/v1/edition", admin.Headers, NewEdition("a1b2"));
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Harness.Code(duplicate)).Should().Be("duplicate");
    }

    [Theory]
    [InlineData("ZZ", "2026-12-21", "2026-12-23")]   // unknown template
    [InlineData("BB", "2026-12-23", "2026-12-21")]   // ends before it starts
    public async Task An_invalid_edition_is_refused(string template, string starts, string ends)
    {
        var admin = await h.StaffWith("super_admin");
        var response = await h.Send(HttpMethod.Post, "/api/v1/edition", admin.Headers,
            new { templateCode = template, slug = $"x-{Guid.NewGuid():N}"[..10], code = "X" + Guid.NewGuid().ToString("N")[..6], name = "X", startsOn = starts, endsOn = ends });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task An_edition_can_be_updated_and_the_camp_ages_are_checked()
    {
        var admin = await h.StaffWith("super_admin");
        var id = await CreateEdition(admin, "upd8");
        var ok = await h.Send(HttpMethod.Put, $"/api/v1/edition/{id}", admin.Headers, new { name = "Bible Bootcamp 2026 (Faith Plaza)", campMinAge = 10, campMaxAge = 16, campCapacity = 120 });
        ok.StatusCode.Should().Be(HttpStatusCode.OK, await ok.Content.ReadAsStringAsync());
        (await Harness.Data(ok)).GetProperty("campCapacity").GetInt32().Should().Be(120);

        var bad = await h.Send(HttpMethod.Put, $"/api/v1/edition/{id}", admin.Headers, new { name = "X", campMinAge = 16, campMaxAge = 10 });
        bad.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Capabilities_are_per_edition_switches()
    {
        var admin = await h.StaffWith("super_admin");
        var id = await CreateEdition(admin, "c4p1");
        foreach (var capability in new[] { "DailyCheckin", "CampReview" })
            (await h.Send(HttpMethod.Put, $"/api/v1/edition/{id}/capability/{capability}", admin.Headers, new { enabled = true })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await h.Send(HttpMethod.Put, $"/api/v1/edition/{id}/capability/Teleport", admin.Headers, new { enabled = true })).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var edition = await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/edition/{id}", admin.Headers));
        edition.GetProperty("capabilities").EnumerateArray().Where(c => c.GetProperty("enabled").GetBoolean())
            .Select(c => c.GetProperty("capability").GetString()).Should().BeEquivalentTo("DailyCheckin", "CampReview");
    }

    [Fact]
    public async Task Registration_cannot_open_without_a_published_price_and_free_renders_from_an_explicit_rule()
    {
        var admin = await h.StaffWith("super_admin");
        var drafter = await h.StaffWith("event_admin");
        var finance = await h.StaffWith("finance_officer");
        var id = await CreateEdition(admin, "fr33");
        (await Stage(id, admin, "Preview")).StatusCode.Should().Be(HttpStatusCode.OK);

        var blocked = await Stage(id, admin, "RegistrationOpen");
        blocked.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Harness.Code(blocked)).Should().Be("pricing_not_published");

        await PublishFree(id, drafter, finance);
        (await Stage(id, admin, "RegistrationOpen")).StatusCode.Should().Be(HttpStatusCode.OK);

        var publicEdition = await Harness.Data(await h.Send(HttpMethod.Get, "/api/v1/public/edition/bb-fr33"));
        publicEdition.GetProperty("price").GetProperty("state").GetString().Should().Be("Free");
        publicEdition.GetProperty("registrationWindow").GetString().Should().Be("Open");
        publicEdition.GetProperty("venue").GetProperty("name").GetString().Should().Be("Faith Plaza");
        publicEdition.GetProperty("pickupWindow").GetProperty("start").GetString().Should().Be("16:00:00");
        // The id is public because a registration is started with it; it grants nothing, every action is permission-checked.
        // The camp's capacity stays out of public output.
        publicEdition.GetProperty("id").GetGuid().Should().Be(id);
        publicEdition.GetProperty("camp").TryGetProperty("capacity", out _).Should().BeFalse();

        var list = await Harness.Data(await h.Send(HttpMethod.Get, "/api/v1/public/edition"));
        list.EnumerateArray().Should().Contain(e => e.GetProperty("slug").GetString() == "bb-fr33");
    }

    [Fact]
    public async Task The_lifecycle_only_moves_along_valid_transitions_and_needs_a_reason()
    {
        var admin = await h.StaffWith("super_admin");
        var id = await CreateEdition(admin, "l1f3");
        (await Stage(id, admin, "Live")).StatusCode.Should().Be(HttpStatusCode.Conflict);          // draft to live is not allowed
        (await Stage(id, admin, "Nonsense")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await h.Send(HttpMethod.Post, $"/api/v1/edition/{id}/stage", admin.Headers, new { to = "Preview" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Stage(id, admin, "Preview")).StatusCode.Should().Be(HttpStatusCode.OK);

        await using var c = await app.Db.OpenConnectionAsync();
        (await c.ExecuteScalarAsync<int>("SELECT count(*) FROM audit_events WHERE action = 'edition.stage' AND reason = 'test' AND edition_id = @id", new { id })).Should().Be(1);
    }

    [Fact]
    public async Task A_drafter_cannot_approve_their_own_rates()
    {
        var both = await h.StaffWith("event_admin", "finance_officer");
        var other = await h.StaffWith("finance_officer");
        var admin = await h.StaffWith("super_admin");
        var id = await CreateEdition(admin, "sod1");
        var version = (await Harness.Data(await h.Send(HttpMethod.Post, $"/api/v1/edition/{id}/rates", both.Headers, new { rules = FreeRules }))).GetProperty("versionId").GetGuid();

        var self = await h.Send(HttpMethod.Post, $"/api/v1/edition/{id}/rates/{version}/approve", both.Headers, new { });
        self.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Harness.Code(self)).Should().Be("separation_of_duties");
        (await h.Send(HttpMethod.Post, $"/api/v1/edition/{id}/rates/{version}/approve", other.Headers, new { })).StatusCode.Should().Be(HttpStatusCode.OK);

        // Publishing needs approval first, and the database refuses a self-approval even if the service were bypassed.
        var second = (await Harness.Data(await h.Send(HttpMethod.Post, $"/api/v1/edition/{id}/rates", both.Headers, new { rules = FreeRules }))).GetProperty("versionId").GetGuid();
        await using var c = await app.Db.OpenConnectionAsync();
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => c.ExecuteAsync("UPDATE rate_plan_versions SET state = 'Approved', approved_by = drafted_by WHERE id = @second", new { second }));
        (await h.Send(HttpMethod.Post, $"/api/v1/edition/{id}/rates/{second}/publish", other.Headers, new { })).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Published_rates_are_immutable_and_a_new_publish_retires_the_old_one()
    {
        var admin = await h.StaffWith("super_admin");
        var drafter = await h.StaffWith("event_admin");
        var finance = await h.StaffWith("finance_officer");
        var id = await CreateEdition(admin, "1mm7");
        await PublishFree(id, drafter, finance);

        await using var c = await app.Db.OpenConnectionAsync();
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => c.ExecuteAsync(
            "UPDATE rate_rules SET age_min = 4 WHERE version_id IN (SELECT v.id FROM rate_plan_versions v JOIN rate_plans p ON p.id = v.plan_id WHERE p.edition_id = @id)", new { id }));
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => c.ExecuteAsync(
            "UPDATE rate_plan_versions SET note = 'x' WHERE state = 'Published' AND plan_id IN (SELECT id FROM rate_plans WHERE edition_id = @id)", new { id }));

        await PublishFree(id, drafter, finance);
        (await c.QueryAsync<string>(
            "SELECT v.state FROM rate_plan_versions v JOIN rate_plans p ON p.id = v.plan_id WHERE p.edition_id = @id ORDER BY v.version", new { id })).Should().Equal("Retired", "Published");
    }

    [Fact]
    public async Task Invalid_and_overlapping_rules_are_rejected()
    {
        var drafter = await h.StaffWith("event_admin");
        var admin = await h.StaffWith("super_admin");
        var id = await CreateEdition(admin, "rul3");
        Task<HttpResponseMessage> Draft(params object[] rules) => h.Send(HttpMethod.Post, $"/api/v1/edition/{id}/rates", drafter.Headers, new { rules });

        (await Draft(new { ageMin = 3, ageMax = 16, kind = "Amount" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);                                   // amount missing
        (await Draft(new { ageMin = 3, ageMax = 16, kind = "Free", amountKobo = 500 })).StatusCode.Should().Be(HttpStatusCode.BadRequest);                     // free with an amount
        var overlap = await Draft(new { ageMin = 3, ageMax = 16, kind = "Free" }, new { ageMin = 10, ageMax = 20, kind = "Free" });
        (await Harness.Code(overlap)).Should().Be("overlapping_rules");
        (await Draft()).StatusCode.Should().Be(HttpStatusCode.BadRequest);                                                                                     // no rules is not "free"
        (await h.Send(HttpMethod.Post, $"/api/v1/edition/{Guid.NewGuid()}/rates", drafter.Headers, new { rules = FreeRules })).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Admin_catalog_endpoints_respect_roles()
    {
        var usher = await h.StaffWith("volunteer_usher");
        var eventAdmin = await h.StaffWith("event_admin");
        (await h.Send(HttpMethod.Get, "/api/v1/edition", usher.Headers)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await h.Send(HttpMethod.Post, "/api/v1/edition", usher.Headers, NewEdition("us3r"))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await h.Send(HttpMethod.Get, "/api/v1/edition")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // An event admin may draft rates but never approve them.
        var admin = await h.StaffWith("super_admin");
        var id = await CreateEdition(admin, "r0l3");
        var version = (await Harness.Data(await h.Send(HttpMethod.Post, $"/api/v1/edition/{id}/rates", eventAdmin.Headers, new { rules = FreeRules }))).GetProperty("versionId").GetGuid();
        (await h.Send(HttpMethod.Post, $"/api/v1/edition/{id}/rates/{version}/approve", eventAdmin.Headers, new { })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Editions_are_never_deleted_or_toggled()
    {
        var admin = await h.StaffWith("super_admin");
        var id = await CreateEdition(admin, "n0d3");
        foreach (var (method, url) in new[] { (HttpMethod.Delete, $"/api/v1/edition/{id}"), (HttpMethod.Patch, $"/api/v1/edition/{id}/disable"), (HttpMethod.Patch, $"/api/v1/edition/{id}/enable") })
            (await h.Send(method, url, admin.Headers)).StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
    }
}
