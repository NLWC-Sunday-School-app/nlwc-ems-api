using System.Net;
using System.Text.RegularExpressions;
using Dapper;
using Staff = nlwc.ems.Tests.Integration.Harness.Session;

namespace nlwc.ems.Tests.Integration;

[Collection("app")]
public sealed class RegistrationTests(TestApp app)
{
    private readonly Harness h = new(app);

    // Fixed birth dates, because the edition's age reference date is fixed: 2017-05-01 is 9 on 21 Dec 2026, 2015-06-01 is 11.
    private const string NineOnReference = "2017-05-01";
    private const string ElevenOnReference = "2015-06-01";

    private async Task<(Guid Id, string Code)> NewEdition(bool open = true, int campMin = 10, int campMax = 16)
    {
        var admin = await h.StaffWith("super_admin");
        var tag = Guid.NewGuid().ToString("N")[..4];
        var response = await h.Send(HttpMethod.Post, "/api/v1/edition", admin.Headers, new
        {
            templateCode = "BB", slug = $"bb-{tag}", code = $"B{tag}", name = "Bible Bootcamp 2026", startsOn = "2026-12-21", endsOn = "2026-12-23",
            venueName = "Faith Plaza", campMinAge = campMin, campMaxAge = campMax
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var id = (await Harness.Data(response)).GetProperty("id").GetGuid();
        if (open)
        {
            await using var c = await app.Db.OpenConnectionAsync();
            await c.ExecuteAsync("UPDATE event_editions SET visibility = 'Public', registration_window = 'Open' WHERE id = @id", new { id });
        }
        return (id, $"B{tag}".ToUpperInvariant());
    }

    private async Task<Guid> Kid(Staff s, string dob = NineOnReference, string name = "Tobi")
    {
        var response = await h.Send(HttpMethod.Post, "/api/v1/dependant", s.Headers,
            new { givenName = name, familyName = "Obi", dateOfBirth = dob, gender = "Male", relationship = "Parent" });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await Harness.Data(response)).GetProperty("id").GetGuid();
    }

    private Task<HttpResponseMessage> Register(Staff s, Guid edition, Guid? person, string mode = "Day") =>
        h.Send(HttpMethod.Post, "/api/v1/registration", s.Headers, new { editionId = edition, personId = person, participationMode = mode });

    private async Task<Guid> Draft(Staff s, Guid edition, Guid person, string mode = "Day")
    {
        var response = await Register(s, edition, person, mode);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await Harness.Data(response)).GetProperty("id").GetGuid();
    }

    private Task<HttpResponseMessage> Consent(Staff s, Guid registration, string kind, bool? accepted) =>
        h.Send(HttpMethod.Put, $"/api/v1/registration/{registration}/consent/{kind}", s.Headers, new { accepted });

    private async Task CompleteRequired(Staff s, Guid registration, Guid child)
    {
        (await h.Send(HttpMethod.Put, $"/api/v1/dependant/{child}/emergency-contact", s.Headers,
            new { fullName = "Ngozi Obi", relationship = "Aunt", phone = "08012345678" })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await h.Send(HttpMethod.Put, $"/api/v1/registration/{registration}/health", s.Headers, new { none = true })).StatusCode.Should().Be(HttpStatusCode.OK);
        foreach (var kind in new[] { "Terms", "GuardianAuthorization", "MedicalEmergency" })
            (await Consent(s, registration, kind, true)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_guardian_starts_a_draft_with_a_reference_age_and_band()
    {
        var (edition, code) = await NewEdition();
        var u = await h.NewUser();
        var first = await Draft(u, edition, await Kid(u));
        var second = await Draft(u, edition, await Kid(u, ElevenOnReference, "Ife"));

        var one = await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/registration/{first}", u.Headers));
        one.GetProperty("state").GetString().Should().Be("Draft");
        one.GetProperty("ageOnReferenceDate").GetInt32().Should().Be(9);
        one.GetProperty("ageBand").GetString().Should().Be("3-9");
        one.GetProperty("reference").GetString().Should().MatchRegex($"^{code}-0001-[A-HJ-NP-Z][0-9]$");
        (await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/registration/{second}", u.Headers)))
            .GetProperty("reference").GetString().Should().StartWith($"{code}-0002-");
        (await Harness.Data(await h.Send(HttpMethod.Get, "/api/v1/registration", u.Headers))).GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task Registration_is_refused_without_a_date_of_birth()
    {
        var (edition, _) = await NewEdition();
        var u = await h.NewUser();   // sign-up collects no date of birth
        var response = await Register(u, edition, null);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Harness.Code(response)).Should().Be("dob_required");
    }

    [Fact]
    public async Task A_person_under_18_cannot_register_themself()
    {
        var (edition, _) = await NewEdition();
        var u = await h.NewUser();
        (await h.Send(HttpMethod.Put, "/api/v1/profile", u.Headers, new { givenName = "Teen", familyName = "Obi", dateOfBirth = "2011-01-01" })).StatusCode.Should().Be(HttpStatusCode.OK);
        var response = await Register(u, edition, null);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Harness.Code(response)).Should().Be("guardian_required");
    }

    [Fact]
    public async Task An_adult_registers_themself_with_only_the_terms_required()
    {
        var (edition, _) = await NewEdition();
        var u = await h.NewUser();
        await h.Send(HttpMethod.Put, "/api/v1/profile", u.Headers, new { givenName = "Ada", familyName = "Obi", dateOfBirth = "1990-05-17" });
        var id = (await Harness.Data(await Register(u, edition, null, "Online"))).GetProperty("id").GetGuid();

        var review = await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/registration/{id}/review", u.Headers));
        review.GetProperty("openTasks").EnumerateArray().Select(t => t.GetProperty("code").GetString()).Should().Equal("consent_terms");
        review.GetProperty("holdsPass").GetBoolean().Should().BeFalse();   // media only applies to children

        (await Consent(u, id, "Media", true)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Consent(u, id, "Terms", true)).StatusCode.Should().Be(HttpStatusCode.OK);
        var submitted = await Harness.Data(await h.Send(HttpMethod.Post, $"/api/v1/registration/{id}/submit", u.Headers, new { }));
        submitted.GetProperty("state").GetString().Should().Be("Confirmed");
    }

    [Fact]
    public async Task Registration_needs_an_open_edition()
    {
        var (edition, _) = await NewEdition(open: false);
        var u = await h.NewUser();
        var response = await Register(u, edition, await Kid(u));
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Harness.Code(response)).Should().Be("registration_closed");
        (await Register(u, Guid.NewGuid(), null)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_person_has_one_active_registration_per_edition_until_it_is_cancelled()
    {
        var (edition, _) = await NewEdition();
        var u = await h.NewUser();
        var child = await Kid(u);
        var id = await Draft(u, edition, child);

        var again = await Register(u, edition, child);
        again.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Harness.Code(again)).Should().Be("already_registered");

        var cancel = await h.Send(HttpMethod.Post, $"/api/v1/registration/{id}/cancel", u.Headers, new { reason = "Family trip" });
        cancel.StatusCode.Should().Be(HttpStatusCode.OK, await cancel.Content.ReadAsStringAsync());
        await Draft(u, edition, child);   // a cancelled registration frees the slot

        await using var c = await app.Db.OpenConnectionAsync();
        (await c.QueryAsync<string?>("SELECT from_state FROM registration_status_histories WHERE registration_id = @id ORDER BY date_created", new { id }))
            .Should().Equal(null, "Draft");
    }

    [Fact]
    public async Task Another_guardian_cannot_see_register_or_change_a_childs_registration()
    {
        var (edition, _) = await NewEdition();
        var owner = await h.NewUser();
        var stranger = await h.NewUser();
        var child = await Kid(owner);
        var id = await Draft(owner, edition, child);

        (await Register(stranger, edition, child)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await h.Send(HttpMethod.Get, $"/api/v1/registration/{id}", stranger.Headers)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await h.Send(HttpMethod.Put, $"/api/v1/registration/{id}/health", stranger.Headers, new { none = true })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await Consent(stranger, id, "Terms", true)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await h.Send(HttpMethod.Post, $"/api/v1/registration/{id}/cancel", stranger.Headers, new { reason = "x" })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await h.Send(HttpMethod.Get, "/api/v1/registration", stranger.Headers)).Should().NotBeNull();
        (await Harness.Data(await h.Send(HttpMethod.Get, "/api/v1/registration", stranger.Headers))).GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Camp_is_a_request_limited_to_the_edition_age_range()
    {
        var (edition, _) = await NewEdition(campMin: 10, campMax: 16);
        var u = await h.NewUser();
        var nine = await Kid(u);
        var eleven = await Kid(u, ElevenOnReference, "Ife");

        var tooYoung = await Register(u, edition, nine, "Camp");
        tooYoung.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Harness.Code(tooYoung)).Should().Be("camp_age_ineligible");
        (await Register(u, edition, nine, "Day")).StatusCode.Should().Be(HttpStatusCode.Created);   // day attendance is not age-gated
        (await Register(u, edition, eleven, "Camp")).StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task A_draft_round_trips_and_only_accepts_an_object()
    {
        var (edition, _) = await NewEdition();
        var u = await h.NewUser();
        var id = await Draft(u, edition, await Kid(u));

        (await h.Send(HttpMethod.Put, $"/api/v1/registration/{id}/draft", u.Headers, new { draft = new { step = 3, studio = true } })).StatusCode.Should().Be(HttpStatusCode.OK);
        var draft = (await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/registration/{id}", u.Headers))).GetProperty("draft");
        draft.GetProperty("step").GetInt32().Should().Be(3);
        draft.GetProperty("studio").GetBoolean().Should().BeTrue();

        (await h.Send(HttpMethod.Put, $"/api/v1/registration/{id}/draft", u.Headers, new { draft = new[] { 1, 2 } })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Health_none_is_exclusive_and_the_answers_are_encrypted_at_rest()
    {
        var (edition, _) = await NewEdition();
        var u = await h.NewUser();
        var id = await Draft(u, edition, await Kid(u));
        string Url() => $"/api/v1/registration/{id}/health";

        var clash = await h.Send(HttpMethod.Put, Url(), u.Headers, new { none = true, conditions = new[] { "asthma" } });
        clash.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Harness.Code(clash)).Should().Be("health_none_exclusive");
        (await Harness.Code(await h.Send(HttpMethod.Put, Url(), u.Headers, new { none = false }))).Should().Be("health_unanswered");
        (await Harness.Code(await h.Send(HttpMethod.Put, Url(), u.Headers, new { conditions = new[] { "flu" } }))).Should().Be("health_unknown_condition");
        (await Harness.Code(await h.Send(HttpMethod.Put, Url(), u.Headers, new { conditions = new[] { "other" } }))).Should().Be("health_other_needs_notes");
        (await Harness.Data(await h.Send(HttpMethod.Get, Url(), u.Headers))).ValueKind.Should().Be(System.Text.Json.JsonValueKind.Null);

        (await h.Send(HttpMethod.Put, Url(), u.Headers, new { conditions = new[] { "asthma", "Asthma", "fever" }, allergies = "peanuts" })).StatusCode.Should().Be(HttpStatusCode.OK);
        var saved = await Harness.Data(await h.Send(HttpMethod.Get, Url(), u.Headers));
        saved.GetProperty("conditions").EnumerateArray().Select(x => x.GetString()).Should().Equal("asthma", "fever");
        saved.GetProperty("allergies").GetString().Should().Be("peanuts");

        await using var c = await app.Db.OpenConnectionAsync();
        var (text, flags) = await c.QuerySingleAsync<(string, bool)>(
            "SELECT encode(data_encrypted, 'escape'), has_flags FROM health_disclosures WHERE registration_id = @id", new { id });
        text.Should().NotContain("asthma").And.NotContain("peanuts");
        flags.Should().BeTrue();
        (await c.ExecuteScalarAsync<string>("SELECT detail::text FROM audit_events WHERE action = 'health.updated' AND record_id = @id ORDER BY seq DESC LIMIT 1", new { id = id.ToString() }))
            .Should().NotContain("asthma");   // the event says health changed, never what it says
    }

    [Fact]
    public async Task Consent_answers_are_append_only_and_unanswered_is_not_a_no()
    {
        var (edition, code) = await NewEdition();
        var u = await h.NewUser();
        var id = await Draft(u, edition, await Kid(u));

        (await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/registration/{id}/consent", u.Headers))).GetArrayLength().Should().Be(0);
        (await Consent(u, id, "Media", null)).StatusCode.Should().Be(HttpStatusCode.BadRequest);   // an answer must be explicit
        (await Consent(u, id, "Media", false)).StatusCode.Should().Be(HttpStatusCode.OK);
        var changed = await Harness.Data(await Consent(u, id, "Media", true));
        changed.GetProperty("documentId").GetString().Should().Be($"{code}-media");
        changed.GetProperty("documentVersion").GetInt32().Should().Be(0);   // no approved wording exists yet

        var current = await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/registration/{id}/consent", u.Headers));
        current.GetArrayLength().Should().Be(1);
        current[0].GetProperty("accepted").GetBoolean().Should().BeTrue();

        await using var c = await app.Db.OpenConnectionAsync();
        (await c.ExecuteScalarAsync<int>("SELECT count(*) FROM consent_records WHERE registration_id = @id", new { id })).Should().Be(2);
        var update = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => c.ExecuteAsync("UPDATE consent_records SET accepted = false"));
        update.MessageText.Should().Contain("append-only");
    }

    [Fact]
    public async Task Submit_lists_what_is_missing_and_a_media_no_never_blocks()
    {
        var (edition, _) = await NewEdition();
        var u = await h.NewUser();
        var child = await Kid(u);
        var id = await Draft(u, edition, child);

        var early = await h.Send(HttpMethod.Post, $"/api/v1/registration/{id}/submit", u.Headers, new { });
        early.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Harness.Code(early)).Should().Be("registration_incomplete");
        (await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/registration/{id}/review", u.Headers))).GetProperty("openTasks").EnumerateArray()
            .Where(t => t.GetProperty("blocking").GetBoolean()).Select(t => t.GetProperty("code").GetString()).Should()
            .BeEquivalentTo("emergency_contact", "health", "consent_terms", "consent_guardian_authorization", "consent_medical_emergency");

        await CompleteRequired(u, id, child);
        var review = await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/registration/{id}/review", u.Headers));
        review.GetProperty("canSubmit").GetBoolean().Should().BeTrue();
        review.GetProperty("holdsPass").GetBoolean().Should().BeTrue();   // media unanswered: the pass stays Pending (Q20)
        review.GetProperty("openTasks").EnumerateArray().Should().ContainSingle(t => t.GetProperty("code").GetString() == "consent_media" && !t.GetProperty("blocking").GetBoolean());

        (await Consent(u, id, "Media", false)).StatusCode.Should().Be(HttpStatusCode.OK);   // an explicit No
        (await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/registration/{id}/review", u.Headers))).GetProperty("holdsPass").GetBoolean().Should().BeFalse();

        var submitted = await Harness.Data(await h.Send(HttpMethod.Post, $"/api/v1/registration/{id}/submit", u.Headers, new { }));
        submitted.GetProperty("state").GetString().Should().Be("Confirmed");   // no payment step on this edition

        var locked = await h.Send(HttpMethod.Put, $"/api/v1/registration/{id}/health", u.Headers, new { none = true });
        locked.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await Harness.Code(locked)).Should().Be("registration_locked");

        await using var c = await app.Db.OpenConnectionAsync();
        (await c.QueryAsync<string>("SELECT to_state FROM registration_status_histories WHERE registration_id = @id ORDER BY date_created", new { id }))
            .Should().Equal("Draft", "Confirmed");
    }

    [Fact]
    public async Task Declining_the_terms_blocks_submission()
    {
        var (edition, _) = await NewEdition();
        var u = await h.NewUser();
        var child = await Kid(u);
        var id = await Draft(u, edition, child);
        await CompleteRequired(u, id, child);
        (await Consent(u, id, "Terms", false)).StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await h.Send(HttpMethod.Post, $"/api/v1/registration/{id}/submit", u.Headers, new { });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Harness.Code(response)).Should().Be("registration_incomplete");
        (await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/registration/{id}/review", u.Headers))).GetProperty("openTasks").EnumerateArray()
            .Select(t => t.GetProperty("code").GetString()).Should().Equal("consent_terms", "consent_media");
    }

    [Fact]
    public async Task With_a_payment_step_a_submitted_registration_waits_for_payment()
    {
        var (edition, _) = await NewEdition();
        await using (var c = await app.Db.OpenConnectionAsync())
            await c.ExecuteAsync("UPDATE edition_capabilities SET enabled = true WHERE edition_id = @edition AND capability = 'PaidRegistration'", new { edition });
        var u = await h.NewUser();
        var child = await Kid(u);
        var id = await Draft(u, edition, child);
        await CompleteRequired(u, id, child);

        var submitted = await Harness.Data(await h.Send(HttpMethod.Post, $"/api/v1/registration/{id}/submit", u.Headers, new { }));
        submitted.GetProperty("state").GetString().Should().Be("Submitted");
    }

    private async Task<(Guid Edition, Guid Registration, Guid Child)> FlaggedRegistration()
    {
        var (edition, _) = await NewEdition();
        var u = await h.NewUser();
        var child = await Kid(u);
        var id = await Draft(u, edition, child);
        (await h.Send(HttpMethod.Put, $"/api/v1/registration/{id}/health", u.Headers, new { conditions = new[] { "asthma" }, notes = "Inhaler in bag" })).StatusCode.Should().Be(HttpStatusCode.OK);
        return (edition, id, child);
    }

    [Fact]
    public async Task Staff_see_the_queue_for_their_edition_and_flags_only_with_health_access()
    {
        var (edition, registration, _) = await FlaggedRegistration();
        var medical = await h.StaffWith("medical_welfare");
        var finance = await h.StaffWith("finance_officer");

        var asMedical = await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/admin/registration?editionId={edition}", medical.Headers));
        asMedical.GetProperty("count").GetInt32().Should().Be(1);
        var row = asMedical.GetProperty("data")[0];
        row.GetProperty("id").GetGuid().Should().Be(registration);
        row.GetProperty("givenName").GetString().Should().Be("Tobi");
        row.GetProperty("hasHealthFlags").GetBoolean().Should().BeTrue();
        row.ToString().Should().NotContain("asthma").And.NotContain("Inhaler");   // the queue never carries health content

        var asFinance = await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/admin/registration?editionId={edition}&state=Draft", finance.Headers));
        asFinance.GetProperty("data")[0].GetProperty("hasHealthFlags").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Null);
        (await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/admin/registration?editionId={edition}&state=Cancelled", finance.Headers)))
            .GetProperty("count").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task The_queue_is_forbidden_without_the_permission_for_that_edition()
    {
        var (edition, _, _) = await FlaggedRegistration();
        var family = await h.NewUser();
        (await h.Send(HttpMethod.Get, $"/api/v1/admin/registration?editionId={edition}", family.Headers)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var (other, _) = await NewEdition();
        var scoped = await h.NewUser("admin", "s");
        await h.Grant(scoped.AccountId, "registration_officer", other);   // a grant for another edition
        await h.VerifyMfa(scoped);
        (await h.Send(HttpMethod.Get, $"/api/v1/admin/registration?editionId={other}", scoped.Headers)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await h.Send(HttpMethod.Get, $"/api/v1/admin/registration?editionId={edition}", scoped.Headers)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Revealing_health_needs_health_access_and_a_reason_and_is_logged()
    {
        var (edition, registration, child) = await FlaggedRegistration();
        var medical = await h.StaffWith("medical_welfare");
        var officer = await h.StaffWith("registration_officer");   // flags only
        var url = $"/api/v1/admin/registration/{registration}/health";

        (await h.Send(HttpMethod.Get, $"{url}?reason=Check", officer.Headers)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var noReason = await h.Send(HttpMethod.Get, url, medical.Headers);
        noReason.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Harness.Code(noReason)).Should().Be("reason_required");
        await using var c = await app.Db.OpenConnectionAsync();
        (await c.ExecuteScalarAsync<int>("SELECT count(*) FROM access_logs WHERE subject_person_id = @child", new { child })).Should().Be(0);   // refusals leave no reveal

        var health = await Harness.Data(await h.Send(HttpMethod.Get, $"{url}?reason=Camp%20fitness%20review", medical.Headers));
        health.GetProperty("conditions")[0].GetString().Should().Be("asthma");
        health.GetProperty("notes").GetString().Should().Be("Inhaler in bag");

        var (actor, resource, reason, edition2) = await c.QuerySingleAsync<(Guid, string, string, Guid)>(
            "SELECT actor_account_id, resource, reason, edition_id FROM access_logs WHERE subject_person_id = @child", new { child });
        (actor, resource, reason, edition2).Should().Be((medical.AccountId, "health", "Camp fitness review", edition));
        (await h.Send(HttpMethod.Get, $"/api/v1/admin/registration/{Guid.NewGuid()}/health?reason=x", medical.Headers)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
