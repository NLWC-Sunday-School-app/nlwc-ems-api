using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using nlwc.ems.Base.Core;
using nlwc.ems.Base.Core.Paging;
using nlwc.ems.Base.Core.Security;
using nlwc.ems.Base.Services;
using nlwc.ems.Base.Services.Interfaces;
using nlwc.ems.Base.Web.Common;

namespace nlwc.ems.Base.Web.Controllers;

internal static class EntityNameFormatter
{
    /// <summary><c>"EventEdition"</c> becomes <c>"Event edition"</c>.</summary>
    public static string Humanize(string pascalCaseName)
    {
        var builder = new StringBuilder(pascalCaseName.Length + 4);
        for (var i = 0; i < pascalCaseName.Length; i++)
        {
            var c = pascalCaseName[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(pascalCaseName[i - 1])) builder.Append(' ');
            builder.Append(i == 0 ? c : char.ToLowerInvariant(c));
        }
        return builder.ToString();
    }
}

[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
public abstract class BaseApiController<TService, TEntity, TFilter>(TService service) : ControllerBase
    where TService : IBaseService<TEntity>
    where TEntity : BaseEntity, IFilterable<TEntity, TFilter>
    where TFilter : class
{
    protected TService Service { get; } = service;

    protected const int MaxPageSize = 100;

    /// <summary>Permission code (<c>resource:level</c>) required to enable/disable, or null for no extra gate.
    /// Enable and Disable share one code: flipping status either way is one capability.</summary>
    protected virtual string? StatusPermissionCode => null;

    protected virtual string? DeletePermissionCode => null;

    /// <summary>
    /// Codes that gate the read actions. Empty (the default) leaves reads open to any authenticated caller; a caller
    /// passes with <em>any one</em> of the codes (an OR), which lets reference data be read from more than one screen.
    /// </summary>
    protected virtual IReadOnlyCollection<string> ViewPermissionCodes => [];

    /// <summary>The edition a by-id action is scoped to, so an edition-scoped grant is honoured. Null (the default)
    /// means only church-wide grants count.</summary>
    protected virtual Guid? PermissionScope(Guid? routeId) => null;

    /// <summary>Null when <see cref="ViewPermissionCodes"/> is empty or the caller holds one of them on an MFA-verified
    /// session; otherwise the 403 to return. Every read action starts with
    /// <c>if (AuthorizeView() is { } forbidden) return forbidden;</c></summary>
    protected ObjectResult? AuthorizeView(Guid? scope = null)
    {
        var codes = ViewPermissionCodes;
        if (codes.Count == 0 || codes.Any(c => User.HasPermissionWithMfa(c, scope))) return null;
        return Forbidden("You do not have permission to view this resource.", codes, scope);
    }

    /// <summary>Null when <paramref name="requiredCode"/> is null (ungated) or the caller holds it; otherwise the 403.</summary>
    protected ActionResult<ApiResponse<TEntity>>? AuthorizePermission(string? requiredCode, Guid? scope = null)
    {
        if (requiredCode is null || User.HasPermissionWithMfa(requiredCode, scope)) return null;
        return Forbidden("You do not have permission to perform this action.", [requiredCode], scope);
    }

    /// <summary><see cref="AuthorizePermission"/> for actions that answer with the non-generic <see cref="ApiResponse"/>.</summary>
    protected ActionResult<ApiResponse>? AuthorizePermissionPlain(string? requiredCode, Guid? scope = null)
    {
        if (requiredCode is null || User.HasPermissionWithMfa(requiredCode, scope)) return null;
        return Forbidden("You do not have permission to perform this action.", [requiredCode], scope);
    }

    private ObjectResult Forbidden(string message, IEnumerable<string> codes, Guid? scope) =>
        StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail(message, User.DenialCode(codes, scope)));

    [HttpGet("{id:guid}")]
    public virtual async Task<ActionResult<ApiResponse<TEntity>>> RetrieveById(Guid id)
    {
        if (AuthorizeView(PermissionScope(id)) is { } forbidden) return forbidden;

        var entity = await Service.RetrieveByIdAsync(id);
        return entity is null
            ? NotFound(ApiResponse<TEntity>.Fail("Not found."))
            : Ok(ApiResponse<TEntity>.Ok(entity));
    }

    [HttpGet]
    public virtual async Task<ActionResult<ApiResponse<IEnumerable<TEntity>>>> RetrieveAll()
    {
        if (AuthorizeView() is { } forbidden) return forbidden;

        return Ok(ApiResponse<IEnumerable<TEntity>>.Ok(await Service.RetrieveAllAsync()));
    }

    [HttpPost("search")]
    public virtual async Task<ActionResult<ApiResponse<PagerEntity<TEntity>>>> Search(
        [FromBody] TFilter? filter, [FromQuery] int page = 0, [FromQuery] int max = 20)
    {
        if (AuthorizeView() is { } forbidden) return forbidden;

        var result = await Service.RetrievePagedAsync(Math.Max(page, 0), Math.Clamp(max, 1, MaxPageSize), TEntity.BuildPredicate(filter));
        return Ok(ApiResponse<PagerEntity<TEntity>>.Ok(result));
    }

    [HttpGet("search/{searchText}")]
    public virtual async Task<ActionResult<ApiResponse<PagerEntity<TEntity>>>> SearchByText(
        string searchText, [FromQuery] int page = 0, [FromQuery] int max = 20)
    {
        if (AuthorizeView() is { } forbidden) return forbidden;

        var result = await Service.RetrievePagedAsync(Math.Max(page, 0), Math.Clamp(max, 1, MaxPageSize), TEntity.BuildPredicate(searchText));
        return Ok(ApiResponse<PagerEntity<TEntity>>.Ok(result));
    }

    [HttpDelete("{id:guid}")]
    public virtual async Task<ActionResult<ApiResponse>> Remove(Guid id)
    {
        if (AuthorizePermissionPlain(DeletePermissionCode, PermissionScope(id)) is { } forbidden) return forbidden;

        var entity = await Service.RetrieveByIdAsync(id);
        if (entity is null) return NotFound(ApiResponse.Fail("Not found."));

        await Service.RemoveAsync(entity);
        return Ok(ApiResponse.Ok("Deleted successfully."));
    }

    [HttpPatch("{id:guid}/enable")]
    public virtual async Task<ActionResult<ApiResponse<TEntity>>> Enable(Guid id)
    {
        if (AuthorizePermission(StatusPermissionCode, PermissionScope(id)) is { } forbidden) return forbidden;
        return await SetStatus(id, BaseStatus.Active);
    }

    [HttpPatch("{id:guid}/disable")]
    public virtual async Task<ActionResult<ApiResponse<TEntity>>> Disable(Guid id)
    {
        if (AuthorizePermission(StatusPermissionCode, PermissionScope(id)) is { } forbidden) return forbidden;
        return await SetStatus(id, BaseStatus.Inactive);
    }

    private async Task<ActionResult<ApiResponse<TEntity>>> SetStatus(Guid id, BaseStatus status)
    {
        var entity = await Service.RetrieveByIdAsync(id);
        if (entity is null) return NotFound(ApiResponse<TEntity>.Fail("Not found."));

        entity.Status = status;
        var result = await Service.UpdateAsync(entity);
        return result.IsSuccessful
            ? Ok(ApiResponse<TEntity>.Ok(result.Value!, result.Message))
            : BadRequest(ApiResponse<TEntity>.Fail(result.Message ?? "Update failed."));
    }
}

/// <summary>
/// Adds generic create/update on top of <see cref="BaseApiController{TService,TEntity,TFilter}"/>. The DTO-to-entity
/// mapping is the only entity-specific part; the 200/400/404 branching around it lives here once.
/// </summary>
public abstract class BaseApiController<TService, TEntity, TFilter, TCreateRequest, TUpdateRequest>(TService service)
    : BaseApiController<TService, TEntity, TFilter>(service)
    where TService : IBaseService<TEntity>
    where TEntity : BaseEntity, IFilterable<TEntity, TFilter>
    where TFilter : class
{
    /// <summary>Noun in the default "{EntityName} created."/"updated." messages ("EventEdition" becomes "Event edition").</summary>
    protected virtual string EntityName => EntityNameFormatter.Humanize(typeof(TEntity).Name);

    protected virtual string? CreatePermissionCode => null;
    protected virtual string? UpdatePermissionCode => null;

    [HttpPost]
    public virtual async Task<ActionResult<ApiResponse<TEntity>>> Create([FromBody] TCreateRequest request)
    {
        if (AuthorizePermission(CreatePermissionCode) is { } forbidden) return forbidden;

        var entity = await MapToNewAsync(request);
        entity.Id = Guid.NewGuid();

        var result = await InsertAsync(entity, request);
        if (!result.IsSuccessful) return BadRequest(ApiResponse<TEntity>.Fail(result.Message ?? "Creation failed."));

        var response = await AfterCreateAsync(result.Value!, request) ?? result.Value!;
        return Ok(ApiResponse<TEntity>.Ok(response, result.Message ?? $"{EntityName} created."));
    }

    [HttpPut("{id:guid}")]
    public virtual async Task<ActionResult<ApiResponse<TEntity>>> Update(Guid id, [FromBody] TUpdateRequest request)
    {
        if (AuthorizePermission(UpdatePermissionCode, PermissionScope(id)) is { } forbidden) return forbidden;

        var entity = await Service.RetrieveByIdAsync(id);
        if (entity is null) return NotFound(ApiResponse<TEntity>.Fail("Not found."));

        await ApplyUpdateAsync(entity, request);
        var result = await Service.UpdateAsync(entity);
        if (!result.IsSuccessful) return BadRequest(ApiResponse<TEntity>.Fail(result.Message ?? "Update failed."));

        var response = await AfterUpdateAsync(result.Value!, request) ?? result.Value!;
        return Ok(ApiResponse<TEntity>.Ok(response, result.Message ?? $"{EntityName} updated."));
    }

    /// <summary>Builds a new, unsaved entity from a create request (its <c>Id</c> is overwritten by <see cref="Create"/>).</summary>
    protected abstract Task<TEntity> MapToNewAsync(TCreateRequest request);

    /// <summary>Persists the newly-mapped entity. Defaults to the plain insert; override when creation needs a
    /// different service call (e.g. an edition that creates its days and capabilities with it).</summary>
    protected virtual Task<ServiceResponse<TEntity>> InsertAsync(TEntity entity, TCreateRequest request) => Service.InsertAsync(entity);

    /// <summary>Applies an update request onto an already-retrieved entity, in place.</summary>
    protected abstract Task ApplyUpdateAsync(TEntity entity, TUpdateRequest request);

    protected virtual Task<TEntity?> AfterCreateAsync(TEntity created, TCreateRequest request) => Task.FromResult<TEntity?>(null);

    protected virtual Task<TEntity?> AfterUpdateAsync(TEntity updated, TUpdateRequest request) => Task.FromResult<TEntity?>(null);
}
