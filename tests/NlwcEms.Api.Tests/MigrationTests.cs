using Dapper;
using Npgsql;
using NlwcEms.Api.Db;

namespace NlwcEms.Api.Tests;

[Collection("app")]
public sealed class MigrationTests(TestApp app)
{
    private async Task<(NpgsqlConnection Conn, Guid Person, Guid Edition)> SeedAsync(string code)
    {
        Guid person = Guid.NewGuid(), org = Guid.NewGuid(), tpl = Guid.NewGuid(), ed = Guid.NewGuid();
        var c = await app.Db.OpenConnectionAsync();
        await c.ExecuteAsync("INSERT INTO person(id,given_name,family_name) VALUES (@person,'A','B')", new { person });
        await c.ExecuteAsync("INSERT INTO organization VALUES (@org,'NLWC')", new { org });
        await c.ExecuteAsync("INSERT INTO event_template VALUES (@tpl,@org,@code,'Bible Bootcamp','{}')", new { tpl, org, code });
        await c.ExecuteAsync("INSERT INTO event_edition(id,template_id,slug,code,name,starts_on,ends_on,age_reference_date) VALUES (@ed,@tpl,@code,@code,'BB','2026-12-21','2026-12-23','2026-12-21')", new { ed, tpl, code });
        return (c, person, ed);
    }

    private static Task<int> Register(NpgsqlConnection c, Guid ed, Guid person, string reference, Guid? id = null) =>
        c.ExecuteAsync("INSERT INTO registration(id,reference,edition_id,person_id,participation_mode) VALUES (@id,@reference,@ed,@person,'day')",
            new { id = id ?? Guid.NewGuid(), reference, ed, person });

    [Fact]
    public async Task Migrations_are_idempotent() => Assert.Empty(await Migrator.RunAsync(app.Db));

    [Fact]
    public async Task One_active_registration_per_person_per_edition()
    {
        var (c, person, ed) = await SeedAsync("T1");
        await using var _ = c;
        await Register(c, ed, person, "T1-R1");
        var ex = await Assert.ThrowsAsync<PostgresException>(() => Register(c, ed, person, "T1-R2"));
        Assert.Equal("registration_one_active", ex.ConstraintName);
    }

    [Fact]
    public async Task Status_history_is_append_only()
    {
        var (c, person, ed) = await SeedAsync("T2");
        await using var _ = c;
        var reg = Guid.NewGuid();
        await Register(c, ed, person, "T2-R1", reg);
        await c.ExecuteAsync("INSERT INTO registration_status_history(id,registration_id,to_status) VALUES (gen_random_uuid(),@reg,'SUBMITTED')", new { reg });
        var upd = await Assert.ThrowsAsync<PostgresException>(() => c.ExecuteAsync("UPDATE registration_status_history SET reason = 'x'"));
        Assert.Contains("append-only", upd.MessageText);
        await Assert.ThrowsAsync<PostgresException>(() => c.ExecuteAsync("DELETE FROM registration_status_history"));
    }
}
