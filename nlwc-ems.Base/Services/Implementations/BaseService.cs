using System.Linq.Expressions;
using nlwc.ems.Base.Core;
using nlwc.ems.Base.Core.Exceptions;
using nlwc.ems.Base.Core.Paging;
using nlwc.ems.Base.Data;
using nlwc.ems.Base.Services.Interfaces;

namespace nlwc.ems.Base.Services.Implementations;

public abstract class BaseService<T>(IBaseDao<T> dao) : IBaseService<T> where T : BaseEntity
{
    protected IBaseDao<T> Dao { get; } = dao;

    public virtual Task<PagerEntity<T>> RetrievePagedAsync(int page, int max, Expression<Func<T, bool>>? predicate = null)
        => Dao.RetrievePagedAsync(page, max, predicate);

    public virtual Task<IEnumerable<T>> RetrieveAllAsync() => Dao.RetrieveAllAsync();

    public virtual Task<T?> RetrieveByIdAsync(Guid id) => Dao.RetrieveByIdAsync(id);

    public virtual Task<IEnumerable<T>> RetrieveByIdsAsync(IEnumerable<Guid> ids) => Dao.RetrieveByIdsAsync(ids);

    public virtual async Task<ServiceResponse<T>> InsertAsync(T item)
    {
        var preSave = await PreSaveAsync(item);
        if (!preSave.IsSuccessful) return preSave;

        var inserted = await Dao.InsertAsync(preSave.Value!);
        return ServiceResponse.Success(inserted, preSave.Message);
    }

    public virtual async Task<ServiceResponse<T>> UpdateAsync(T item)
    {
        var preUpdate = await PreUpdateAsync(item);
        if (!preUpdate.IsSuccessful) return preUpdate;

        var updated = await Dao.UpdateAsync(preUpdate.Value!);
        return ServiceResponse.Success(updated, preUpdate.Message);
    }

    public virtual Task<ServiceResponse<T>> PreSaveAsync(T item) => Task.FromResult(ServiceResponse.Success(item));

    public virtual Task<ServiceResponse<T>> PreUpdateAsync(T item) => Task.FromResult(ServiceResponse.Success(item));

    public virtual Task RemoveAsync(T item) => Dao.RemoveAsync(item);

    /// <summary>For internal call sites (a status flip, a domain action) that want the full
    /// <see cref="InsertAsync"/> pipeline but have no structured response to report through: a failure is unexpected.</summary>
    protected async Task<T> InsertOrThrowAsync(T item)
    {
        var result = await InsertAsync(item);
        if (!result.IsSuccessful) throw new ValidationException([result.Message ?? "Insert failed."]);
        return result.Value!;
    }

    /// <summary>Update-side counterpart to <see cref="InsertOrThrowAsync"/>.</summary>
    protected async Task<T> UpdateOrThrowAsync(T item)
    {
        var result = await UpdateAsync(item);
        if (!result.IsSuccessful) throw new ValidationException([result.Message ?? "Update failed."]);
        return result.Value!;
    }
}
