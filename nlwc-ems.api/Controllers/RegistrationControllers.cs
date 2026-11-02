using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nlwc.ems.Core.Models;
using nlwc.ems.Core.Security;
using nlwc.ems.Dtos;

namespace nlwc.ems.Controllers;

/// <summary>A family's registrations. Everything is scoped to registrations the caller made or is the subject of.</summary>
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class RegistrationController(IRegistrationService registrationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RegistrationDto>>>> List()
        => Ok(ApiResponse<IReadOnlyList<RegistrationDto>>.Ok(await registrationService.ListAsync(User.GetPersonId())));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RegistrationDto>>> Get(Guid id)
        => Ok(ApiResponse<RegistrationDto>.Ok(await registrationService.GetAsync(User.GetPersonId(), id)));

    /// <summary>Starts a draft for yourself or one of your children. Refused until the person has a date of birth.</summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<RegistrationDto>>> Create([FromBody] CreateRegistrationRequest request)
        => StatusCode(StatusCodes.Status201Created,
            ApiResponse<RegistrationDto>.Ok(await registrationService.CreateAsync(User.GetPersonId(), request), "Registration started."));

    [HttpPut("{id:guid}/draft")]
    public async Task<ActionResult<ApiResponse>> SaveDraft(Guid id, [FromBody] SaveDraftRequest request)
    {
        await registrationService.SaveDraftAsync(User.GetPersonId(), id, request);
        return Ok(ApiResponse.Ok("Draft saved."));
    }

    /// <summary>What is still open, computed by the API so the wizard never decides it.</summary>
    [HttpGet("{id:guid}/review")]
    public async Task<ActionResult<ApiResponse<ReviewDto>>> Review(Guid id)
        => Ok(ApiResponse<ReviewDto>.Ok(await registrationService.ReviewAsync(User.GetPersonId(), id)));

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<ApiResponse<ReviewDto>>> Submit(Guid id)
        => Ok(ApiResponse<ReviewDto>.Ok(await registrationService.SubmitAsync(User.GetPersonId(), id), "Registration submitted."));

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse>> Cancel(Guid id, [FromBody] ReasonRequest request)
    {
        await registrationService.CancelAsync(User.GetPersonId(), id, request.Reason);
        return Ok(ApiResponse.Ok("Registration cancelled."));
    }
}

/// <summary>Per-registration permissions. Each answer is a new record; "unanswered" and "No" are different things.</summary>
[ApiController]
[Authorize]
[Route("api/v1/registration/{registrationId:guid}/consent")]
public class ConsentController(IConsentService consentService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ConsentDto>>>> List(Guid registrationId)
        => Ok(ApiResponse<IReadOnlyList<ConsentDto>>.Ok(await consentService.ListAsync(User.GetPersonId(), registrationId)));

    [HttpPut("{kind}")]
    public async Task<ActionResult<ApiResponse<ConsentDto>>> Record(Guid registrationId, ConsentKind kind, [FromBody] ConsentRequest request)
        => Ok(ApiResponse<ConsentDto>.Ok(await consentService.RecordAsync(User.GetPersonId(), registrationId, kind, request.Accepted!.Value), "Answer recorded."));
}

/// <summary>Restricted health answers for one registration. Readable by the family; staff access comes with the admin console.</summary>
[ApiController]
[Authorize]
[Route("api/v1/registration/{registrationId:guid}/health")]
public class HealthController(IHealthService healthService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<HealthDto?>>> Get(Guid registrationId)
        => Ok(ApiResponse<HealthDto?>.Ok(await healthService.GetAsync(User.GetPersonId(), registrationId)));

    [HttpPut]
    public async Task<ActionResult> Set(Guid registrationId, [FromBody] HealthRequest request)
    {
        await healthService.SetAsync(User.GetPersonId(), registrationId, request);
        return Ok(ApiResponse.Ok("Health answers saved."));
    }
}

/// <summary>Staff views. Permissions are edition-scoped and checked here, against the registration's own edition.</summary>
[ApiController]
[Authorize]
[Route("api/v1/admin/registration")]
public class StaffRegistrationController(IStaffRegistrationService staffService) : ControllerBase
{
    private const int MaxPageSize = 100;

    private ObjectResult Denied(string code, Guid editionId)
        => StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("You do not have permission to view this.", User.DenialCode([code], editionId)));

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagerEntity<StaffRegistrationDto>>>> List(
        [FromQuery] Guid editionId, [FromQuery] RegistrationState? state, [FromQuery] ParticipationMode? mode, [FromQuery] string? ageBand,
        [FromQuery] string? q, [FromQuery] int page = 0, [FromQuery] int max = 50)
    {
        if (!User.HasPermissionWithMfa(EmsPermissions.Registrations.View, editionId)) return Denied(EmsPermissions.Registrations.View, editionId);
        var flags = User.HasPermissionWithMfa(EmsPermissions.Health.Flags, editionId);
        var filter = new StaffRegistrationFilter { State = state, Mode = mode, AgeBand = string.IsNullOrWhiteSpace(ageBand) ? null : ageBand, Text = q };
        return Ok(ApiResponse<PagerEntity<StaffRegistrationDto>>.Ok(
            await staffService.ListAsync(editionId, filter, Math.Max(page, 0), Math.Clamp(max, 1, MaxPageSize), flags)));
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<StaffDashboardDto>>> Dashboard([FromQuery] Guid editionId)
    {
        if (!User.HasPermissionWithMfa(EmsPermissions.Registrations.View, editionId)) return Denied(EmsPermissions.Registrations.View, editionId);
        return Ok(ApiResponse<StaffDashboardDto>.Ok(await staffService.GetDashboardAsync(editionId)));
    }

    /// <summary>An audited export of the filtered list: needs <c>registrations:view</c> and a reason.</summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] Guid editionId, [FromQuery] RegistrationState? state, [FromQuery] ParticipationMode? mode,
        [FromQuery] string? ageBand, [FromQuery] string? q, [FromQuery] string? reason)
    {
        if (!User.HasPermissionWithMfa(EmsPermissions.Registrations.View, editionId)) return Denied(EmsPermissions.Registrations.View, editionId);
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 500)
            throw new ValidationException(["Say why you are exporting this list (up to 500 characters)."], "reason_required");
        var filter = new StaffRegistrationFilter { State = state, Mode = mode, AgeBand = string.IsNullOrWhiteSpace(ageBand) ? null : ageBand, Text = q };
        var csv = await staffService.ExportCsvAsync(editionId, filter, User.HasPermissionWithMfa(EmsPermissions.Health.Flags, editionId), reason);
        return File(System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(csv)).ToArray(), "text/csv", $"registrations-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv");
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<StaffRegistrationDetailDto>>> Detail(Guid id)
    {
        var editionId = await staffService.GetEditionIdAsync(id);
        if (!User.HasPermissionWithMfa(EmsPermissions.Registrations.View, editionId)) return Denied(EmsPermissions.Registrations.View, editionId);
        var flags = User.HasPermissionWithMfa(EmsPermissions.Health.Flags, editionId);
        return Ok(ApiResponse<StaffRegistrationDetailDto>.Ok(await staffService.GetDetailAsync(id, flags)));
    }

    /// <summary>Staff cancel on someone's behalf. Needs <c>registrations:edit</c> and a reason, which is audited.</summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse>> Cancel(Guid id, [FromBody] ReasonRequest request)
    {
        var editionId = await staffService.GetEditionIdAsync(id);
        if (!User.HasPermissionWithMfa(EmsPermissions.Registrations.Edit, editionId)) return Denied(EmsPermissions.Registrations.Edit, editionId);
        await staffService.CancelAsync(User.GetPersonId(), id, request.Reason);
        return Ok(ApiResponse.Ok("Registration cancelled."));
    }

    /// <summary>A deliberate reveal: needs <c>health:view</c>, a reason, and leaves an access-log row.</summary>
    [HttpGet("{id:guid}/health")]
    public async Task<ActionResult<ApiResponse<HealthDto?>>> Health(Guid id, [FromQuery] string? reason)
    {
        var editionId = await staffService.GetEditionIdAsync(id);
        if (!User.HasPermissionWithMfa(EmsPermissions.Health.View, editionId)) return Denied(EmsPermissions.Health.View, editionId);
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 500)
            throw new ValidationException(["Say why you are opening this record (up to 500 characters)."], "reason_required");
        return Ok(ApiResponse<HealthDto?>.Ok(await staffService.RevealHealthAsync(User.GetAccountId(), id, reason)));
    }
}
