using System.Text.RegularExpressions;
using Dapper;
using Npgsql;
using NlwcEms.Api.Access;
using NlwcEms.Api.Identity;

namespace NlwcEms.Api.People;

public sealed class PeopleException(string code, int status = 400) : Exception(code)
{
    public string Code { get; } = code;
    public int Status { get; } = status;
}

public sealed record ProfileInput(string? GivenName, string? FamilyName, string? MiddleName, string? Gender, string? MaritalStatus, string? Country, string? City, string? DateOfBirth);
public sealed record DependantInput(string? GivenName, string? FamilyName, string? DateOfBirth, string? Gender, string? Relationship);
public sealed record CollectorInput(string? FullName, string? Relationship, string? Phone, List<string>? ValidDays);
public sealed record ContactInput(string? FullName, string? Relationship, string? Phone);

public sealed partial class PeopleService(NpgsqlDataSource db, AppConfig config, AuditService audit)
{
    /// <summary>Dependants are guardian-managed people with no account. 13 and over manage their own profile.</summary>
    public const int MaxDependantAge = 12;

    // ---- validation helpers ----
    public static int AgeOn(DateOnly dob, DateOnly on) => on.Year - dob.Year - (on < dob.AddYears(on.Year - dob.Year) ? 1 : 0);

    private static DateOnly TodayLagos() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Africa/Lagos"));

    private static DateOnly ParseDob(string? s)
    {
        if (!DateOnly.TryParseExact(s, "yyyy-MM-dd", out var d) || d > TodayLagos() || d.Year < 1900) throw new PeopleException("invalid_date_of_birth");
        return d;
    }

    private static string Name(string? s, string field)
    {
        var t = s?.Trim();
        if (string.IsNullOrEmpty(t) || t.Length > 100) throw new PeopleException("validation_failed:" + field);
        return t;
    }

    /// <summary>E.164. Accepts +234..., 234..., or a Nigerian national number starting with 0.</summary>
    public static string NormalizePhone(string? raw)
    {
        var s = Regex.Replace(raw ?? "", @"[\s\-().]", "");
        if (s.StartsWith("0") && s.Length == 11) s = "+234" + s[1..];
        else if (s.StartsWith("234") && s.Length == 13) s = "+" + s;
        if (!E164().IsMatch(s)) throw new PeopleException("invalid_phone");
        return s;
    }

    [GeneratedRegex(@"^\+[1-9]\d{7,14}$")] private static partial Regex E164();

    private static string? OneOf(string? v, params string[] allowed) =>
        v is null ? null : allowed.Contains(v) ? v : throw new PeopleException("validation_failed");

    // ---- profile ----
    private sealed record PersonRow(Guid Id, string GivenName, string FamilyName, string? MiddleName, string? DateOfBirth, string? Gender, string? MaritalStatus, string? Country, string? City);

    private const string PersonSelect = "SELECT id, given_name, family_name, middle_name, to_char(date_of_birth,'YYYY-MM-DD') AS DateOfBirth, gender, marital_status, country, city FROM person";

    public async Task<object> GetProfileAsync(Guid personId, string email, bool emailVerified)
    {
        await using var c = await db.OpenConnectionAsync();
        var p = await c.QuerySingleAsync<PersonRow>(PersonSelect + " WHERE id = @personId", new { personId });
        var contact = await c.QuerySingleOrDefaultAsync<ContactRow>("SELECT id, full_name, relationship, phone_encrypted FROM emergency_contact WHERE person_id = @personId ORDER BY created_at DESC LIMIT 1", new { personId });
        return new
        {
            personId = p.Id, p.GivenName, p.FamilyName, p.MiddleName, p.DateOfBirth, p.Gender, p.MaritalStatus, p.Country, p.City,
            email, emailVerified,
            needsDateOfBirth = p.DateOfBirth is null,   // registration is refused until this exists
            emergencyContact = contact is null ? null : ShapeContact(contact),
        };
    }

    public async Task UpdateProfileAsync(Guid actorAccount, Guid personId, ProfileInput i)
    {
        var given = Name(i.GivenName, "givenName");
        var family = Name(i.FamilyName, "familyName");
        var gender = OneOf(i.Gender, "male", "female");
        DateOnly? dob = i.DateOfBirth is null ? null : ParseDob(i.DateOfBirth);
        await using var c = await db.OpenConnectionAsync();
        await using var tx = await c.BeginTransactionAsync();
        var current = await c.ExecuteScalarAsync<DateTime?>("SELECT date_of_birth FROM person WHERE id = @personId FOR UPDATE", new { personId }, tx);
        var firstDob = false;
        if (dob is not null)
        {
            if (current is not null && DateOnly.FromDateTime(current.Value) != dob) throw new PeopleException("dob_locked", 409);   // changes are reviewed, not self-served
            firstDob = current is null;
        }
        await c.ExecuteAsync(
            """
            UPDATE person SET given_name = @given, family_name = @family, middle_name = @MiddleName, gender = @gender,
                   marital_status = @MaritalStatus, country = @Country, city = @City,
                   date_of_birth = COALESCE(date_of_birth, @dob), updated_at = now()
            WHERE id = @personId
            """, new { given, family, i.MiddleName, gender, i.MaritalStatus, i.Country, i.City, dob = dob?.ToDateTime(TimeOnly.MinValue), personId }, tx);
        await tx.CommitAsync();
        if (firstDob) await audit.WriteAsync(actorAccount, "person.dob_set", "people", null, null, new { personId });
    }

    // ---- guardianship ----
    /// <summary>Not-found rather than forbidden, so a guardian cannot probe for other people's children.</summary>
    private static async Task RequireGuardianAsync(NpgsqlConnection c, Guid guardian, Guid dependant)
    {
        var ok = await c.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM guardian_authorization WHERE guardian_person_id = @guardian AND dependant_person_id = @dependant AND revoked_at IS NULL)", new { guardian, dependant });
        if (!ok) throw new PeopleException("not_found", 404);
    }

    private sealed record DependantRow(Guid Id, string GivenName, string FamilyName, string? DateOfBirth, string? Gender, string DeclaredRelationship);

    public async Task<IReadOnlyList<object>> ListDependantsAsync(Guid guardian)
    {
        await using var c = await db.OpenConnectionAsync();
        var rows = await c.QueryAsync<DependantRow>(
            """
            SELECT p.id, p.given_name, p.family_name, to_char(p.date_of_birth,'YYYY-MM-DD') AS DateOfBirth, p.gender, g.declared_relationship
            FROM guardian_authorization g JOIN person p ON p.id = g.dependant_person_id
            WHERE g.guardian_person_id = @guardian AND g.revoked_at IS NULL ORDER BY p.date_of_birth
            """, new { guardian });
        return rows.Select(r => (object)new { personId = r.Id, r.GivenName, r.FamilyName, r.DateOfBirth, r.Gender, relationship = r.DeclaredRelationship }).ToList();
    }

    public async Task<Guid> AddDependantAsync(Guid actorAccount, Guid guardian, DependantInput i)
    {
        var given = Name(i.GivenName, "givenName");
        var family = Name(i.FamilyName, "familyName");
        var dob = ParseDob(i.DateOfBirth);
        var gender = OneOf(i.Gender, "male", "female");
        var rel = OneOf(i.Relationship, "parent", "legal_guardian", "grandparent", "other_relative") ?? throw new PeopleException("validation_failed");
        if (AgeOn(dob, TodayLagos()) > MaxDependantAge) throw new PeopleException("too_old_for_dependant");
        var id = Crypto.NewId();
        await using var c = await db.OpenConnectionAsync();
        await using var tx = await c.BeginTransactionAsync();
        await c.ExecuteAsync("INSERT INTO person(id, given_name, family_name, date_of_birth, gender, created_by) VALUES (@id, @given, @family, @dob, @gender, @guardian)",
            new { id, given, family, dob = dob.ToDateTime(TimeOnly.MinValue), gender, guardian }, tx);
        await c.ExecuteAsync("INSERT INTO guardian_authorization(id, guardian_person_id, dependant_person_id, declared_relationship) VALUES (@x, @guardian, @id, @rel)", new { x = Crypto.NewId(), guardian, id, rel }, tx);
        await c.ExecuteAsync("INSERT INTO person_relationship(id, from_person_id, to_person_id, kind) VALUES (@x, @id, @guardian, 'dependant_of')", new { x = Crypto.NewId(), id, guardian }, tx);
        await tx.CommitAsync();
        await audit.WriteAsync(actorAccount, "dependant.added", "people", null, null, new { guardian, dependant = id, relationship = rel });
        return id;
    }

    public async Task RemoveDependantAsync(Guid actorAccount, Guid guardian, Guid dependant, string reason)
    {
        await using var c = await db.OpenConnectionAsync();
        await RequireGuardianAsync(c, guardian, dependant);
        await c.ExecuteAsync("UPDATE guardian_authorization SET revoked_at = now() WHERE guardian_person_id = @guardian AND dependant_person_id = @dependant AND revoked_at IS NULL", new { guardian, dependant });
        await audit.WriteAsync(actorAccount, "dependant.removed", "people", null, reason, new { guardian, dependant });
    }

    // ---- authorised collectors ----
    private sealed record CollectorRow(Guid Id, string FullName, string Relationship, byte[] PhoneEncrypted, Array? ValidDays);

    private object ShapeCollector(CollectorRow r) => new
    {
        collectorId = r.Id, r.FullName, r.Relationship,
        phone = System.Text.Encoding.UTF8.GetString(Crypto.Decrypt(config.MasterKey, r.PhoneEncrypted)),
        validDays = r.ValidDays as string[],   // null = always
    };

    public async Task<IReadOnlyList<object>> ListCollectorsAsync(Guid guardian, Guid child)
    {
        await using var c = await db.OpenConnectionAsync();
        await RequireGuardianAsync(c, guardian, child);
        var rows = await c.QueryAsync<CollectorRow>("SELECT id, full_name, relationship, phone_encrypted, CASE WHEN valid_days IS NULL THEN NULL ELSE ARRAY(SELECT to_char(d, 'YYYY-MM-DD') FROM unnest(valid_days) AS d ORDER BY d) END AS ValidDays FROM authorized_collector WHERE child_person_id = @child AND removed_at IS NULL ORDER BY created_at", new { child });
        return rows.Select(ShapeCollector).ToList();
    }

    public async Task<Guid> AddCollectorAsync(Guid actorAccount, Guid guardian, Guid child, CollectorInput i)
    {
        var name = Name(i.FullName, "fullName");
        var rel = Name(i.Relationship, "relationship");
        var phone = NormalizePhone(i.Phone);
        DateOnly[]? days = i.ValidDays?.Select(ParseDay).ToArray();
        if (days is { Length: 0 }) throw new PeopleException("validation_failed:validDays");
        await using var c = await db.OpenConnectionAsync();
        await RequireGuardianAsync(c, guardian, child);
        var id = Crypto.NewId();
        // Typed parameter: a null date[] sent as untyped text cannot be cast by Postgres.
        await using (var cmd = new NpgsqlCommand("INSERT INTO authorized_collector(id, child_person_id, full_name, relationship, phone_encrypted, valid_days, created_by) VALUES (@id, @child, @name, @rel, @phone, @days, @guardian)", c))
        {
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("child", child);
            cmd.Parameters.AddWithValue("name", name);
            cmd.Parameters.AddWithValue("rel", rel);
            cmd.Parameters.AddWithValue("phone", Crypto.Encrypt(config.MasterKey, System.Text.Encoding.UTF8.GetBytes(phone)));
            cmd.Parameters.Add(new NpgsqlParameter("days", NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Date) { Value = (object?)days ?? DBNull.Value });
            cmd.Parameters.AddWithValue("guardian", guardian);
            await cmd.ExecuteNonQueryAsync();
        }
        await audit.WriteAsync(actorAccount, "collector.added", "custody", null, null, new { child, collectorId = id });
        return id;
    }

    private static DateOnly ParseDay(string s) =>
        DateOnly.TryParseExact(s, "yyyy-MM-dd", out var d) ? d : throw new PeopleException("validation_failed:validDays");

    public async Task RemoveCollectorAsync(Guid actorAccount, Guid guardian, Guid child, Guid collectorId)
    {
        await using var c = await db.OpenConnectionAsync();
        await RequireGuardianAsync(c, guardian, child);
        var n = await c.ExecuteAsync("UPDATE authorized_collector SET removed_at = now() WHERE id = @collectorId AND child_person_id = @child AND removed_at IS NULL", new { collectorId, child });
        if (n == 0) throw new PeopleException("not_found", 404);
        await audit.WriteAsync(actorAccount, "collector.removed", "custody", null, null, new { child, collectorId });
    }

    // ---- emergency contact (one per person; for a child, set by the guardian) ----
    private sealed record ContactRow(Guid Id, string FullName, string Relationship, byte[] PhoneEncrypted);

    private object ShapeContact(ContactRow r) => new
    {
        r.FullName, r.Relationship,
        phone = System.Text.Encoding.UTF8.GetString(Crypto.Decrypt(config.MasterKey, r.PhoneEncrypted)),
    };

    public async Task SetEmergencyContactAsync(Guid actorAccount, Guid guardianOrSelf, Guid subject, ContactInput i)
    {
        var name = Name(i.FullName, "fullName");
        var rel = Name(i.Relationship, "relationship");
        var phone = Crypto.Encrypt(config.MasterKey, System.Text.Encoding.UTF8.GetBytes(NormalizePhone(i.Phone)));
        await using var c = await db.OpenConnectionAsync();
        if (subject != guardianOrSelf) await RequireGuardianAsync(c, guardianOrSelf, subject);
        await using var tx = await c.BeginTransactionAsync();
        await c.ExecuteAsync("DELETE FROM emergency_contact WHERE person_id = @subject", new { subject }, tx);
        await c.ExecuteAsync("INSERT INTO emergency_contact(id, person_id, full_name, relationship, phone_encrypted) VALUES (@id, @subject, @name, @rel, @phone)", new { id = Crypto.NewId(), subject, name, rel, phone }, tx);
        await tx.CommitAsync();
        await audit.WriteAsync(actorAccount, "emergency_contact.set", "people", null, null, new { subject });
    }

    public async Task<object?> GetEmergencyContactAsync(Guid guardianOrSelf, Guid subject)
    {
        await using var c = await db.OpenConnectionAsync();
        if (subject != guardianOrSelf) await RequireGuardianAsync(c, guardianOrSelf, subject);
        var r = await c.QuerySingleOrDefaultAsync<ContactRow>("SELECT id, full_name, relationship, phone_encrypted FROM emergency_contact WHERE person_id = @subject LIMIT 1", new { subject });
        return r is null ? null : ShapeContact(r);
    }
}
