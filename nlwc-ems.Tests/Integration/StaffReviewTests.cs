using System.Net;
using Dapper;
using Staff = nlwc.ems.Tests.Integration.Harness.Session;

namespace nlwc.ems.Tests.Integration;

[Collection("app")]
public sealed class StaffReviewTests(TestApp app)
{
    private readonly Harness h = new(app);

    private async Task<Staff> Adult(string family, string given = "Ada", string dob = "1990-05-17")
    {
        var u = await h.NewUser();
        (await h.Send(HttpMethod.Put, "/api/v1/profile", u.Headers, new { givenName = given, familyName = family, dateOfBirth = dob })).StatusCode.Should().Be(HttpStatusCode.OK);
        return u;
    }

    private static string Tag() => Guid.NewGuid().ToString("N")[..8];

    // ---- people ----
    [Fact]
    public async Task People_search_and_detail_need_people_access()
    {
        var family = $"Searchable{Tag()}";
        var person = await Adult(family);
        var officer = await h.StaffWith("registration_officer");
        var outsider = await h.NewUser();

        var found = await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/admin/person?q={family.ToLowerInvariant()}", officer.Headers));
        found.GetProperty("count").GetInt32().Should().Be(1);
        found.GetProperty("data")[0].GetProperty("id").GetGuid().Should().Be(person.PersonId);
        (await Harness.Data(await h.Send(HttpMethod.Get, "/api/v1/admin/person?q=%25", officer.Headers))).GetProperty("count").GetInt32().Should().Be(0);   // % is not a wildcard

        var detail = await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/admin/person/{person.PersonId}", officer.Headers));
        detail.GetProperty("person").GetProperty("familyName").GetString().Should().Be(family);
        detail.ToString().Should().NotContain(person.Email);   // no contact details in the staff view

        (await h.Send(HttpMethod.Get, "/api/v1/admin/person", outsider.Headers)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await h.Send(HttpMethod.Get, $"/api/v1/admin/person/{Guid.NewGuid()}", officer.Headers)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_guardian_and_their_dependant_see_each_other_in_the_detail()
    {
        var guardian = await Adult($"Guard{Tag()}");
        var kid = await h.Send(HttpMethod.Post, "/api/v1/dependant", guardian.Headers,
            new { givenName = "Tobi", familyName = "Obi", dateOfBirth = "2017-05-01", gender = "Male", relationship = "Parent" });
        var kidId = (await Harness.Data(kid)).GetProperty("id").GetGuid();
        var officer = await h.StaffWith("registration_officer");

        var asGuardian = await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/admin/person/{guardian.PersonId}", officer.Headers));
        asGuardian.GetProperty("relations").EnumerateArray().Should().ContainSingle(r => r.GetProperty("relation").GetString() == "Dependant" && r.GetProperty("personId").GetGuid() == kidId);
        var asKid = await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/admin/person/{kidId}", officer.Headers));
        asKid.GetProperty("relations").EnumerateArray().Should().ContainSingle(r => r.GetProperty("relation").GetString() == "Guardian" && r.GetProperty("personId").GetGuid() == guardian.PersonId);
    }

    [Fact]
    public async Task Look_alikes_are_offered_until_staff_say_they_are_different_people()
    {
        var family = $"Twin{Tag()}";
        var a = await Adult(family, "Adaeze");
        var b = await Adult(family, "Ada");
        var different = await Adult(family, "Zainab");   // same birthday and family name, but a different first name
        var officer = await h.StaffWith("registration_officer");
        var viewer = await h.StaffWith("auditor");   // can't edit people

        async Task<List<(Guid, Guid)>> Mine() => (await Harness.Data(await h.Send(HttpMethod.Get, "/api/v1/admin/person/duplicates", officer.Headers))).EnumerateArray()
            .Select(p => (p.GetProperty("a").GetProperty("id").GetGuid(), p.GetProperty("b").GetProperty("id").GetGuid()))
            .Where(p => new[] { a.PersonId, b.PersonId, different.PersonId }.Contains(p.Item1)).ToList();

        var offered = await Mine();
        offered.Should().ContainSingle();
        new[] { offered[0].Item1, offered[0].Item2 }.Should().BeEquivalentTo([a.PersonId, b.PersonId]);

        var url = "/api/v1/admin/person/duplicates/dismiss";
        (await h.Send(HttpMethod.Post, url, viewer.Headers, new { personAId = a.PersonId, personBId = b.PersonId, reason = "Different" })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await h.Send(HttpMethod.Post, url, officer.Headers, new { personAId = a.PersonId, personBId = a.PersonId, reason = "Same" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await h.Send(HttpMethod.Post, url, officer.Headers, new { personAId = a.PersonId, personBId = b.PersonId, reason = "" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        (await h.Send(HttpMethod.Post, url, officer.Headers, new { personAId = b.PersonId, personBId = a.PersonId, reason = "Cousins, not the same person" })).StatusCode.Should().Be(HttpStatusCode.OK);   // either way round
        (await Mine()).Should().BeEmpty();
        var again = await h.Send(HttpMethod.Post, url, officer.Headers, new { personAId = a.PersonId, personBId = b.PersonId, reason = "Again" });
        again.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Harness.Code(again)).Should().Be("already_reviewed");
    }

    // ---- spouses ----
    [Fact]
    public async Task Staff_pair_two_adult_spouses_and_can_remove_the_link()
    {
        var husband = await Adult($"Pair{Tag()}", "Emeka", "1985-03-02");
        var wife = await Adult($"Pair{Tag()}", "Ngozi", "1987-09-09");
        var third = await Adult($"Pair{Tag()}", "Chidi", "1988-01-01");
        var officer = await h.StaffWith("registration_officer");
        var viewer = await h.StaffWith("auditor");
        var pair = $"/api/v1/admin/person/{husband.PersonId}/spouse";

        (await h.Send(HttpMethod.Post, pair, viewer.Headers, new { spousePersonId = wife.PersonId, reason = "Married" })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await h.Send(HttpMethod.Post, pair, officer.Headers, new { spousePersonId = husband.PersonId, reason = "Self" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await h.Send(HttpMethod.Post, pair, officer.Headers, new { spousePersonId = wife.PersonId, reason = "Marriage certificate seen" })).StatusCode.Should().Be(HttpStatusCode.OK);

        foreach (var (p, other) in new[] { (husband, wife), (wife, husband) })
        {
            var detail = await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/admin/person/{p.PersonId}", officer.Headers));
            detail.GetProperty("relations").EnumerateArray().Should().ContainSingle(r => r.GetProperty("relation").GetString() == "Spouse" && r.GetProperty("personId").GetGuid() == other.PersonId);
        }
        var taken = await h.Send(HttpMethod.Post, $"/api/v1/admin/person/{third.PersonId}/spouse", officer.Headers, new { spousePersonId = wife.PersonId, reason = "Second spouse?" });
        taken.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Harness.Code(taken)).Should().Be("already_paired");

        var remove = $"/api/v1/admin/person/{husband.PersonId}/spouse/remove";
        (await h.Send(HttpMethod.Post, remove, officer.Headers, new { reason = "Entered in error" })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/admin/person/{wife.PersonId}", officer.Headers))).GetProperty("relations").GetArrayLength().Should().Be(0);
        (await h.Send(HttpMethod.Post, remove, officer.Headers, new { reason = "Again" })).StatusCode.Should().Be(HttpStatusCode.NotFound);

        await using var c = await app.Db.OpenConnectionAsync();
        (await c.ExecuteScalarAsync<int>("SELECT count(*) FROM audit_events WHERE action IN ('spouse.paired','spouse.unpaired') AND record_id = @id", new { id = husband.PersonId.ToString() })).Should().Be(2);
    }

    [Fact]
    public async Task A_child_cannot_be_paired_as_a_spouse()
    {
        var parent = await Adult($"Kid{Tag()}");
        var adult = await Adult($"Kid{Tag()}", "Emeka");
        var kid = await Harness.Data(await h.Send(HttpMethod.Post, "/api/v1/dependant", parent.Headers,
            new { givenName = "Tobi", familyName = "Obi", dateOfBirth = "2017-05-01", gender = "Male", relationship = "Parent" }));
        var officer = await h.StaffWith("registration_officer");
        var response = await h.Send(HttpMethod.Post, $"/api/v1/admin/person/{adult.PersonId}/spouse", officer.Headers, new { spousePersonId = kid.GetProperty("id").GetGuid(), reason = "No" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Harness.Code(response)).Should().Be("spouse_must_be_adult");
    }

    // ---- camp review ----
    private async Task<(Guid Edition, Staff Family, Guid RegistrationA, Guid RegistrationB)> CampEdition(int? capacity, int kids = 2)
    {
        var admin = await h.StaffWith("super_admin");
        var tag = Tag();
        var created = await h.Send(HttpMethod.Post, "/api/v1/edition", admin.Headers, new
        {
            templateCode = "BB", slug = $"bb-{tag}", code = $"C{tag[..4]}", name = "Bible Bootcamp 2026", startsOn = "2026-12-21", endsOn = "2026-12-23",
            venueName = "Faith Plaza", campMinAge = 10, campMaxAge = 16, campCapacity = capacity
        });
        created.StatusCode.Should().Be(HttpStatusCode.OK, await created.Content.ReadAsStringAsync());
        var edition = (await Harness.Data(created)).GetProperty("id").GetGuid();
        await using (var c = await app.Db.OpenConnectionAsync())
            await c.ExecuteAsync("UPDATE event_editions SET registration_window = 'Open' WHERE id = @edition", new { edition });

        var family = await Adult($"Camp{Tag()}");
        var ids = new List<Guid>();
        for (var i = 0; i < kids; i++)
        {
            var kid = (await Harness.Data(await h.Send(HttpMethod.Post, "/api/v1/dependant", family.Headers,
                new { givenName = $"Kid{i}", familyName = "Camper", dateOfBirth = "2014-06-01", gender = "Male", relationship = "Parent" }))).GetProperty("id").GetGuid();
            var registration = (await Harness.Data(await h.Send(HttpMethod.Post, "/api/v1/registration", family.Headers, new { editionId = edition, personId = kid, participationMode = "Camp" }))).GetProperty("id").GetGuid();
            (await h.Send(HttpMethod.Put, $"/api/v1/dependant/{kid}/emergency-contact", family.Headers, new { fullName = "Ngozi Obi", relationship = "Aunt", phone = "08012345678" })).StatusCode.Should().Be(HttpStatusCode.OK);
            (await h.Send(HttpMethod.Put, $"/api/v1/registration/{registration}/health", family.Headers, new { none = true })).StatusCode.Should().Be(HttpStatusCode.OK);
            foreach (var kind in new[] { "Terms", "GuardianAuthorization", "MedicalEmergency" })
                (await h.Send(HttpMethod.Put, $"/api/v1/registration/{registration}/consent/{kind}", family.Headers, new { accepted = true })).StatusCode.Should().Be(HttpStatusCode.OK);
            (await h.Send(HttpMethod.Post, $"/api/v1/registration/{registration}/submit", family.Headers, new { })).StatusCode.Should().Be(HttpStatusCode.OK);
            ids.Add(registration);
        }
        return (edition, family, ids[0], ids.Count > 1 ? ids[1] : ids[0]);
    }

    [Fact]
    public async Task The_camp_queue_lists_requests_with_no_decision_and_hides_flags_without_access()
    {
        var (edition, _, a, _) = await CampEdition(capacity: 10);
        var officer = await h.StaffWith("registration_officer");   // health:flags
        var finance = await h.StaffWith("finance_officer");       // no flags

        var queue = await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/admin/camp?editionId={edition}", officer.Headers));
        queue.GetProperty("capacity").GetInt32().Should().Be(10);
        queue.GetProperty("approved").GetInt32().Should().Be(0);
        var rows = queue.GetProperty("rows").EnumerateArray().ToList();
        rows.Should().HaveCount(2);
        rows.Should().OnlyContain(r => r.GetProperty("decision").ValueKind == System.Text.Json.JsonValueKind.Null && r.GetProperty("guardianConsentGiven").GetBoolean() && r.GetProperty("age").GetInt32() == 12);
        rows.Should().Contain(r => r.GetProperty("registrationId").GetGuid() == a);

        var asFinance = await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/admin/camp?editionId={edition}", finance.Headers));
        asFinance.GetProperty("rows")[0].GetProperty("hasHealthFlags").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Null);
        (await h.Send(HttpMethod.Get, $"/api/v1/admin/camp?editionId={edition}", (await h.NewUser()).Headers)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Approval_needs_capacity_and_room_but_waitlisting_does_not()
    {
        var (edition, _, a, b) = await CampEdition(capacity: null);
        var officer = await h.StaffWith("registration_officer");
        var viewer = await h.StaffWith("finance_officer");   // can view, cannot edit
        string Decide(Guid id) => $"/api/v1/admin/camp/{id}/decision";

        (await h.Send(HttpMethod.Post, Decide(a), viewer.Headers, new { decision = "Approved", reason = "Fit" })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await h.Send(HttpMethod.Post, Decide(a), officer.Headers, new { decision = "Approved", reason = "" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var noCapacity = await h.Send(HttpMethod.Post, Decide(a), officer.Headers, new { decision = "Approved", reason = "Fit to camp" });
        noCapacity.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Harness.Code(noCapacity)).Should().Be("camp_capacity_not_set");
        (await h.Send(HttpMethod.Post, Decide(a), officer.Headers, new { decision = "Waitlisted", reason = "Capacity not set yet" })).StatusCode.Should().Be(HttpStatusCode.OK);

        await using var c = await app.Db.OpenConnectionAsync();
        await c.ExecuteAsync("UPDATE event_editions SET camp_capacity = 1 WHERE id = @edition", new { edition });
        (await h.Send(HttpMethod.Post, Decide(a), officer.Headers, new { decision = "Approved", reason = "Space now" })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await h.Send(HttpMethod.Post, Decide(a), officer.Headers, new { decision = "Approved", reason = "Re-saving the same place" })).StatusCode.Should().Be(HttpStatusCode.OK);   // keeps its own place
        var full = await h.Send(HttpMethod.Post, Decide(b), officer.Headers, new { decision = "Approved", reason = "One more" });
        full.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Harness.Code(full)).Should().Be("camp_full");
        (await h.Send(HttpMethod.Post, Decide(b), officer.Headers, new { decision = "Waitlisted", reason = "Camp is full" })).StatusCode.Should().Be(HttpStatusCode.OK);

        var queue = await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/admin/camp?editionId={edition}", officer.Headers));
        queue.GetProperty("approved").GetInt32().Should().Be(1);
        var rows = queue.GetProperty("rows").EnumerateArray().ToDictionary(r => r.GetProperty("registrationId").GetGuid());
        rows[a].GetProperty("decision").GetString().Should().Be("Approved");
        rows[b].GetProperty("decision").GetString().Should().Be("Waitlisted");
        rows[b].GetProperty("decisionReason").GetString().Should().Be("Camp is full");

        (await c.ExecuteScalarAsync<int>("SELECT count(*) FROM camp_decisions WHERE registration_id = @a", new { a })).Should().Be(3);   // every decision stays on record
        (await h.Send(HttpMethod.Post, Decide(Guid.NewGuid()), officer.Headers, new { decision = "Declined", reason = "x" })).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Only_a_submitted_camp_request_can_be_reviewed()
    {
        var (edition, family, a, _) = await CampEdition(capacity: 5, kids: 1);
        var officer = await h.StaffWith("registration_officer");
        (await h.Send(HttpMethod.Post, $"/api/v1/registration/{a}/cancel", family.Headers, new { reason = "Changed mind" })).StatusCode.Should().Be(HttpStatusCode.OK);
        var response = await h.Send(HttpMethod.Post, $"/api/v1/admin/camp/{a}/decision", officer.Headers, new { decision = "Approved", reason = "Fit" });
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Harness.Code(response)).Should().Be("camp_not_reviewable");
        (await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/admin/camp?editionId={edition}", officer.Headers))).GetProperty("rows").GetArrayLength().Should().Be(0);
    }
}
