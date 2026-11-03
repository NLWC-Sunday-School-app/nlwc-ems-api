using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using nlwc.ems.Data.EFCore;

namespace nlwc.ems.Tests.Integration;

[Collection("app")]
public sealed class MigrationTests(TestApp app)
{
    private async Task<(MySqlConnection Conn, Guid Person, Guid Edition)> SeedAsync(string code)
    {
        Guid person = Guid.NewGuid(), edition = Guid.NewGuid();
        var conn = await app.Db.OpenConnectionAsync();
        var template = await conn.ExecuteScalarAsync<Guid>("SELECT id FROM event_templates WHERE code = 'BB'");
        await conn.ExecuteAsync("INSERT INTO people(id, given_name, family_name, status, date_created, date_modified) VALUES (@person, 'A', 'B', 'Active', UTC_TIMESTAMP(6), UTC_TIMESTAMP(6))", new { person });
        await conn.ExecuteAsync(
            """
            INSERT INTO event_editions(id, template_id, slug, code, name, timezone, starts_on, ends_on, age_reference_date,
                                       visibility, registration_window, live_state, status, date_created, date_modified)
            VALUES (@edition, @template, @code, @code, 'BB', 'Africa/Lagos', '2026-12-21', '2026-12-23', '2026-12-21',
                    'Draft', 'NotOpen', 'NotLive', 'Active', UTC_TIMESTAMP(6), UTC_TIMESTAMP(6))
            """, new { edition, template, code });
        return (conn, person, edition);
    }

    private static Task<int> Register(MySqlConnection c, Guid edition, Guid person, string reference, Guid? id = null) =>
        c.ExecuteAsync(
            """
            INSERT INTO registrations(id, reference, edition_id, person_id, state, participation_mode, draft, status, date_created, date_modified)
            VALUES (@id, @reference, @edition, @person, 'Draft', 'Day', '{}', 'Active', UTC_TIMESTAMP(6), UTC_TIMESTAMP(6))
            """, new { id = id ?? Guid.NewGuid(), reference, edition, person });

    [Fact]
    public async Task The_app_applies_every_migration_at_startup_and_none_are_left_pending()
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EmsDbContext>();
        (await db.Database.GetPendingMigrationsAsync()).Should().BeEmpty();
        (await db.Database.GetAppliedMigrationsAsync()).Should().NotBeEmpty();
    }

    [Fact]
    public async Task Reference_data_is_seeded()
    {
        await using var c = await app.Db.OpenConnectionAsync();
        (await c.QueryAsync<string>("SELECT code FROM event_templates ORDER BY code")).Should().Equal("BB", "CP", "SS");
        (await c.ExecuteScalarAsync<int>("SELECT count(*) FROM roles")).Should().Be(12);
        (await c.ExecuteScalarAsync<int>("SELECT count(*) FROM role_permissions")).Should().BeGreaterThan(20);
    }

    [Fact]
    public async Task One_active_registration_per_person_per_edition()
    {
        var (c, person, edition) = await SeedAsync("T1");
        await using var _ = c;
        await Register(c, edition, person, "T1-R1");
        var ex = await Assert.ThrowsAsync<MySqlException>(() => Register(c, edition, person, "T1-R2"));
        ex.ErrorCode.Should().Be(MySqlErrorCode.DuplicateKeyEntry);
        ex.Message.Should().Contain("ix_registrations_one_active");
    }

    [Fact]
    public async Task Status_history_is_append_only()
    {
        var (c, person, edition) = await SeedAsync("T2");
        await using var _ = c;
        var registration = Guid.NewGuid();
        await Register(c, edition, person, "T2-R1", registration);
        await c.ExecuteAsync(
            "INSERT INTO registration_status_histories(id, registration_id, to_state, status, date_created, date_modified) VALUES (UUID(), @registration, 'Submitted', 'Active', UTC_TIMESTAMP(6), UTC_TIMESTAMP(6))",
            new { registration });

        var update = await Assert.ThrowsAsync<MySqlException>(() => c.ExecuteAsync("UPDATE registration_status_histories SET reason = 'x'"));
        update.Message.Should().Contain("append-only");
        await Assert.ThrowsAsync<MySqlException>(() => c.ExecuteAsync("DELETE FROM registration_status_histories"));
    }

    [Fact]
    public async Task Money_rules_are_enforced_by_the_database()
    {
        await using var c = await app.Db.OpenConnectionAsync();
        var checks = (await c.QueryAsync<string>("SELECT constraint_name FROM information_schema.check_constraints WHERE constraint_schema = DATABASE() AND constraint_name LIKE 'ck_rate_%'")).ToList();
        checks.Should().Contain(["ck_rate_rules_kind", "ck_rate_rules_ages", "ck_rate_plan_versions_approved"]);
    }
}
