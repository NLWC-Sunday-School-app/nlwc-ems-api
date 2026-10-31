using Dapper;
using Npgsql;
using NlwcEms.Api.Access;
using NlwcEms.Api.Identity;

namespace NlwcEms.Api.Catalog;

public sealed class CatalogException(string code, int status = 400) : Exception(code)
{
    public string Code { get; } = code;
    public int Status { get; } = status;
}

public sealed record RuleInput(int AgeMin, int? AgeMax, string? ParticipationMode, string? AccommodationCategory, string Kind, long? AmountKobo);

public sealed class CatalogService(NpgsqlDataSource db, AuditService audit)
{
    public static readonly string[] Capabilities = ["accommodation", "daily_checkin", "camp_review", "studio_requests", "parent_forum", "paid_registration"];

    private const string EditionSelect = """
        SELECT e.id, e.slug, e.code, e.name, e.timezone,
               to_char(e.starts_on,'YYYY-MM-DD') AS StartsOn, to_char(e.ends_on,'YYYY-MM-DD') AS EndsOn,
               to_char(e.age_reference_date,'YYYY-MM-DD') AS AgeReferenceDate,
               e.camp_min_age AS CampMinAge, e.camp_max_age AS CampMaxAge, e.camp_capacity AS CampCapacity,
               to_char(e.pickup_window_start,'HH24:MI') AS PickupStart, to_char(e.pickup_window_end,'HH24:MI') AS PickupEnd,
               e.visibility, e.registration_state AS RegistrationState, e.live_state AS LiveState,
               v.name AS VenueName, v.address AS VenueAddress
        FROM event_edition e LEFT JOIN venue v ON v.id = e.venue_id
        """;

    private sealed record EditionRow(Guid Id, string Slug, string Code, string Name, string Timezone, string StartsOn, string EndsOn, string AgeReferenceDate,
        int? CampMinAge, int? CampMaxAge, int? CampCapacity, string? PickupStart, string? PickupEnd, string Visibility, string RegistrationState, string LiveState,
        string? VenueName, string? VenueAddress);

    private async Task<object> ShapeAsync(NpgsqlConnection c, EditionRow e, bool admin)
    {
        var caps = (await c.QueryAsync<(string Capability, bool Enabled)>("SELECT capability, enabled FROM edition_capability WHERE edition_id = @id", new { id = e.Id })).ToList();
        var days = (await c.QueryAsync<(string Day, string? Label)>("SELECT to_char(day,'YYYY-MM-DD') AS Day, label FROM event_day WHERE edition_id = @id ORDER BY day", new { id = e.Id }))
            .Select(d => new { day = d.Day, label = d.Label }).ToList();
        return new
        {
            id = admin ? e.Id : (Guid?)null,
            e.Slug, e.Code, e.Name, e.Timezone, e.StartsOn, e.EndsOn,
            venue = e.VenueName is null ? null : new { name = e.VenueName, address = e.VenueAddress },
            camp = e.CampMinAge is null ? null : new { minAge = e.CampMinAge, maxAge = e.CampMaxAge, capacity = admin ? e.CampCapacity : null },
            pickupWindow = e.PickupStart is null ? null : new { start = e.PickupStart, end = e.PickupEnd },
            stage = Lifecycle.StageOf(e.Visibility, e.RegistrationState, e.LiveState),
            e.RegistrationState,
            capabilities = caps.Where(x => x.Enabled).Select(x => x.Capability).Order().ToList(),
            price = new { state = await PriceStateAsync(c, e.Id) },
            days,
        };
    }

    /// <summary>"free" only from an explicit published FREE rule set; "pending" when nothing is published.</summary>
    private static async Task<string> PriceStateAsync(NpgsqlConnection c, Guid editionId)
    {
        var kinds = (await c.QueryAsync<string>(
            """
            SELECT r.kind FROM rate_plan p
            JOIN rate_plan_version v ON v.plan_id = p.id AND v.state = 'published'
            JOIN rate_rule r ON r.version_id = v.id
            WHERE p.edition_id = @editionId
            """, new { editionId })).ToList();
        return kinds.Count == 0 ? "pending" : kinds.All(k => k == "FREE") ? "free" : "amount";
    }

    // ---- public ----
    public async Task<IReadOnlyList<object>> ListPublicAsync()
    {
        await using var c = await db.OpenConnectionAsync();
        var rows = await c.QueryAsync<EditionRow>(EditionSelect + " WHERE e.visibility = 'public' ORDER BY e.starts_on");
        var result = new List<object>();
        foreach (var r in rows) result.Add(await ShapeAsync(c, r, false));
        return result;
    }

    public async Task<object?> GetPublicAsync(string slug)
    {
        await using var c = await db.OpenConnectionAsync();
        var r = await c.QuerySingleOrDefaultAsync<EditionRow>(EditionSelect + " WHERE e.slug = @slug AND e.visibility = 'public'", new { slug });
        return r is null ? null : await ShapeAsync(c, r, false);
    }

    // ---- admin ----
    public async Task<IReadOnlyList<object>> ListAdminAsync()
    {
        await using var c = await db.OpenConnectionAsync();
        var rows = await c.QueryAsync<EditionRow>(EditionSelect + " ORDER BY e.starts_on DESC");
        var result = new List<object>();
        foreach (var r in rows) result.Add(await ShapeAsync(c, r, true));
        return result;
    }

    public async Task<object> GetAdminAsync(Guid id)
    {
        await using var c = await db.OpenConnectionAsync();
        var r = await c.QuerySingleOrDefaultAsync<EditionRow>(EditionSelect + " WHERE e.id = @id", new { id }) ?? throw new CatalogException("not_found", 404);
        return await ShapeAsync(c, r, true);
    }

    public sealed record CreateEdition(string TemplateCode, string Slug, string Code, string Name, DateOnly StartsOn, DateOnly EndsOn,
        string? VenueName, string? VenueAddress, int? CampMinAge, int? CampMaxAge, int? CampCapacity, string? PickupStart, string? PickupEnd);

    public async Task<Guid> CreateAsync(Guid actor, CreateEdition i)
    {
        if (i.EndsOn < i.StartsOn) throw new CatalogException("validation_failed");
        if (i.CampMinAge > i.CampMaxAge) throw new CatalogException("validation_failed");
        await using var c = await db.OpenConnectionAsync();
        await using var tx = await c.BeginTransactionAsync();
        var tpl = await c.ExecuteScalarAsync<Guid?>("SELECT id FROM event_template WHERE code = @code", new { code = i.TemplateCode }, tx) ?? throw new CatalogException("unknown_template");
        Guid? venue = null;
        if (i.VenueName is not null)
        {
            venue = Crypto.NewId();
            await c.ExecuteAsync("INSERT INTO venue(id, name, address) VALUES (@venue, @VenueName, @VenueAddress)", new { venue, i.VenueName, i.VenueAddress }, tx);
        }
        var id = Crypto.NewId();
        try
        {
            await c.ExecuteAsync(
                """
                INSERT INTO event_edition(id, template_id, venue_id, slug, code, name, starts_on, ends_on, age_reference_date,
                                          camp_min_age, camp_max_age, camp_capacity, pickup_window_start, pickup_window_end)
                VALUES (@id, @tpl, @venue, @Slug, @Code, @Name, @StartsOn, @EndsOn, @StartsOn,
                        @CampMinAge, @CampMaxAge, @CampCapacity, @ps::time, @pe::time)
                """, new { id, tpl, venue, i.Slug, i.Code, i.Name, StartsOn = i.StartsOn.ToDateTime(TimeOnly.MinValue), EndsOn = i.EndsOn.ToDateTime(TimeOnly.MinValue), i.CampMinAge, i.CampMaxAge, i.CampCapacity, ps = i.PickupStart, pe = i.PickupEnd }, tx);
        }
        catch (PostgresException ex) when (ex.SqlState == "23505") { throw new CatalogException("duplicate", 409); }
        for (var d = i.StartsOn; d <= i.EndsOn; d = d.AddDays(1))
            await c.ExecuteAsync("INSERT INTO event_day(id, edition_id, day) VALUES (@x, @id, @d)", new { x = Crypto.NewId(), id, d = d.ToDateTime(TimeOnly.MinValue) }, tx);
        foreach (var cap in Capabilities)
            await c.ExecuteAsync("INSERT INTO edition_capability(edition_id, capability, enabled) VALUES (@id, @cap, false)", new { id, cap }, tx);
        await c.ExecuteAsync("INSERT INTO reference_counter(edition_id) VALUES (@id)", new { id }, tx);
        await tx.CommitAsync();
        await audit.WriteAsync(actor, "edition.created", "config", id, null, new { i.Slug, i.Code });
        return id;
    }

    public async Task SetCapabilityAsync(Guid actor, Guid editionId, string capability, bool enabled, string? reason)
    {
        if (!Capabilities.Contains(capability)) throw new CatalogException("unknown_capability");
        await using var c = await db.OpenConnectionAsync();
        var n = await c.ExecuteAsync("UPDATE edition_capability SET enabled = @enabled WHERE edition_id = @editionId AND capability = @capability", new { enabled, editionId, capability });
        if (n == 0) throw new CatalogException("not_found", 404);
        await audit.WriteAsync(actor, "edition.capability", "config", editionId, reason, new { capability, enabled });
    }

    private sealed record StageRow(string Visibility, string RegistrationState, string LiveState);
    private sealed record VersionRow(Guid EditionId, Guid PlanId, string State, Guid DraftedBy);

    public async Task TransitionAsync(Guid actor, Guid editionId, string to, string reason)
    {
        if (!Lifecycle.Stages.TryGetValue(to, out var target)) throw new CatalogException("unknown_stage");
        await using var c = await db.OpenConnectionAsync();
        await using var tx = await c.BeginTransactionAsync();
        var cur = await c.QuerySingleOrDefaultAsync<StageRow>("SELECT visibility, registration_state AS RegistrationState, live_state AS LiveState FROM event_edition WHERE id = @editionId FOR UPDATE", new { editionId }, tx)
            ?? throw new CatalogException("not_found", 404);
        var from = Lifecycle.StageOf(cur.Visibility, cur.RegistrationState, cur.LiveState)!;
        if (!Lifecycle.CanMove(from, to)) throw new CatalogException("invalid_transition", 409);
        if (to == "registration_open" && await PriceStateAsync(c, editionId) == "pending")
            throw new CatalogException("pricing_not_published", 409);   // unconfigured price blocks opening (PRICE-03)
        await c.ExecuteAsync("UPDATE event_edition SET visibility = @v, registration_state = @r, live_state = @l, updated_at = now() WHERE id = @editionId",
            new { v = target.Visibility, r = target.Registration, l = target.Live, editionId }, tx);
        await tx.CommitAsync();
        await audit.WriteAsync(actor, "edition.stage", "config", editionId, reason, new { from, to });
    }

    // ---- pricing model ----
    private static void ValidateRules(IReadOnlyList<RuleInput> rules)
    {
        if (rules.Count == 0) throw new CatalogException("validation_failed");
        foreach (var r in rules)
        {
            var ok = r.AgeMin >= 0 && (r.AgeMax is null || r.AgeMax >= r.AgeMin)
                && (r.ParticipationMode is null or "day" or "camp" or "online")
                && ((r.Kind == "FREE" && r.AmountKobo is null) || (r.Kind == "AMOUNT" && r.AmountKobo is > 0));
            if (!ok) throw new CatalogException("validation_failed");
        }
        // No two rules may cover the same age for the same mode and category.
        foreach (var g in rules.GroupBy(r => (r.ParticipationMode, r.AccommodationCategory)))
        {
            var sorted = g.OrderBy(r => r.AgeMin).ToList();
            for (var i = 1; i < sorted.Count; i++)
                if (sorted[i - 1].AgeMax is null || sorted[i - 1].AgeMax >= sorted[i].AgeMin) throw new CatalogException("overlapping_rules");
        }
    }

    public async Task<Guid> DraftRatesAsync(Guid actor, Guid editionId, string? note, IReadOnlyList<RuleInput> rules)
    {
        ValidateRules(rules);
        await using var c = await db.OpenConnectionAsync();
        await using var tx = await c.BeginTransactionAsync();
        if (await c.ExecuteScalarAsync<bool>("SELECT NOT EXISTS (SELECT 1 FROM event_edition WHERE id = @editionId)", new { editionId }, tx)) throw new CatalogException("not_found", 404);
        var planId = await c.ExecuteScalarAsync<Guid?>("SELECT id FROM rate_plan WHERE edition_id = @editionId", new { editionId }, tx);
        if (planId is null)
        {
            planId = Crypto.NewId();
            await c.ExecuteAsync("INSERT INTO rate_plan(id, edition_id, name) VALUES (@planId, @editionId, 'Rates')", new { planId, editionId }, tx);
        }
        await c.ExecuteAsync("SELECT pg_advisory_xact_lock(hashtext(@k))", new { k = planId.ToString() }, tx);
        var version = await c.ExecuteScalarAsync<int>("SELECT COALESCE(MAX(version), 0) + 1 FROM rate_plan_version WHERE plan_id = @planId", new { planId }, tx);
        var vid = Crypto.NewId();
        await c.ExecuteAsync("INSERT INTO rate_plan_version(id, plan_id, version, drafted_by, note) VALUES (@vid, @planId, @version, @actor, @note)", new { vid, planId, version, actor, note }, tx);
        foreach (var r in rules)
            await c.ExecuteAsync("INSERT INTO rate_rule(id, version_id, age_min, age_max, participation_mode, accommodation_category, kind, amount_kobo) VALUES (@id, @vid, @AgeMin, @AgeMax, @ParticipationMode, @AccommodationCategory, @Kind, @AmountKobo)",
                new { id = Crypto.NewId(), vid, r.AgeMin, r.AgeMax, r.ParticipationMode, r.AccommodationCategory, r.Kind, r.AmountKobo }, tx);
        await tx.CommitAsync();
        await audit.WriteAsync(actor, "rates.drafted", "pricing", editionId, note, new { versionId = vid, version, rules = rules.Count });
        return vid;
    }

    private async Task<VersionRow> VersionAsync(NpgsqlConnection c, NpgsqlTransaction tx, Guid versionId, Guid editionId) =>
        await c.QuerySingleOrDefaultAsync<VersionRow>(
            "SELECT p.edition_id AS EditionId, p.id AS PlanId, v.state, v.drafted_by AS DraftedBy FROM rate_plan_version v JOIN rate_plan p ON p.id = v.plan_id WHERE v.id = @versionId AND p.edition_id = @editionId FOR UPDATE OF v",
            new { versionId, editionId }, tx) ?? throw new CatalogException("not_found", 404);

    public async Task ApproveRatesAsync(Guid actor, Guid editionId, Guid versionId, string? note)
    {
        await using var c = await db.OpenConnectionAsync();
        await using var tx = await c.BeginTransactionAsync();
        var v = await VersionAsync(c, tx, versionId, editionId);
        if (v.State != "draft") throw new CatalogException("invalid_transition", 409);
        if (v.DraftedBy == actor) throw new CatalogException("separation_of_duties", 403);   // drafter cannot approve
        await c.ExecuteAsync("UPDATE rate_plan_version SET state = 'approved', approved_by = @actor, approved_at = now() WHERE id = @versionId", new { actor, versionId }, tx);
        await tx.CommitAsync();
        await audit.WriteAsync(actor, "rates.approved", "pricing", editionId, note, new { versionId });
    }

    public async Task PublishRatesAsync(Guid actor, Guid editionId, Guid versionId, string? note)
    {
        await using var c = await db.OpenConnectionAsync();
        await using var tx = await c.BeginTransactionAsync();
        var v = await VersionAsync(c, tx, versionId, editionId);
        if (v.State != "approved") throw new CatalogException("invalid_transition", 409);
        await c.ExecuteAsync("UPDATE rate_plan_version SET state = 'retired' WHERE plan_id = @planId AND state = 'published'", new { planId = v.PlanId }, tx);
        await c.ExecuteAsync("UPDATE rate_plan_version SET state = 'published', published_at = now() WHERE id = @versionId", new { versionId }, tx);
        await tx.CommitAsync();
        await audit.WriteAsync(actor, "rates.published", "pricing", editionId, note, new { versionId });
    }
}
