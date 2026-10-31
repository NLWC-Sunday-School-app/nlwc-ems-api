using System.Net;
using Dapper;

namespace nlwc.ems.Tests.Integration;

[Collection("app")]
public sealed class PeopleTests(TestApp app)
{
    private readonly Harness h = new(app);

    private static string DateAgo(int years, int extraDays = 0) => DateTime.UtcNow.AddYears(-years).AddDays(extraDays).ToString("yyyy-MM-dd");

    private Task<HttpResponseMessage> AddKid(Harness.Session s, int ageYears, string name = "Tobi") =>
        h.Send(HttpMethod.Post, "/api/v1/dependant", s.Headers, new { givenName = name, familyName = "Obi", dateOfBirth = DateAgo(ageYears, -30), gender = "Male", relationship = "Parent" });

    private async Task<Guid> Kid(Harness.Session s, int age = 8)
    {
        var response = await AddKid(s, age);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await Harness.Data(response)).GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task The_profile_is_readable_and_asks_for_a_date_of_birth()
    {
        var u = await h.NewUser();
        var profile = await Harness.Data(await h.Send(HttpMethod.Get, "/api/v1/profile", u.Headers));
        profile.GetProperty("needsDateOfBirth").GetBoolean().Should().BeTrue();
        profile.GetProperty("email").GetString().Should().Be(u.Email);
    }

    [Fact]
    public async Task Date_of_birth_can_be_set_once_and_then_needs_review()
    {
        var u = await h.NewUser();
        object Body(string dob) => new { givenName = "Ada", familyName = "Obi", gender = "Female", country = "NG", city = "Lagos", dateOfBirth = dob };

        (await h.Send(HttpMethod.Put, "/api/v1/profile", u.Headers, Body("1990-05-17"))).StatusCode.Should().Be(HttpStatusCode.OK);
        (await Harness.Data(await h.Send(HttpMethod.Get, "/api/v1/profile", u.Headers))).GetProperty("needsDateOfBirth").GetBoolean().Should().BeFalse();
        (await h.Send(HttpMethod.Put, "/api/v1/profile", u.Headers, Body("1990-05-17"))).StatusCode.Should().Be(HttpStatusCode.OK);   // same value is fine

        var changed = await h.Send(HttpMethod.Put, "/api/v1/profile", u.Headers, Body("1991-05-17"));
        changed.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Harness.Code(changed)).Should().Be("dob_locked");

        await using var c = await app.Db.OpenConnectionAsync();
        (await c.ExecuteScalarAsync<int>("SELECT count(*) FROM audit_events WHERE action = 'person.dob_set' AND actor_account_id = @id", new { id = u.AccountId })).Should().Be(1);
    }

    [Theory]
    [InlineData("2999-01-01")]
    [InlineData("1800-01-01")]
    [InlineData("17/05/1990")]
    public async Task Invalid_dates_of_birth_are_rejected(string dob)
    {
        var u = await h.NewUser();
        var response = await h.Send(HttpMethod.Put, "/api/v1/profile", u.Headers, new { givenName = "A", familyName = "B", dateOfBirth = dob });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Dependants_are_limited_to_ages_0_to_12_and_have_no_account()
    {
        var u = await h.NewUser();
        (await AddKid(u, 12)).StatusCode.Should().Be(HttpStatusCode.Created);

        var tooOld = await AddKid(u, 13);
        tooOld.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Harness.Code(tooOld)).Should().Be("too_old_for_dependant");

        var list = await Harness.Data(await h.Send(HttpMethod.Get, "/api/v1/dependant", u.Headers));
        list.GetArrayLength().Should().Be(1);
        await using var c = await app.Db.OpenConnectionAsync();
        (await c.ExecuteScalarAsync<bool>("SELECT EXISTS (SELECT 1 FROM accounts WHERE person_id = @id)", new { id = list[0].GetProperty("personId").GetGuid() })).Should().BeFalse();
    }

    [Fact]
    public async Task One_guardian_cannot_see_or_change_another_guardians_child()
    {
        var owner = await h.NewUser();
        var other = await h.NewUser();
        var kid = await Kid(owner);

        (await Harness.Data(await h.Send(HttpMethod.Get, "/api/v1/dependant", other.Headers))).GetArrayLength().Should().Be(0);
        (await h.Send(HttpMethod.Get, $"/api/v1/dependant/{kid}/collector", other.Headers)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await h.Send(HttpMethod.Post, $"/api/v1/dependant/{kid}/collector", other.Headers, new { fullName = "X Y", relationship = "uncle", phone = "08031234567" })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await h.Send(HttpMethod.Get, $"/api/v1/dependant/{kid}/emergency-contact", other.Headers)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await h.Send(HttpMethod.Post, $"/api/v1/dependant/{kid}/remove", other.Headers, new { reason = "x" })).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Collectors_phones_are_normalised_and_stored_encrypted()
    {
        var u = await h.NewUser();
        var kid = await Kid(u);
        var add = await h.Send(HttpMethod.Post, $"/api/v1/dependant/{kid}/collector", u.Headers,
            new { fullName = "Chidi Obi", relationship = "uncle", phone = "0803 123 4567", validDays = new[] { "2026-12-21", "2026-12-22" } });
        add.StatusCode.Should().Be(HttpStatusCode.Created, await add.Content.ReadAsStringAsync());

        var list = await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/dependant/{kid}/collector", u.Headers));
        list[0].GetProperty("phone").GetString().Should().Be("+2348031234567");
        list[0].GetProperty("validDays").GetArrayLength().Should().Be(2);

        await using var c = await app.Db.OpenConnectionAsync();
        var raw = await c.ExecuteScalarAsync<byte[]>("SELECT phone_encrypted FROM authorized_collectors WHERE child_person_id = @kid", new { kid });
        System.Text.Encoding.UTF8.GetString(raw!).Should().NotContain("8031234567");

        (await h.Send(HttpMethod.Post, $"/api/v1/dependant/{kid}/collector", u.Headers, new { fullName = "Bad", relationship = "x", phone = "123" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await h.Send(HttpMethod.Post, $"/api/v1/dependant/{kid}/collector", u.Headers, new { fullName = "Bad", relationship = "x", phone = "08031234567", validDays = Array.Empty<string>() })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_collector_with_no_days_is_valid_every_day()
    {
        var u = await h.NewUser();
        var kid = await Kid(u);
        await h.Send(HttpMethod.Post, $"/api/v1/dependant/{kid}/collector", u.Headers, new { fullName = "A B", relationship = "aunt", phone = "+2348031234567" });
        var list = await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/dependant/{kid}/collector", u.Headers));
        list[0].GetProperty("validDays").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Null);
    }

    [Fact]
    public async Task A_removed_collector_is_no_longer_listed_but_the_row_is_kept()
    {
        var u = await h.NewUser();
        var kid = await Kid(u);
        var id = (await Harness.Data(await h.Send(HttpMethod.Post, $"/api/v1/dependant/{kid}/collector", u.Headers, new { fullName = "A B", relationship = "aunt", phone = "+2348031234567" }))).GetProperty("id").GetGuid();

        (await h.Send(HttpMethod.Delete, $"/api/v1/dependant/{kid}/collector/{id}", u.Headers)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/dependant/{kid}/collector", u.Headers))).GetArrayLength().Should().Be(0);
        await using var c = await app.Db.OpenConnectionAsync();
        (await c.ExecuteScalarAsync<bool>("SELECT removed_at IS NOT NULL FROM authorized_collectors WHERE id = @id", new { id })).Should().BeTrue();
        (await h.Send(HttpMethod.Delete, $"/api/v1/dependant/{kid}/collector/{id}", u.Headers)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Adding_and_removing_a_collector_is_audited_without_the_phone_number()
    {
        var u = await h.NewUser();
        var kid = await Kid(u);
        var id = (await Harness.Data(await h.Send(HttpMethod.Post, $"/api/v1/dependant/{kid}/collector", u.Headers, new { fullName = "Audit Me", relationship = "aunt", phone = "+2348039998888" }))).GetProperty("id").GetGuid();
        await h.Send(HttpMethod.Delete, $"/api/v1/dependant/{kid}/collector/{id}", u.Headers);

        await using var c = await app.Db.OpenConnectionAsync();
        var rows = (await c.QueryAsync<(string Action, string Detail)>("SELECT action, detail FROM audit_events WHERE entity_type = 'AuthorizedCollector' AND record_id = @id ORDER BY seq", new { id = id.ToString() })).ToList();
        rows.Select(r => r.Action).Should().Equal("Create", "Update");
        rows.Should().OnlyContain(r => !r.Detail.Contains("8039998888"));
    }

    [Fact]
    public async Task The_emergency_contact_is_one_per_person_and_replaceable()
    {
        var u = await h.NewUser();
        var kid = await Kid(u);
        object Body(string name) => new { fullName = name, relationship = "aunt", phone = "08031234567" };
        await h.Send(HttpMethod.Put, $"/api/v1/dependant/{kid}/emergency-contact", u.Headers, Body("First"));
        await h.Send(HttpMethod.Put, $"/api/v1/dependant/{kid}/emergency-contact", u.Headers, Body("Second"));
        (await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/dependant/{kid}/emergency-contact", u.Headers))).GetProperty("fullName").GetString().Should().Be("Second");

        await h.Send(HttpMethod.Put, "/api/v1/emergency-contact", u.Headers, Body("Mine"));
        (await Harness.Data(await h.Send(HttpMethod.Get, "/api/v1/profile", u.Headers))).GetProperty("emergencyContact").GetProperty("fullName").GetString().Should().Be("Mine");

        await using var c = await app.Db.OpenConnectionAsync();
        (await c.ExecuteScalarAsync<int>("SELECT count(*) FROM emergency_contacts WHERE person_id = @kid", new { kid })).Should().Be(1);
    }

    [Fact]
    public async Task Removing_a_dependant_ends_guardianship_with_a_reason()
    {
        var u = await h.NewUser();
        var kid = await Kid(u);
        (await h.Send(HttpMethod.Post, $"/api/v1/dependant/{kid}/remove", u.Headers, new { })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await h.Send(HttpMethod.Post, $"/api/v1/dependant/{kid}/remove", u.Headers, new { reason = "added by mistake" })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await Harness.Data(await h.Send(HttpMethod.Get, "/api/v1/dependant", u.Headers))).GetArrayLength().Should().Be(0);
        (await h.Send(HttpMethod.Get, $"/api/v1/dependant/{kid}/collector", u.Headers)).StatusCode.Should().Be(HttpStatusCode.NotFound);

        await using var c = await app.Db.OpenConnectionAsync();
        (await c.ExecuteScalarAsync<int>("SELECT count(*) FROM audit_events WHERE action = 'dependant.removed' AND reason = 'added by mistake'")).Should().Be(1);
    }

    [Fact]
    public async Task These_endpoints_need_a_session_and_csrf()
    {
        (await h.Send(HttpMethod.Get, "/api/v1/profile")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var u = await h.NewUser();
        var noCsrf = new Dictionary<string, string> { ["Cookie"] = u.Headers["Cookie"] };
        (await h.Send(HttpMethod.Put, "/api/v1/profile", noCsrf, new { givenName = "A", familyName = "B" })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Rows_are_stamped_with_who_created_them()
    {
        var u = await h.NewUser();
        var kid = await Kid(u);
        await using var c = await app.Db.OpenConnectionAsync();

        // A child added by a signed-in guardian is stamped with that guardian's account...
        // (The IP stamp needs a real connection; the in-memory test server has no remote address.)
        var child = await c.QuerySingleAsync<(string? CreatedBy, string Status)>("SELECT created_by, status FROM people WHERE id = @kid", new { kid });
        child.CreatedBy.Should().Be(u.AccountId.ToString());
        child.Status.Should().Be("Active");

        // ...while the person created at sign-up has no signed-in actor yet, which is stamped as absent rather than failing the save.
        var self = await c.QuerySingleAsync<(string? CreatedBy, DateTime DateCreated)>("SELECT created_by, date_created FROM people WHERE id = @id", new { id = u.PersonId });
        self.CreatedBy.Should().BeNull();
        self.DateCreated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
    }
}
