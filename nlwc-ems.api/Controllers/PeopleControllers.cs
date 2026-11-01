using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nlwc.ems.Dtos;

namespace nlwc.ems.Controllers;

/// <summary>The caller's own profile. Everything here is scoped to the signed-in person.</summary>
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class ProfileController(IProfileService profileService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<ProfileDto>>> Get()
        => Ok(ApiResponse<ProfileDto>.Ok(await profileService.GetAsync(User.GetAccountId(), User.GetPersonId(), User.IsEmailVerified())));

    [HttpPut]
    public async Task<ActionResult<ApiResponse>> Update([FromBody] UpdateProfileRequest request)
    {
        await profileService.UpdateAsync(User.GetAccountId(), User.GetPersonId(), request);
        return Ok(ApiResponse.Ok("Profile updated."));
    }
}

/// <summary>Minors (ages 0–17) the caller is guardian of. Another guardian's child answers 404, never 403.</summary>
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class DependantController(IDependantService dependantService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DependantDto>>>> List()
        => Ok(ApiResponse<IReadOnlyList<DependantDto>>.Ok(await dependantService.ListAsync(User.GetPersonId())));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CreatedDto>>> Add([FromBody] AddDependantRequest request)
    {
        var id = await dependantService.AddAsync(User.GetPersonId(), request);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<CreatedDto>.Ok(new CreatedDto { Id = id }, "Child added."));
    }

    /// <summary>Ends the caller's guardianship. Needs a reason; the record is kept.</summary>
    [HttpPost("{id:guid}/remove")]
    public async Task<ActionResult<ApiResponse>> Remove(Guid id, [FromBody] ReasonRequest request)
    {
        await dependantService.RemoveAsync(User.GetPersonId(), id, request.Reason);
        return Ok(ApiResponse.Ok("Child removed."));
    }
}

/// <summary>Adults named to collect a child, with optional specific days.</summary>
[ApiController]
[Authorize]
[Route("api/v1/dependant/{childId:guid}/collector")]
public class CollectorController(ICollectorService collectorService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CollectorDto>>>> List(Guid childId)
        => Ok(ApiResponse<IReadOnlyList<CollectorDto>>.Ok(await collectorService.ListAsync(User.GetPersonId(), childId)));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CreatedDto>>> Add(Guid childId, [FromBody] AddCollectorRequest request)
    {
        var id = await collectorService.AddAsync(User.GetPersonId(), childId, request);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<CreatedDto>.Ok(new CreatedDto { Id = id }, "Collector added."));
    }

    [HttpDelete("{collectorId:guid}")]
    public async Task<ActionResult<ApiResponse>> Remove(Guid childId, Guid collectorId)
    {
        await collectorService.RemoveAsync(User.GetPersonId(), childId, collectorId);
        return Ok(ApiResponse.Ok("Collector removed."));
    }
}

/// <summary>One emergency contact for yourself, and one for each child you are guardian of.</summary>
[ApiController]
[Authorize]
public class EmergencyContactController(IEmergencyContactService contactService) : ControllerBase
{
    [HttpGet("~/api/v1/emergency-contact")]
    public async Task<ActionResult<ApiResponse<ContactDto?>>> GetMine()
        => Ok(ApiResponse<ContactDto?>.Ok(await contactService.GetAsync(User.GetPersonId(), User.GetPersonId())));

    [HttpPut("~/api/v1/emergency-contact")]
    public async Task<ActionResult<ApiResponse>> SetMine([FromBody] EmergencyContactRequest request)
    {
        await contactService.SetAsync(User.GetPersonId(), User.GetPersonId(), request);
        return Ok(ApiResponse.Ok("Emergency contact saved."));
    }

    [HttpGet("~/api/v1/dependant/{dependantId:guid}/emergency-contact")]
    public async Task<ActionResult<ApiResponse<ContactDto?>>> GetForDependant(Guid dependantId)
        => Ok(ApiResponse<ContactDto?>.Ok(await contactService.GetAsync(User.GetPersonId(), dependantId)));

    [HttpPut("~/api/v1/dependant/{dependantId:guid}/emergency-contact")]
    public async Task<ActionResult<ApiResponse>> SetForDependant(Guid dependantId, [FromBody] EmergencyContactRequest request)
    {
        await contactService.SetAsync(User.GetPersonId(), dependantId, request);
        return Ok(ApiResponse.Ok("Emergency contact saved."));
    }
}
