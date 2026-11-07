using System.Net;
using Dapper;
using Staff = nlwc.ems.Tests.Integration.Harness.Session;

namespace nlwc.ems.Tests.Integration;

[Collection("app")]
public sealed class RegistrationEmailTests(TestApp app)
{
    private readonly Harness h = new(app);

    private async Task<Guid> BootcampEdition(string templateCode = "BB")
    {
        var admin = await h.StaffWith("super_admin");
        var tag = Guid.NewGuid().ToString("N")[..8];
        var created = await h.Send(HttpMethod.Post, "/api/v1/edition", admin.Headers, new
        {
            templateCode, slug = $"em-{tag}", code = $"E{tag[..4]}", name = "Bible Bootcamp 2026", startsOn = "2026-12-21", endsOn = "2026-12-23",
            venueName = "Faith Plaza", venueAddress = "Bariga, Lagos", campMinAge = 10, campMaxAge = 16, campCapacity = 10, pickupWindowStart = "16:00:00", pickupWindowEnd = "18:00:00"
        });
        created.StatusCode.Should().Be(HttpStatusCode.OK, await created.Content.ReadAsStringAsync());
        var id = (await Harness.Data(created)).GetProperty("id").GetGuid();
        await using var c = await app.Db.OpenConnectionAsync();
        await c.ExecuteAsync("UPDATE event_editions SET registration_window = 'Open' WHERE id = @id", new { id });
        return id;
    }

    private async Task<Staff> Guardian(string given = "Chioma")
    {
        var u = await h.NewUser();
        (await h.Send(HttpMethod.Put, "/api/v1/profile", u.Headers, new { givenName = given, familyName = "Egwu", dateOfBirth = "1985-05-17" })).StatusCode.Should().Be(HttpStatusCode.OK);
        return u;
    }

    /// <summary>Registers one child, completes every required step and submits. Returns the registration id.</summary>
    private async Task<Guid> SubmitChild(Staff parent, Guid edition, string name, string mode = "Day", string dob = "2017-05-01")
    {
        var kid = (await Harness.Data(await h.Send(HttpMethod.Post, "/api/v1/dependant", parent.Headers,
            new { givenName = name, familyName = "Egwu", dateOfBirth = dob, gender = "Male", relationship = "Parent" }))).GetProperty("id").GetGuid();
        var reg = (await Harness.Data(await h.Send(HttpMethod.Post, "/api/v1/registration", parent.Headers, new { editionId = edition, personId = kid, participationMode = mode }))).GetProperty("id").GetGuid();
        (await h.Send(HttpMethod.Post, "/api/v1/emergency-contacts", parent.Headers, new { fullName = "Ngozi Obi", relationship = "Aunt", phone = "08012345678" })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await h.Send(HttpMethod.Put, $"/api/v1/registration/{reg}/health", parent.Headers, new { none = true })).StatusCode.Should().Be(HttpStatusCode.OK);
        foreach (var kind in new[] { "Terms", "GuardianAuthorization", "MedicalEmergency" })
            (await h.Send(HttpMethod.Put, $"/api/v1/registration/{reg}/consent/{kind}", parent.Headers, new { accepted = true })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await h.Send(HttpMethod.Post, $"/api/v1/registration/{reg}/submit", parent.Headers, new { })).StatusCode.Should().Be(HttpStatusCode.OK);
        return reg;
    }

    private List<(string To, string Subject, string Html)> SentTo(string address)
    {
        lock (app.Email.Sent) return app.Email.Sent.Where(e => e.To == address).ToList();
    }

    [Fact]
    public async Task The_parent_is_emailed_when_a_child_is_registered()
    {
        var edition = await BootcampEdition();
        var parent = await Guardian();
        await SubmitChild(parent, edition, "Tobi");

        var mail = SentTo(parent.Email).Should().ContainSingle().Subject;
        mail.Subject.Should().Be("Registration confirmed: Bible Bootcamp 2026");
        mail.Html.Should().Contain("Congratulations, Chioma!").And.Contain("Tobi Egwu").And.Contain("Faith Plaza, Bariga, Lagos").And.Contain("4:00pm – 6:00pm daily").And.Contain("Walk-in");
        mail.Html.Should().NotContain("What to bring to camp").And.NotContain("{{");
    }

    [Fact]
    public async Task Staff_can_resend_the_confirmation_with_a_reason_and_only_with_edit_access()
    {
        var edition = await BootcampEdition();
        var parent = await Guardian();
        var reg = await SubmitChild(parent, edition, "Tobi");
        var before = SentTo(parent.Email).Count;
        var url = $"/api/v1/admin/registration/{reg}/resend-confirmation";

        var viewer = await h.StaffWith("finance_officer");   // can view, cannot edit
        var admin = await h.StaffWith("super_admin");
        (await h.Send(HttpMethod.Post, url, viewer.Headers, new { reason = "Parent says it never arrived" })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await h.Send(HttpMethod.Post, url, admin.Headers, new { reason = "" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        SentTo(parent.Email).Count.Should().Be(before);

        var ok = await h.Send(HttpMethod.Post, url, admin.Headers, new { reason = "Parent says it never arrived" });
        ok.StatusCode.Should().Be(HttpStatusCode.OK, await ok.Content.ReadAsStringAsync());
        SentTo(parent.Email).Count.Should().Be(before + 1);

        await using var c = await app.Db.OpenConnectionAsync();
        (await c.ExecuteScalarAsync<int>("SELECT count(*) FROM audit_events WHERE action = 'registration.confirmation_resent' AND record_id = @id", new { id = reg.ToString() })).Should().Be(1);
    }

    [Fact]
    public async Task A_second_child_gets_one_more_email_that_lists_both()
    {
        var edition = await BootcampEdition();
        var parent = await Guardian();
        await SubmitChild(parent, edition, "Tobi");
        await SubmitChild(parent, edition, "Dara", "Camp", "2014-06-01");

        var mails = SentTo(parent.Email);
        mails.Should().HaveCount(2);
        mails[1].Html.Should().Contain("Tobi and Dara").And.Contain("Tobi Egwu").And.Contain("Dara Egwu").And.Contain("What to bring to camp").And.Contain("Pending our team&#39;s review");
    }

    [Fact]
    public async Task An_adult_registering_themself_gets_no_guardian_email()
    {
        var edition = await BootcampEdition();
        var adult = await Guardian("Emeka");
        var reg = (await Harness.Data(await h.Send(HttpMethod.Post, "/api/v1/registration", adult.Headers, new { editionId = edition, participationMode = "Online" }))).GetProperty("id").GetGuid();
        (await h.Send(HttpMethod.Post, "/api/v1/emergency-contacts", adult.Headers, new { fullName = "Ngozi Obi", relationship = "Aunt", phone = "08012345678" })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await h.Send(HttpMethod.Put, $"/api/v1/registration/{reg}/consent/Terms", adult.Headers, new { accepted = true })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await h.Send(HttpMethod.Post, $"/api/v1/registration/{reg}/submit", adult.Headers, new { })).StatusCode.Should().Be(HttpStatusCode.OK);
        SentTo(adult.Email).Should().BeEmpty();
    }

    [Fact]
    public async Task A_programme_without_a_template_sends_nothing_but_the_registration_still_succeeds()
    {
        var edition = await BootcampEdition("SS");   // Singlz: no confirmation email has been designed yet
        var parent = await Guardian();
        await SubmitChild(parent, edition, "Tobi");   // asserts the submit returned 200
        SentTo(parent.Email).Should().BeEmpty();
    }
}
