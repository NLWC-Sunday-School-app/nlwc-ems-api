using System.Net;
using Dapper;
using nlwc.ems.Core.Security;
using nlwc.ems.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using nlwc.ems.Base.Core.Audit;

namespace nlwc.ems.Tests.Integration;

[Collection("app")]
public sealed class AccessTests(TestApp app)
{
    private readonly Harness h = new(app);

    private async Task<Guid> NewEdition()
    {
        var admin = await h.StaffWith("super_admin");
        var response = await h.Send(HttpMethod.Post, "/api/v1/edition", admin.Headers, new
        {
            templateCode = "BB", slug = $"e-{Guid.NewGuid():N}"[..14], code = "E" + Guid.NewGuid().ToString("N")[..8], name = "E", startsOn = "2026-12-21", endsOn = "2026-12-23"
        });
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await Harness.Data(response)).GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Without_a_grant_access_is_denied_and_audited()
    {
        var user = await h.NewUser("admin", "nogrant");
        await h.VerifyMfa(user);

        var response = await h.Send(HttpMethod.Get, "/api/v1/audit", user.Headers);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Harness.Code(response)).Should().Be("forbidden");

        await using var c = await app.Db.OpenConnectionAsync();
        (await c.ExecuteScalarAsync<int>("SELECT count(*) FROM audit_events WHERE action = 'access.denied' AND actor_account_id = @id", new { id = user.AccountId })).Should().Be(1);
    }

    [Fact]
    public async Task A_denial_from_a_controllers_own_check_is_audited_too()
    {
        var usher = await h.StaffWith("volunteer_usher");
        (await h.Send(HttpMethod.Get, "/api/v1/edition", usher.Headers)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await using var c = await app.Db.OpenConnectionAsync();
        (await c.ExecuteScalarAsync<int>("SELECT count(*) FROM audit_events WHERE action = 'access.denied' AND actor_account_id = @id", new { id = usher.AccountId })).Should().Be(1);
    }

    [Fact]
    public async Task Staff_need_mfa_even_with_the_right_role()
    {
        var user = await h.NewUser("admin", "nomfa");
        await h.Grant(user.AccountId, "super_admin");

        var blocked = await h.Send(HttpMethod.Get, "/api/v1/audit", user.Headers);
        blocked.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await Harness.Code(blocked)).Should().Be("mfa_required");

        await h.VerifyMfa(user);
        (await h.Send(HttpMethod.Get, "/api/v1/audit", user.Headers)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_low_role_cannot_read_audit_or_grant_roles()
    {
        var usher = await h.StaffWith("volunteer_usher");
        (await h.Send(HttpMethod.Get, "/api/v1/audit", usher.Headers)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var grant = await h.Send(HttpMethod.Post, "/api/v1/rolegrant", usher.Headers, new { accountId = usher.AccountId, role = "super_admin", reason = "self" });
        grant.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Granting_needs_a_reason_and_is_audited_and_edition_scoped()
    {
        var admin = await h.StaffWith("super_admin");
        var target = await h.NewUser();
        var edition = await NewEdition();

        var noReason = await h.Send(HttpMethod.Post, "/api/v1/rolegrant", admin.Headers, new { accountId = target.AccountId, role = "event_admin", editionId = edition });
        noReason.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var ok = await h.Send(HttpMethod.Post, "/api/v1/rolegrant", admin.Headers,
            new { accountId = target.AccountId, role = "event_admin", editionId = edition, reason = "Bootcamp lead" });
        ok.StatusCode.Should().Be(HttpStatusCode.OK, await ok.Content.ReadAsStringAsync());

        var inEdition = await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/access/me?editionId={edition}", target.Headers));
        inEdition.GetProperty("permissions").EnumerateArray()
            .Should().Contain(p => p.GetProperty("code").GetString() == "config:edit" && p.GetProperty("editionId").GetGuid() == edition);

        (await Harness.Data(await h.Send(HttpMethod.Get, $"/api/v1/access/me?editionId={Guid.NewGuid()}", target.Headers))).GetProperty("permissions").GetArrayLength().Should().Be(0);
        (await Harness.Data(await h.Send(HttpMethod.Get, "/api/v1/access/me", target.Headers))).GetProperty("permissions").GetArrayLength().Should().Be(0);

        // A generic Create is audited by the DAO, with the reason as the record's display name.
        await using var c = await app.Db.OpenConnectionAsync();
        (await c.ExecuteScalarAsync<int>("SELECT count(*) FROM audit_events WHERE action = 'Create' AND entity_type = 'RoleGrant' AND display_name = 'Bootcamp lead'")).Should().Be(1);
    }

    [Fact]
    public async Task An_edition_scoped_grant_works_in_that_edition_and_not_another()
    {
        var edition = await NewEdition();
        var other = await NewEdition();
        var lead = await h.NewUser("admin", "lead");
        await h.Grant(lead.AccountId, "event_admin", edition);
        await h.VerifyMfa(lead);

        (await h.Send(HttpMethod.Get, $"/api/v1/edition/{edition}", lead.Headers)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await h.Send(HttpMethod.Get, $"/api/v1/edition/{other}", lead.Headers)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await h.Send(HttpMethod.Put, $"/api/v1/edition/{edition}/capability/DailyCheckin", lead.Headers, new { enabled = true })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await h.Send(HttpMethod.Put, $"/api/v1/edition/{other}/capability/DailyCheckin", lead.Headers, new { enabled = true })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Expired_and_revoked_grants_stop_working()
    {
        var user = await h.NewUser("admin", "exp");
        await h.Grant(user.AccountId, "auditor");
        await h.VerifyMfa(user);
        (await h.Send(HttpMethod.Get, "/api/v1/audit", user.Headers)).StatusCode.Should().Be(HttpStatusCode.OK);

        await using var c = await app.Db.OpenConnectionAsync();
        await c.ExecuteAsync("UPDATE role_grants SET expires_at = now() - interval '1 minute' WHERE account_id = @id", new { id = user.AccountId });
        (await h.Send(HttpMethod.Get, "/api/v1/audit", user.Headers)).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        await c.ExecuteAsync("UPDATE role_grants SET expires_at = NULL, revoked_at = now() WHERE account_id = @id", new { id = user.AccountId });
        (await h.Send(HttpMethod.Get, "/api/v1/audit", user.Headers)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Revoking_a_grant_needs_a_reason_and_takes_effect_at_once()
    {
        var admin = await h.StaffWith("super_admin");
        var target = await h.NewUser("admin", "rev");
        await h.VerifyMfa(target);
        var grant = await Harness.Data(await h.Send(HttpMethod.Post, "/api/v1/rolegrant", admin.Headers, new { accountId = target.AccountId, role = "auditor", reason = "audit week" }));
        var id = grant.GetProperty("id").GetGuid();
        (await h.Send(HttpMethod.Get, "/api/v1/audit", target.Headers)).StatusCode.Should().Be(HttpStatusCode.OK);

        (await h.Send(HttpMethod.Post, $"/api/v1/rolegrant/{id}/revoke", admin.Headers, new { })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await h.Send(HttpMethod.Post, $"/api/v1/rolegrant/{id}/revoke", admin.Headers, new { reason = "audit done" })).StatusCode.Should().Be(HttpStatusCode.OK);
        (await h.Send(HttpMethod.Get, "/api/v1/audit", target.Headers)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await h.Send(HttpMethod.Post, $"/api/v1/rolegrant/{id}/revoke", admin.Headers, new { reason = "again" })).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_grant_cannot_be_deleted_only_revoked()
    {
        var admin = await h.StaffWith("super_admin");
        var response = await h.Send(HttpMethod.Delete, $"/api/v1/rolegrant/{Guid.NewGuid()}", admin.Headers);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public void Higher_levels_include_lower_ones()
    {
        EmsPermissions.Satisfies("health", "full", "flags").Should().BeTrue();
        EmsPermissions.Satisfies("custody", "view", "act").Should().BeTrue();
        EmsPermissions.Satisfies("custody", "scan", "act").Should().BeFalse();
        EmsPermissions.Satisfies("pricing", "draft", "approve").Should().BeFalse();
        EmsPermissions.Satisfies("nope", "x", "x").Should().BeFalse();
        EmsPermissions.Expand("health", "full").Should().Equal("health:flags", "health:view", "health:full");
    }

    [Fact]
    public async Task Medical_and_finance_are_never_bundled_into_other_roles()
    {
        await using var c = await app.Db.OpenConnectionAsync();
        (await c.QueryAsync<string>("SELECT r.key FROM roles r JOIN role_permissions p ON p.role_id = r.id WHERE p.resource = 'health' AND p.level IN ('view', 'full')")).Should().Equal("medical_welfare");
        (await c.QueryAsync<string>("SELECT r.key FROM roles r JOIN role_permissions p ON p.role_id = r.id WHERE p.resource = 'money' AND p.level = 'edit'")).Should().Equal("finance_officer");

        var all = await c.QueryAsync<(string R, string L)>("SELECT resource, level FROM role_permissions");
        all.Should().OnlyContain(p => EmsPermissions.Known(p.R, p.L));
    }

    [Fact]
    public async Task The_audit_chain_verifies_and_detects_tampering()
    {
        using var scope = app.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IAuditEventPublisher>();
        var audit = scope.ServiceProvider.GetRequiredService<IAuditService>();
        await publisher.PublishAsync(new EntityAuditEvent { EntityType = "Test", Action = "test.one" });
        await publisher.PublishAsync(new EntityAuditEvent { EntityType = "Test", Action = "test.two", Reason = "because", Detail = new { a = 1 } });
        (await audit.VerifyChainAsync()).Should().BeNull();

        await using var c = await app.Db.OpenConnectionAsync();
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => c.ExecuteAsync("UPDATE audit_events SET action = 'x' WHERE action = 'test.one'"));
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => c.ExecuteAsync("DELETE FROM audit_events WHERE action = 'test.one'"));

        // Someone with enough privilege to switch the trigger off and edit a row is caught by the hash chain.
        await c.ExecuteAsync("ALTER TABLE audit_events DISABLE TRIGGER audit_events_append_only");
        await c.ExecuteAsync("UPDATE audit_events SET action = 'tampered' WHERE action = 'test.one'");
        await c.ExecuteAsync("ALTER TABLE audit_events ENABLE TRIGGER audit_events_append_only");
        (await audit.VerifyChainAsync()).Should().NotBeNull();

        await c.ExecuteAsync("ALTER TABLE audit_events DISABLE TRIGGER audit_events_append_only");
        await c.ExecuteAsync("UPDATE audit_events SET action = 'test.one' WHERE action = 'tampered'");
        await c.ExecuteAsync("ALTER TABLE audit_events ENABLE TRIGGER audit_events_append_only");
        (await audit.VerifyChainAsync()).Should().BeNull();
    }
}
