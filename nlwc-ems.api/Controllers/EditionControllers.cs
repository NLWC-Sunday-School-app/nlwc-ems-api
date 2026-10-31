using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nlwc.ems.Core.Filters;
using nlwc.ems.Core.Models;
using nlwc.ems.Core.Security;
using nlwc.ems.Dtos;

namespace nlwc.ems.Controllers;

/// <summary>
/// Admin edition management. An edition is never deleted or toggled active/inactive: it moves through its lifecycle and
/// is finally archived. Edition-scoped grants count: the edition id in the route is the permission scope.
/// </summary>
public class EditionController(IEditionService editionService, IRatePlanService ratePlanService)
    : BaseApiController<IEditionService, EventEdition, EditionFilter, CreateEditionRequest, UpdateEditionRequest>(editionService)
{
    protected override IReadOnlyCollection<string> ViewPermissionCodes => [EmsPermissions.Config.View];
    protected override string? CreatePermissionCode => EmsPermissions.Config.Edit;
    protected override string? UpdatePermissionCode => EmsPermissions.Config.Edit;
    protected override Guid? PermissionScope(Guid? routeId) => routeId;

    [NonAction]
    public override Task<ActionResult<ApiResponse>> Remove(Guid id) => base.Remove(id);

    [NonAction]
    public override Task<ActionResult<ApiResponse<EventEdition>>> Enable(Guid id) => base.Enable(id);

    [NonAction]
    public override Task<ActionResult<ApiResponse<EventEdition>>> Disable(Guid id) => base.Disable(id);

    protected override Task<EventEdition> MapToNewAsync(CreateEditionRequest request) => Task.FromResult(new EventEdition
    {
        Slug = request.Slug, Code = request.Code.ToUpperInvariant(), Name = request.Name.Trim(),
        StartsOn = request.StartsOn!.Value, EndsOn = request.EndsOn!.Value,
        CampMinAge = request.CampMinAge, CampMaxAge = request.CampMaxAge, CampCapacity = request.CampCapacity,
        PickupWindowStart = request.PickupWindowStart, PickupWindowEnd = request.PickupWindowEnd
    });

    // The venue, days, capability switches and reference counter are created with the edition, not by a plain insert.
    protected override Task<ServiceResponse<EventEdition>> InsertAsync(EventEdition entity, CreateEditionRequest request)
        => Service.CreateAsync(entity, request);

    protected override Task ApplyUpdateAsync(EventEdition edition, UpdateEditionRequest request)
    {
        edition.Name = request.Name.Trim();
        edition.CampMinAge = request.CampMinAge;
        edition.CampMaxAge = request.CampMaxAge;
        edition.CampCapacity = request.CampCapacity;
        edition.PickupWindowStart = request.PickupWindowStart;
        edition.PickupWindowEnd = request.PickupWindowEnd;
        return Task.CompletedTask;
    }

    /// <summary>Switches a capability on or off for this edition. Off removes its screens and API for the edition.</summary>
    [HttpPut("{editionId:guid}/capability/{capability}")]
    [RequirePermission(EmsPermissions.Config.Edit)]
    public async Task<ActionResult<ApiResponse>> SetCapability(Guid editionId, Capability capability, [FromBody] SetCapabilityRequest request)
    {
        await Service.SetCapabilityAsync(editionId, capability, request.Enabled!.Value, request.Reason);
        return Ok(ApiResponse.Ok("Capability updated."));
    }

    /// <summary>Moves the edition to another lifecycle stage. Only valid moves are accepted, and each records why.</summary>
    [HttpPost("{editionId:guid}/stage")]
    [RequirePermission(EmsPermissions.Config.Edit)]
    public async Task<ActionResult<ApiResponse>> Transition(Guid editionId, [FromBody] TransitionEditionRequest request)
    {
        await Service.TransitionAsync(editionId, request.To!.Value, request.Reason);
        return Ok(ApiResponse.Ok("Stage updated."));
    }

    /// <summary>Drafts a new version of the edition's rates. Free is an explicit <c>Free</c> rule, never a missing one.</summary>
    [HttpPost("{editionId:guid}/rates")]
    [RequirePermission(EmsPermissions.Pricing.Draft)]
    public async Task<ActionResult<ApiResponse<RateVersionCreatedDto>>> DraftRates(Guid editionId, [FromBody] DraftRatesRequest request)
    {
        var versionId = await ratePlanService.DraftAsync(User.GetAccountId(), editionId, request.Note, request.Rules!);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<RateVersionCreatedDto>.Ok(new RateVersionCreatedDto { VersionId = versionId }, "Rates drafted."));
    }

    /// <summary>Approval needs a different person from the drafter.</summary>
    [HttpPost("{editionId:guid}/rates/{versionId:guid}/approve")]
    [RequirePermission(EmsPermissions.Pricing.Approve)]
    public async Task<ActionResult<ApiResponse>> ApproveRates(Guid editionId, Guid versionId, [FromBody] RateNoteRequest request)
    {
        await ratePlanService.ApproveAsync(User.GetAccountId(), editionId, versionId, request.Note);
        return Ok(ApiResponse.Ok("Rates approved."));
    }

    [HttpPost("{editionId:guid}/rates/{versionId:guid}/publish")]
    [RequirePermission(EmsPermissions.Pricing.Approve)]
    public async Task<ActionResult<ApiResponse>> PublishRates(Guid editionId, Guid versionId, [FromBody] RateNoteRequest request)
    {
        await ratePlanService.PublishAsync(User.GetAccountId(), editionId, versionId, request.Note);
        return Ok(ApiResponse.Ok("Rates published."));
    }
}

/// <summary>What the public sees. Only editions whose visibility is Public; never internal ids or camp capacity.</summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/public/edition")]
public class PublicEditionController(IEditionService editionService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PublicEditionDto>>>> List()
        => Ok(ApiResponse<IReadOnlyList<PublicEditionDto>>.Ok(await editionService.ListPublicAsync()));

    [HttpGet("{slug}")]
    public async Task<ActionResult<ApiResponse<PublicEditionDto>>> Get(string slug)
        => await editionService.GetPublicAsync(slug) is { } edition
            ? Ok(ApiResponse<PublicEditionDto>.Ok(edition))
            : NotFound(ApiResponse<PublicEditionDto>.Fail("Not found."));
}
