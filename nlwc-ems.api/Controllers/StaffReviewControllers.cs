using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nlwc.ems.Core.Models;
using nlwc.ems.Core.Security;
using nlwc.ems.Dtos;

namespace nlwc.ems.Controllers;

/// <summary>Who people are: search, relatives, registrations, look-alikes and spouse links. Church-wide, so the permission is not edition-scoped.</summary>
[ApiController]
[Authorize]
[Route("api/v1/admin/person")]
public class StaffPeopleController(IStaffPeopleService peopleService) : ControllerBase
{
    private const int MaxPageSize = 100;

    private ObjectResult Denied(string code)
        => StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("You do not have permission to do this.", User.DenialCode([code])));

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagerEntity<StaffPersonSummaryDto>>>> Search([FromQuery] string? q, [FromQuery] int page = 0, [FromQuery] int max = 25)
    {
        if (!User.HasPermissionWithMfa(EmsPermissions.People.View)) return Denied(EmsPermissions.People.View);
        return Ok(ApiResponse<PagerEntity<StaffPersonSummaryDto>>.Ok(await peopleService.SearchAsync(q, Math.Max(page, 0), Math.Clamp(max, 1, MaxPageSize))));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<StaffPersonDetailDto>>> Detail(Guid id)
    {
        if (!User.HasPermissionWithMfa(EmsPermissions.People.View)) return Denied(EmsPermissions.People.View);
        return Ok(ApiResponse<StaffPersonDetailDto>.Ok(await peopleService.GetDetailAsync(id)));
    }

    [HttpGet("duplicates")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DuplicatePairDto>>>> Duplicates()
    {
        if (!User.HasPermissionWithMfa(EmsPermissions.People.View)) return Denied(EmsPermissions.People.View);
        return Ok(ApiResponse<IReadOnlyList<DuplicatePairDto>>.Ok(await peopleService.GetDuplicatesAsync()));
    }

    /// <summary>"These two are different people." Nothing is merged; the pair just stops being offered.</summary>
    [HttpPost("duplicates/dismiss")]
    public async Task<ActionResult<ApiResponse>> Dismiss([FromBody] DismissDuplicateRequest request)
    {
        if (!User.HasPermissionWithMfa(EmsPermissions.People.Edit)) return Denied(EmsPermissions.People.Edit);
        await peopleService.DismissDuplicateAsync(User.GetPersonId(), request.PersonAId!.Value, request.PersonBId!.Value, request.Reason);
        return Ok(ApiResponse.Ok("Marked as different people."));
    }

    [HttpPost("{id:guid}/spouse")]
    public async Task<ActionResult<ApiResponse>> Pair(Guid id, [FromBody] PairSpouseRequest request)
    {
        if (!User.HasPermissionWithMfa(EmsPermissions.People.Edit)) return Denied(EmsPermissions.People.Edit);
        await peopleService.PairSpousesAsync(User.GetPersonId(), id, request.SpousePersonId!.Value, request.Reason);
        return Ok(ApiResponse.Ok("Spouses paired."));
    }

    [HttpPost("{id:guid}/spouse/remove")]
    public async Task<ActionResult<ApiResponse>> Unpair(Guid id, [FromBody] ReasonRequest request)
    {
        if (!User.HasPermissionWithMfa(EmsPermissions.People.Edit)) return Denied(EmsPermissions.People.Edit);
        await peopleService.UnpairSpousesAsync(User.GetPersonId(), id, request.Reason);
        return Ok(ApiResponse.Ok("Spouse link removed."));
    }
}

/// <summary>Camp is a request a person decides on. The system never approves or assigns anything by itself.</summary>
[ApiController]
[Authorize]
[Route("api/v1/admin/camp")]
public class StaffCampController(ICampReviewService campService, IStaffRegistrationService registrationService) : ControllerBase
{
    private ObjectResult Denied(string code, Guid editionId)
        => StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("You do not have permission to do this.", User.DenialCode([code], editionId)));

    [HttpGet]
    public async Task<ActionResult<ApiResponse<CampQueueDto>>> Queue([FromQuery] Guid editionId)
    {
        if (!User.HasPermissionWithMfa(EmsPermissions.Registrations.View, editionId)) return Denied(EmsPermissions.Registrations.View, editionId);
        return Ok(ApiResponse<CampQueueDto>.Ok(await campService.GetQueueAsync(editionId, User.HasPermissionWithMfa(EmsPermissions.Health.Flags, editionId))));
    }

    [HttpPost("{registrationId:guid}/decision")]
    public async Task<ActionResult<ApiResponse>> Decide(Guid registrationId, [FromBody] CampDecisionRequest request)
    {
        var editionId = await registrationService.GetEditionIdAsync(registrationId);
        if (!User.HasPermissionWithMfa(EmsPermissions.Registrations.Edit, editionId)) return Denied(EmsPermissions.Registrations.Edit, editionId);
        await campService.DecideAsync(User.GetPersonId(), registrationId, request.Decision!.Value, request.Reason);
        return Ok(ApiResponse.Ok("Decision recorded."));
    }
}
