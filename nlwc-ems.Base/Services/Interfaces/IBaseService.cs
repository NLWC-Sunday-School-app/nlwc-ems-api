using System.Linq.Expressions;
using nlwc.ems.Base.Core;
using nlwc.ems.Base.Core.Paging;

namespace nlwc.ems.Base.Services.Interfaces;

public interface IBaseService<T> where T : BaseEntity
{
    Task<PagerEntity<T>> RetrievePagedAsync(int page, int max, Expression<Func<T, bool>>? predicate = null);
    Task<IEnumerable<T>> RetrieveAllAsync();
    Task<T?> RetrieveByIdAsync(Guid id);
    Task<IEnumerable<T>> RetrieveByIdsAsync(IEnumerable<Guid> ids);
    Task<ServiceResponse<T>> InsertAsync(T item);
    Task<ServiceResponse<T>> UpdateAsync(T item);

    /// <summary>Runs before <see cref="InsertAsync"/> persists: the create-side hook for business-rule checks
    /// (uniqueness, referenced-entity existence) that are expected outcomes, not exceptions.</summary>
    Task<ServiceResponse<T>> PreSaveAsync(T item);

    /// <summary>Update-side counterpart to <see cref="PreSaveAsync"/>. Independent, not a call-through: update and
    /// insert enforce different rules ("unique among other records" vs "unique outright").</summary>
    Task<ServiceResponse<T>> PreUpdateAsync(T item);

    Task RemoveAsync(T item);
}
