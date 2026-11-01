using System.Net;
using Dapper;
using Staff = nlwc.ems.Tests.Integration.Harness.Session;

namespace nlwc.ems.Tests.Integration;

[Collection("app")]
public sealed class StaffGapsTests(TestApp app)
{
    private readonly Harness h = new(app);

    private async Task<Guid> NewEdition(Staff admin)
    {
        var tag = Guid.NewGuid().ToString("N")[..4];
        var response = await h.Send(HttpMethod.Post, "/api/v1/edition", admin.Headers, new
        {
            templateCode = "BB", slug = $"bb-{tag}", code = $"B{tag}", name = "Bible Bootcamp 2026", startsOn = "2026-12-21", endsOn = "2026-12-23"
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await Harness.Data(response)).GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Me_reports_enrolment_and_a_confirmed_authenticator_cannot_be_replaced()
    {
        var u = await h.NewUser("admin", "m");
        (await Harness.Data(await h.Send(HttpMethod.Get, "/api/v1/auth/me", u.Headers))).GetProperty("mfaEnrolled").GetBoolean().Should().BeFalse();

        await h.VerifyMfa(u);   // enrols and confirms
        (await Harness.Data(await h.Send(HttpMethod.Get, "/api/v1/auth/me", u.Headers))).GetProperty("mfaEnrolled").GetBoolean().Should().BeTrue();

        var again = await h.Send(HttpMethod.Post, "/api/v1/auth/mfa/enroll", u.Headers, new { });
        again.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Harness.Code(again)).Should().Be("mfa_already_enrolled");
        await using var c = await app.Db.OpenConnectionAsync();
        (await c.ExecuteScalarAsync<int>("SELECT count(*) FROM mfa_factors WHERE account_id = @id", new { id = u.AccountId })).Should().Be(1);
    }

    [Fact]
    public async Task Pending_enrolment_can_be_restarted()
    {
        var u = await h.NewUser("admin", "p");
        (await h.Send(HttpMethod.Post, "/api/v1/auth/mfa/enroll", u.Headers, new { })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await h.Send(HttpMethod.Post, "/api/v1/auth/mfa/enroll", u.Headers, new { })).StatusCode.Should().Be(HttpStatusCode.OK);   // not confirmed yet
        (await Harness.Data(await h.Send(HttpMethod.Get, "/api/v1/auth/me", u.Headers))).GetProperty("mfaEnrolled").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task My_editions_follow_the_callers_grants()
    {
        var admin = await h.StaffWith("super_admin");
        var one = await NewEdition(admin);
        var two = await NewEdition(admin);

        var all = (await Harness.Data(await h.Send(HttpMethod.Get, "/api/v1/edition/mine", admin.Headers))).EnumerateArray().Select(e => e.GetProperty("id").GetGuid()).ToList();
        all.Should().Contain([one, two]);

        var scoped = await h.NewUser("admin", "s");
        await h.Grant(scoped.AccountId, "medical_welfare", one);   // a role that cannot list editions through config:view
        await h.VerifyMfa(scoped);
        var mine = (await Harness.Data(await h.Send(HttpMethod.Get, "/api/v1/edition/mine", scoped.Headers))).EnumerateArray().ToList();
        mine.Should().ContainSingle().Which.GetProperty("id").GetGuid().Should().Be(one);
        mine[0].GetProperty("stage").GetString().Should().Be("Draft");
    }

    [Fact]
    public async Task My_editions_needs_a_staff_grant_and_a_verified_second_factor()
    {
        var family = await h.NewUser();
        (await h.Send(HttpMethod.Get, "/api/v1/edition/mine", family.Headers)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var staff = await h.NewUser("admin", "n");
        await h.Grant(staff.AccountId, "super_admin");   // no MFA yet
        var response = await h.Send(HttpMethod.Get, "/api/v1/edition/mine", staff.Headers);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Harness.Code(response)).Should().Be("mfa_required");
    }

    [Fact]
    public async Task Rate_versions_can_be_listed_with_their_rules_and_who_acted()
    {
        var admin = await h.StaffWith("super_admin");
        var edition = await NewEdition(admin);
        var drafter = await h.StaffWith("event_admin");
        (await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/edition/{edition}/rates", drafter.Headers))).GetArrayLength().Should().Be(0);

        object[] free = [new { ageMin = 3, ageMax = 16, kind = "Free" }, new { ageMin = 17, kind = "Free" }];
        var first = (await Harness.Data(await h.Send(HttpMethod.Post, $"/api/v1/edition/{edition}/rates", drafter.Headers, new { note = "v1", rules = free }))).GetProperty("versionId").GetGuid();
        await h.Send(HttpMethod.Post, $"/api/v1/edition/{edition}/rates", drafter.Headers, new { note = "v2", rules = new object[] { new { ageMin = 0, kind = "Amount", amountKobo = 500000 } } });

        var list = (await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/edition/{edition}/rates", drafter.Headers))).EnumerateArray().ToList();
        list.Select(v => v.GetProperty("version").GetInt32()).Should().Equal(2, 1);
        list[0].GetProperty("rules")[0].GetProperty("amountKobo").GetInt64().Should().Be(500000);
        list[1].GetProperty("versionId").GetGuid().Should().Be(first);
        list[1].GetProperty("state").GetString().Should().Be("Draft");
        list[1].GetProperty("draftedBy").GetGuid().Should().Be(drafter.AccountId);
        list[1].GetProperty("rules").GetArrayLength().Should().Be(2);

        var family = await h.NewUser();
        (await h.Send(HttpMethod.Get, $"/api/v1/edition/{edition}/rates", family.Headers)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Staff_can_find_an_account_by_email_to_grant_a_role()
    {
        var admin = await h.StaffWith("super_admin");
        var person = await h.NewUser();

        var found = await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/rolegrant/lookup?email={Uri.EscapeDataString(person.Email)}", admin.Headers));
        found.GetProperty("accountId").GetGuid().Should().Be(person.AccountId);
        found.GetProperty("givenName").GetString().Should().Be("Ada");

        var missing = await h.Send(HttpMethod.Get, "/api/v1/rolegrant/lookup?email=nobody@example.com", admin.Headers);
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Harness.Code(missing)).Should().Be("account_not_found");

        var officer = await h.StaffWith("registration_officer");   // no staff:edit
        (await h.Send(HttpMethod.Get, $"/api/v1/rolegrant/lookup?email={Uri.EscapeDataString(person.Email)}", officer.Headers)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await h.Send(HttpMethod.Get, $"/api/v1/rolegrant/lookup?email={Uri.EscapeDataString(person.Email)}", person.Headers)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Coming_soon_lists_programmes_with_no_public_edition()
    {
        var before = (await Harness.Data(await h.Send(HttpMethod.Get, "/api/v1/public/coming-soon"))).EnumerateArray().Select(e => e.GetProperty("code").GetString()).ToList();
        before.Should().Contain(["BB", "SS", "CP"]);   // seeded templates, none public yet in a fresh database or already public elsewhere

        var admin = await h.StaffWith("super_admin");
        var edition = await NewEdition(admin);
        await using var c = await app.Db.OpenConnectionAsync();
        await c.ExecuteAsync("UPDATE event_editions SET visibility = 'Public' WHERE id = @edition", new { edition });
        var after = (await Harness.Data(await h.Send(HttpMethod.Get, "/api/v1/public/coming-soon"))).EnumerateArray().Select(e => e.GetProperty("code").GetString()).ToList();
        after.Should().NotContain("BB");   // a template with a public edition is no longer "coming soon"
        after.Should().Contain("SS");
    }
}
