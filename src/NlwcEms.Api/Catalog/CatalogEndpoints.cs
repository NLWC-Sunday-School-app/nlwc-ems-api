using NlwcEms.Api.Access;
using NlwcEms.Api.Identity;

namespace NlwcEms.Api.Catalog;

public static class CatalogEndpoints
{
    private static IResult Problem(int status, string code) => Results.Json(new { code }, statusCode: status);

    private static async Task<IResult> Run(Func<Task<IResult>> f)
    {
        try { return await f(); }
        catch (CatalogException e) { return Problem(e.Status, e.Code); }
    }

    public sealed record CreateBody(string? TemplateCode, string? Slug, string? Code, string? Name, DateOnly? StartsOn, DateOnly? EndsOn,
        string? VenueName, string? VenueAddress, int? CampMinAge, int? CampMaxAge, int? CampCapacity, string? PickupStart, string? PickupEnd);
    public sealed record CapabilityBody(bool? Enabled, string? Reason);
    public sealed record StageBody(string? To, string? Reason);
    public sealed record DraftBody(string? Note, List<RuleInput>? Rules);
    public sealed record NoteBody(string? Note);

    public static void MapCatalog(this IEndpointRouteBuilder app)
    {
        // Public: no session. Only editions whose visibility is 'public' are returned.
        app.MapGet("/v1/editions", async (CatalogService c) => Results.Ok(await c.ListPublicAsync()));
        app.MapGet("/v1/editions/{slug}", async (string slug, CatalogService c) =>
            await c.GetPublicAsync(slug) is { } e ? Results.Ok(e) : Problem(404, "not_found"));

        var a = app.MapGroup("/v1/admin/editions");

        a.MapGet("", async (CatalogService c) => Results.Ok(await c.ListAdminAsync())).RequirePermission("config", "view");

        a.MapGet("/{editionId:guid}", (Guid editionId, CatalogService c) => Run(async () => Results.Ok(await c.GetAdminAsync(editionId))))
            .RequirePermission("config", "view");

        a.MapPost("", (CreateBody b, HttpContext http, CatalogService c) => Run(async () =>
        {
            if (b.TemplateCode is null || b.Slug is null || b.Code is null || b.Name is null || b.StartsOn is null || b.EndsOn is null) return Problem(400, "validation_failed");
            var id = await c.CreateAsync(http.Session().AccountId, new(b.TemplateCode, b.Slug, b.Code, b.Name, b.StartsOn.Value, b.EndsOn.Value, b.VenueName, b.VenueAddress, b.CampMinAge, b.CampMaxAge, b.CampCapacity, b.PickupStart, b.PickupEnd));
            return Results.Json(new { id }, statusCode: 201);
        })).RequirePermission("config", "edit");   // church-wide: no edition in scope yet

        a.MapPut("/{editionId:guid}/capabilities/{capability}", (Guid editionId, string capability, CapabilityBody b, HttpContext http, CatalogService c) => Run(async () =>
        {
            if (b.Enabled is null) return Problem(400, "validation_failed");
            await c.SetCapabilityAsync(http.Session().AccountId, editionId, capability, b.Enabled.Value, b.Reason);
            return Results.NoContent();
        })).RequirePermission("config", "edit");

        a.MapPost("/{editionId:guid}/stage", (Guid editionId, StageBody b, HttpContext http, CatalogService c) => Run(async () =>
        {
            if (b.To is null || string.IsNullOrWhiteSpace(b.Reason)) return Problem(400, "validation_failed");   // dangerous actions need a reason
            await c.TransitionAsync(http.Session().AccountId, editionId, b.To, b.Reason);
            return Results.NoContent();
        })).RequirePermission("config", "edit");

        a.MapPost("/{editionId:guid}/rates", (Guid editionId, DraftBody b, HttpContext http, CatalogService c) => Run(async () =>
        {
            if (b.Rules is null) return Problem(400, "validation_failed");
            return Results.Json(new { versionId = await c.DraftRatesAsync(http.Session().AccountId, editionId, b.Note, b.Rules) }, statusCode: 201);
        })).RequirePermission("pricing", "draft");

        a.MapPost("/{editionId:guid}/rates/{versionId:guid}/approve", (Guid editionId, Guid versionId, NoteBody b, HttpContext http, CatalogService c) => Run(async () =>
        {
            await c.ApproveRatesAsync(http.Session().AccountId, editionId, versionId, b.Note);
            return Results.NoContent();
        })).RequirePermission("pricing", "approve");

        a.MapPost("/{editionId:guid}/rates/{versionId:guid}/publish", (Guid editionId, Guid versionId, NoteBody b, HttpContext http, CatalogService c) => Run(async () =>
        {
            await c.PublishRatesAsync(http.Session().AccountId, editionId, versionId, b.Note);
            return Results.NoContent();
        })).RequirePermission("pricing", "approve");
    }
}
