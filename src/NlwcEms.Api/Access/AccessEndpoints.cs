using NlwcEms.Api.Identity;

namespace NlwcEms.Api.Access;

public static class AccessEndpoints
{
    private static IResult Problem(int status, string code) => Results.Json(new { code }, statusCode: status);

    /// <summary>
    /// Requires a session, MFA (every permissioned role is a staff role), and can(actor, resource, level, edition).
    /// The edition comes from the <c>editionId</c> route value, else the query string. Denials are audited.
    /// </summary>
    public static RouteHandlerBuilder RequirePermission(this RouteHandlerBuilder b, string resource, string level)
    {
        if (!Permissions.Known(resource, level)) throw new ArgumentException($"unknown permission {resource}:{level}");
        return b.RequireSession().AddEndpointFilter(async (ctx, next) =>
        {
            var http = ctx.HttpContext;
            var s = http.Session();
            var raw = http.Request.RouteValues["editionId"]?.ToString() ?? http.Request.Query["editionId"].ToString();
            Guid? edition = Guid.TryParse(raw, out var e) ? e : null;
            var access = http.RequestServices.GetRequiredService<AccessService>();
            if (!await access.CanAsync(s.AccountId, resource, level, edition))
            {
                await http.RequestServices.GetRequiredService<AuditService>()
                    .WriteAsync(s.AccountId, "access.denied", resource, edition, null, new { level, path = http.Request.Path.Value });
                return Problem(403, "forbidden");
            }
            if (!s.MfaVerified) return Problem(403, "mfa_required");
            return await next(ctx);
        });   // filters run in the order added, so the session filter above runs first
    }

    public sealed record GrantBody(Guid? AccountId, string? Role, Guid? EditionId, string? Reason, DateTime? ExpiresAt);
    public sealed record ReasonBody(string? Reason);

    public static void MapAccess(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/v1");

        // The API returns the permission set so admin can build navigation from it. Hiding links is not security.
        g.MapGet("/access/me", async (HttpContext http, AccessService access, Guid? editionId) =>
            Results.Ok(new { permissions = await access.PermissionsAsync(http.Session().AccountId, editionId), mfaVerified = http.Session().MfaVerified }))
            .RequireSession();

        g.MapPost("/admin/staff/grants", async (GrantBody b, HttpContext http, AccessService access) =>
        {
            if (b.AccountId is null || string.IsNullOrWhiteSpace(b.Role) || string.IsNullOrWhiteSpace(b.Reason)) return Problem(400, "validation_failed");
            if (b.ExpiresAt is { } x && x <= DateTime.UtcNow) return Problem(400, "validation_failed");
            try { return Results.Ok(new { id = await access.GrantAsync(http.Session().AccountId, b.AccountId.Value, b.Role, b.EditionId, b.Reason, b.ExpiresAt) }); }
            catch (AuthException ex) { return Problem(400, ex.Code); }
        }).RequirePermission("staff", "edit");

        g.MapPost("/admin/staff/grants/{id:guid}/revoke", async (Guid id, ReasonBody b, HttpContext http, AccessService access) =>
        {
            if (string.IsNullOrWhiteSpace(b.Reason)) return Problem(400, "validation_failed");
            try { await access.RevokeAsync(http.Session().AccountId, id, b.Reason); return Results.NoContent(); }
            catch (AuthException ex) { return Problem(404, ex.Code); }
        }).RequirePermission("staff", "edit");

        g.MapGet("/admin/audit", async (Npgsql.NpgsqlDataSource db, int? limit) =>
        {
            await using var conn = await db.OpenConnectionAsync();
            var rows = await Dapper.SqlMapper.QueryAsync(conn,
                "SELECT seq, occurred_at AS \"occurredAt\", actor_account_id AS \"actor\", action, resource, reason FROM audit_event ORDER BY seq DESC LIMIT @n",
                new { n = Math.Clamp(limit ?? 50, 1, 200) });
            return Results.Ok(rows);
        }).RequirePermission("audit", "view");
    }
}
