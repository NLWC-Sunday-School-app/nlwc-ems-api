using NlwcEms.Api.Identity;

namespace NlwcEms.Api.People;

public static class PeopleEndpoints
{
    private static IResult Problem(int status, string code) => Results.Json(new { code }, statusCode: status);

    private static async Task<IResult> Run(Func<Task<IResult>> f)
    {
        try { return await f(); }
        catch (PeopleException e)
        {
            // "validation_failed:field" carries the field name
            var parts = e.Code.Split(':', 2);
            return parts.Length == 2 ? Results.Json(new { code = parts[0], field = parts[1] }, statusCode: e.Status) : Problem(e.Status, e.Code);
        }
    }

    public sealed record ReasonBody(string? Reason);

    public static void MapPeople(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/v1/me");

        g.MapGet("/profile", async (HttpContext http, PeopleService svc, Npgsql.NpgsqlDataSource db) =>
        {
            var s = http.Session();
            await using var c = await db.OpenConnectionAsync();
            var email = await Dapper.SqlMapper.ExecuteScalarAsync<string>(c, "SELECT email::text FROM account WHERE id = @id", new { id = s.AccountId });
            return Results.Ok(await svc.GetProfileAsync(s.PersonId, email!, s.EmailVerified));
        }).RequireSession();

        g.MapPut("/profile", (ProfileInput b, HttpContext http, PeopleService svc) => Run(async () =>
        {
            var s = http.Session();
            await svc.UpdateProfileAsync(s.AccountId, s.PersonId, b);
            return Results.NoContent();
        })).RequireSession();

        g.MapGet("/dependants", async (HttpContext http, PeopleService svc) => Results.Ok(await svc.ListDependantsAsync(http.Session().PersonId))).RequireSession();

        g.MapPost("/dependants", (DependantInput b, HttpContext http, PeopleService svc) => Run(async () =>
        {
            var s = http.Session();
            return Results.Json(new { personId = await svc.AddDependantAsync(s.AccountId, s.PersonId, b) }, statusCode: 201);
        })).RequireSession();

        g.MapPost("/dependants/{id:guid}/remove", (Guid id, ReasonBody b, HttpContext http, PeopleService svc) => Run(async () =>
        {
            if (string.IsNullOrWhiteSpace(b.Reason)) return Problem(400, "validation_failed");
            var s = http.Session();
            await svc.RemoveDependantAsync(s.AccountId, s.PersonId, id, b.Reason);
            return Results.NoContent();
        })).RequireSession();

        g.MapGet("/dependants/{id:guid}/collectors", (Guid id, HttpContext http, PeopleService svc) => Run(async () =>
            Results.Ok(await svc.ListCollectorsAsync(http.Session().PersonId, id)))).RequireSession();

        g.MapPost("/dependants/{id:guid}/collectors", (Guid id, CollectorInput b, HttpContext http, PeopleService svc) => Run(async () =>
        {
            var s = http.Session();
            return Results.Json(new { collectorId = await svc.AddCollectorAsync(s.AccountId, s.PersonId, id, b) }, statusCode: 201);
        })).RequireSession();

        g.MapDelete("/dependants/{id:guid}/collectors/{collectorId:guid}", (Guid id, Guid collectorId, HttpContext http, PeopleService svc) => Run(async () =>
        {
            var s = http.Session();
            await svc.RemoveCollectorAsync(s.AccountId, s.PersonId, id, collectorId);
            return Results.NoContent();
        })).RequireSession();

        // One emergency contact for yourself, and one per dependant.
        g.MapPut("/emergency-contact", (ContactInput b, HttpContext http, PeopleService svc) => Run(async () =>
        {
            var s = http.Session();
            await svc.SetEmergencyContactAsync(s.AccountId, s.PersonId, s.PersonId, b);
            return Results.NoContent();
        })).RequireSession();

        g.MapPut("/dependants/{id:guid}/emergency-contact", (Guid id, ContactInput b, HttpContext http, PeopleService svc) => Run(async () =>
        {
            var s = http.Session();
            await svc.SetEmergencyContactAsync(s.AccountId, s.PersonId, id, b);
            return Results.NoContent();
        })).RequireSession();

        g.MapGet("/dependants/{id:guid}/emergency-contact", (Guid id, HttpContext http, PeopleService svc) => Run(async () =>
            Results.Ok(await svc.GetEmergencyContactAsync(http.Session().PersonId, id)))).RequireSession();
    }
}
