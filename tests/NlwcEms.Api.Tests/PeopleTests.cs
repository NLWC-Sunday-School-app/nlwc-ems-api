using System.Net;
using Dapper;

namespace NlwcEms.Api.Tests;

[Collection("app")]
public sealed class PeopleTests(TestApp app)
{
    private readonly Harness h = new(app);

    private static string Today(int yearsAgo, int extraDays = 0) => DateTime.UtcNow.AddYears(-yearsAgo).AddDays(extraDays).ToString("yyyy-MM-dd");

    private Task<HttpResponseMessage> AddKid(Harness.Session s, int ageYears, string name = "Tobi") =>
        h.Send(HttpMethod.Post, "/v1/me/dependants", s.Headers, new { givenName = name, familyName = "Obi", dateOfBirth = Today(ageYears, -30), gender = "male", relationship = "parent" });

    private async Task<Guid> Kid(Harness.Session s, int age = 8) =>
        (await Harness.Json(await AddKid(s, age))).GetProperty("personId").GetGuid();

    [Fact]
    public async Task Profile_is_readable_and_asks_for_a_date_of_birth()
    {
        var u = await h.NewUser();
        var p = await Harness.Json(await h.Send(HttpMethod.Get, "/v1/me/profile", u.Headers));
        Assert.True(p.GetProperty("needsDateOfBirth").GetBoolean());
        Assert.Equal(u.Email, p.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Date_of_birth_can_be_set_once_and_then_needs_review()
    {
        var u = await h.NewUser();
        object Body(string dob) => new { givenName = "Ada", familyName = "Obi", gender = "female", country = "NG", city = "Lagos", dateOfBirth = dob };
        Assert.Equal(HttpStatusCode.NoContent, (await h.Send(HttpMethod.Put, "/v1/me/profile", u.Headers, Body("1990-05-17"))).StatusCode);
        Assert.False((await Harness.Json(await h.Send(HttpMethod.Get, "/v1/me/profile", u.Headers))).GetProperty("needsDateOfBirth").GetBoolean());
        Assert.Equal(HttpStatusCode.NoContent, (await h.Send(HttpMethod.Put, "/v1/me/profile", u.Headers, Body("1990-05-17"))).StatusCode);   // same value is fine
        var changed = await h.Send(HttpMethod.Put, "/v1/me/profile", u.Headers, Body("1991-05-17"));
        Assert.Equal(HttpStatusCode.Conflict, changed.StatusCode);
        Assert.Equal("dob_locked", (await Harness.Json(changed)).GetProperty("code").GetString());
        await using var c = await app.Db.OpenConnectionAsync();
        Assert.Equal(1, await c.ExecuteScalarAsync<int>("SELECT count(*) FROM audit_event WHERE action = 'person.dob_set' AND actor_account_id = @id", new { id = u.AccountId }));
    }

    [Theory]
    [InlineData("2999-01-01")]
    [InlineData("1800-01-01")]
    [InlineData("17/05/1990")]
    public async Task Invalid_dates_of_birth_are_rejected(string dob)
    {
        var u = await h.NewUser();
        var r = await h.Send(HttpMethod.Put, "/v1/me/profile", u.Headers, new { givenName = "A", familyName = "B", dateOfBirth = dob });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Dependants_are_limited_to_ages_0_to_12_and_have_no_account()
    {
        var u = await h.NewUser();
        Assert.Equal(HttpStatusCode.Created, (await AddKid(u, 12)).StatusCode);
        var tooOld = await AddKid(u, 13);
        Assert.Equal(HttpStatusCode.BadRequest, tooOld.StatusCode);
        Assert.Equal("too_old_for_dependant", (await Harness.Json(tooOld)).GetProperty("code").GetString());
        var list = await Harness.Json(await h.Send(HttpMethod.Get, "/v1/me/dependants", u.Headers));
        Assert.Equal(1, list.GetArrayLength());
        await using var c = await app.Db.OpenConnectionAsync();
        var id = list[0].GetProperty("personId").GetGuid();
        Assert.False(await c.ExecuteScalarAsync<bool>("SELECT EXISTS (SELECT 1 FROM account WHERE person_id = @id)", new { id }));
    }

    [Fact]
    public async Task One_guardian_cannot_see_or_change_another_guardians_child()
    {
        var owner = await h.NewUser();
        var other = await h.NewUser();
        var kid = await Kid(owner);
        Assert.Equal(0, (await Harness.Json(await h.Send(HttpMethod.Get, "/v1/me/dependants", other.Headers))).GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, (await h.Send(HttpMethod.Get, $"/v1/me/dependants/{kid}/collectors", other.Headers)).StatusCode);
        var add = await h.Send(HttpMethod.Post, $"/v1/me/dependants/{kid}/collectors", other.Headers, new { fullName = "X Y", relationship = "uncle", phone = "08031234567" });
        Assert.Equal(HttpStatusCode.NotFound, add.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await h.Send(HttpMethod.Get, $"/v1/me/dependants/{kid}/emergency-contact", other.Headers)).StatusCode);
        var rm = await h.Send(HttpMethod.Post, $"/v1/me/dependants/{kid}/remove", other.Headers, new { reason = "x" });
        Assert.Equal(HttpStatusCode.NotFound, rm.StatusCode);
    }

    [Fact]
    public async Task Collectors_phones_are_normalised_and_stored_encrypted()
    {
        var u = await h.NewUser();
        var kid = await Kid(u);
        var add = await h.Send(HttpMethod.Post, $"/v1/me/dependants/{kid}/collectors", u.Headers, new { fullName = "Chidi Obi", relationship = "uncle", phone = "0803 123 4567", validDays = new[] { "2026-12-21", "2026-12-22" } });
        Assert.Equal(HttpStatusCode.Created, add.StatusCode);
        var list = await Harness.Json(await h.Send(HttpMethod.Get, $"/v1/me/dependants/{kid}/collectors", u.Headers));
        Assert.Equal("+2348031234567", list[0].GetProperty("phone").GetString());
        Assert.Equal(2, list[0].GetProperty("validDays").GetArrayLength());

        await using var c = await app.Db.OpenConnectionAsync();
        var raw = await c.ExecuteScalarAsync<byte[]>("SELECT phone_encrypted FROM authorized_collector WHERE child_person_id = @kid", new { kid });
        Assert.DoesNotContain("8031234567", System.Text.Encoding.UTF8.GetString(raw!));

        Assert.Equal(HttpStatusCode.BadRequest, (await h.Send(HttpMethod.Post, $"/v1/me/dependants/{kid}/collectors", u.Headers, new { fullName = "Bad", relationship = "x", phone = "123" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await h.Send(HttpMethod.Post, $"/v1/me/dependants/{kid}/collectors", u.Headers, new { fullName = "Bad", relationship = "x", phone = "08031234567", validDays = Array.Empty<string>() })).StatusCode);
    }

    [Fact]
    public async Task A_removed_collector_is_no_longer_listed_but_the_row_is_kept()
    {
        var u = await h.NewUser();
        var kid = await Kid(u);
        var id = (await Harness.Json(await h.Send(HttpMethod.Post, $"/v1/me/dependants/{kid}/collectors", u.Headers, new { fullName = "A B", relationship = "aunt", phone = "+2348031234567" }))).GetProperty("collectorId").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await h.Send(HttpMethod.Delete, $"/v1/me/dependants/{kid}/collectors/{id}", u.Headers)).StatusCode);
        Assert.Equal(0, (await Harness.Json(await h.Send(HttpMethod.Get, $"/v1/me/dependants/{kid}/collectors", u.Headers))).GetArrayLength());
        await using var c = await app.Db.OpenConnectionAsync();
        Assert.True(await c.ExecuteScalarAsync<bool>("SELECT removed_at IS NOT NULL FROM authorized_collector WHERE id = @id", new { id }));
        Assert.Equal(HttpStatusCode.NotFound, (await h.Send(HttpMethod.Delete, $"/v1/me/dependants/{kid}/collectors/{id}", u.Headers)).StatusCode);
    }

    [Fact]
    public async Task Emergency_contact_is_one_per_person_and_replaceable()
    {
        var u = await h.NewUser();
        var kid = await Kid(u);
        object Body(string name) => new { fullName = name, relationship = "aunt", phone = "08031234567" };
        await h.Send(HttpMethod.Put, $"/v1/me/dependants/{kid}/emergency-contact", u.Headers, Body("First"));
        await h.Send(HttpMethod.Put, $"/v1/me/dependants/{kid}/emergency-contact", u.Headers, Body("Second"));
        var got = await Harness.Json(await h.Send(HttpMethod.Get, $"/v1/me/dependants/{kid}/emergency-contact", u.Headers));
        Assert.Equal("Second", got.GetProperty("fullName").GetString());
        await h.Send(HttpMethod.Put, "/v1/me/emergency-contact", u.Headers, Body("Mine"));
        Assert.Equal("Mine", (await Harness.Json(await h.Send(HttpMethod.Get, "/v1/me/profile", u.Headers))).GetProperty("emergencyContact").GetProperty("fullName").GetString());
        await using var c = await app.Db.OpenConnectionAsync();
        Assert.Equal(1, await c.ExecuteScalarAsync<int>("SELECT count(*) FROM emergency_contact WHERE person_id = @kid", new { kid }));
    }

    [Fact]
    public async Task Removing_a_dependant_ends_guardianship_with_a_reason()
    {
        var u = await h.NewUser();
        var kid = await Kid(u);
        Assert.Equal(HttpStatusCode.BadRequest, (await h.Send(HttpMethod.Post, $"/v1/me/dependants/{kid}/remove", u.Headers, new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await h.Send(HttpMethod.Post, $"/v1/me/dependants/{kid}/remove", u.Headers, new { reason = "added by mistake" })).StatusCode);
        Assert.Equal(0, (await Harness.Json(await h.Send(HttpMethod.Get, "/v1/me/dependants", u.Headers))).GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, (await h.Send(HttpMethod.Get, $"/v1/me/dependants/{kid}/collectors", u.Headers)).StatusCode);
    }

    [Fact]
    public async Task These_endpoints_need_a_session_and_csrf()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await h.Send(HttpMethod.Get, "/v1/me/profile")).StatusCode);
        var u = await h.NewUser();
        var noCsrf = new Dictionary<string, string> { ["Cookie"] = u.Headers["Cookie"] };
        Assert.Equal(HttpStatusCode.Forbidden, (await h.Send(HttpMethod.Put, "/v1/me/profile", noCsrf, new { givenName = "A", familyName = "B" })).StatusCode);
    }

    [Fact]
    public void Age_is_computed_on_the_reference_date()
    {
        Assert.Equal(8, People.PeopleService.AgeOn(new DateOnly(2017, 12, 22), new DateOnly(2026, 12, 21)));   // birthday the day after
        Assert.Equal(10, People.PeopleService.AgeOn(new DateOnly(2016, 12, 21), new DateOnly(2026, 12, 21)));  // birthday on the day
    }
}
