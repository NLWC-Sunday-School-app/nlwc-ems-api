using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nlwc.ems.Core.Filters;
using nlwc.ems.Core.Models;
using nlwc.ems.Core.Security;
using nlwc.ems.Dtos;

namespace nlwc.ems.Controllers;

/// <summary>The caller's own permissions, so the admin console can build navigation from them. Hiding a link is not
/// security: every endpoint checks again.</summary>
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public class AccessController : ControllerBase
{
    [HttpGet("me")]
    public ActionResult<ApiResponse<AccessMeDto>> Me([FromQuery] Guid? editionId)
    {
        var permissions = User.FindAll(EmsClaimTypes.Permissions).Select(c => c.Value).Select(v =>
        {
            var at = v.IndexOf('@');
            return at < 0 ? new PermissionDto { Code = v } : new PermissionDto { Code = v[..at], EditionId = Guid.Parse(v[(at + 1)..]) };
        })
        // Church-wide permissions always apply; edition-scoped ones only for the edition asked about.
        .Where(p => p.EditionId is null || p.EditionId == editionId)
        .OrderBy(p => p.Code).ToList();

        return Ok(ApiResponse<AccessMeDto>.Ok(new AccessMeDto { MfaVerified = User.IsMfaVerified(), Permissions = permissions }));
    }
}

/// <summary>The role matrix. Read-only: roles are seeded and reviewed, not edited through the API.</summary>
public class RoleController(IRoleService roleService) : BaseApiController<IRoleService, Role, RoleFilter>(roleService)
{
    protected override IReadOnlyCollection<string> ViewPermissionCodes => [EmsPermissions.Staff.View];

    [NonAction]
    public override Task<ActionResult<ApiResponse>> Remove(Guid id) => base.Remove(id);

    [NonAction]
    public override Task<ActionResult<ApiResponse<Role>>> Enable(Guid id) => base.Enable(id);

    [NonAction]
    public override Task<ActionResult<ApiResponse<Role>>> Disable(Guid id) => base.Disable(id);
}

/// <summary>Who holds which role, where, and until when. A grant is revoked, never deleted.</summary>
public class RoleGrantController(IRoleGrantService grantService) : BaseApiController<IRoleGrantService, RoleGrant, RoleGrantFilter>(grantService)
{
    protected override IReadOnlyCollection<string> ViewPermissionCodes => [EmsPermissions.Staff.View];

    [NonAction]
    public override Task<ActionResult<ApiResponse>> Remove(Guid id) => base.Remove(id);

    [NonAction]
    public override Task<ActionResult<ApiResponse<RoleGrant>>> Enable(Guid id) => base.Enable(id);

    [NonAction]
    public override Task<ActionResult<ApiResponse<RoleGrant>>> Disable(Guid id) => base.Disable(id);

    [HttpPost]
    [RequirePermission(EmsPermissions.Staff.Edit)]
    public async Task<ActionResult<ApiResponse<RoleGrant>>> Grant([FromBody] GrantRoleRequest request)
    {
        var grant = await Service.GrantAsync(User.GetAccountId(), request);
        return Ok(ApiResponse<RoleGrant>.Ok(grant, "Role granted."));
    }

    [HttpPost("{id:guid}/revoke")]
    [RequirePermission(EmsPermissions.Staff.Edit)]
    public async Task<ActionResult<ApiResponse>> Revoke(Guid id, [FromBody] ReasonRequest request)
    {
        await Service.RevokeAsync(User.GetAccountId(), id, request.Reason);
        return Ok(ApiResponse.Ok("Role revoked."));
    }
}

[ApiController]
[Route("api/v1/[controller]")]
public class AuditController(IAuditService auditService) : ControllerBase
{
    private const int MaxPageSize = 200;

    [HttpGet]
    [RequirePermission(EmsPermissions.Audit.View)]
    public async Task<ActionResult<ApiResponse<PagerEntity<AuditEvent>>>> List(
        [FromQuery] string? action, [FromQuery] int page = 0, [FromQuery] int max = 50)
        => Ok(ApiResponse<PagerEntity<AuditEvent>>.Ok(await auditService.RetrievePagedAsync(Math.Max(page, 0), Math.Clamp(max, 1, MaxPageSize), action)));

    /// <summary>Recomputes the hash chain. <c>null</c> means intact; otherwise the first row that no longer matches.</summary>
    [HttpGet("verify")]
    [RequirePermission(EmsPermissions.Audit.View)]
    public async Task<ActionResult<ApiResponse<long?>>> Verify()
        => Ok(ApiResponse<long?>.Ok(await auditService.VerifyChainAsync()));
}
