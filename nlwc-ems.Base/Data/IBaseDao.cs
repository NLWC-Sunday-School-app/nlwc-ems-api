using System.Linq.Expressions;
using nlwc.ems.Base.Core;
using nlwc.ems.Base.Core.Paging;

namespace nlwc.ems.Base.Data;

public interface IBaseDao<T> where T : BaseEntity
{
    Task<PagerEntity<T>> RetrievePagedAsync(int page, int max, Expression<Func<T, bool>>? predicate = null);
    Task<IEnumerable<T>> RetrieveAllAsync();
    Task<T?> RetrieveByIdAsync(Guid id);
    Task<IEnumerable<T>> RetrieveByIdsAsync(IEnumerable<Guid> ids);
    Task<T> InsertAsync(T item);
    Task<T> UpdateAsync(T item);
    Task RemoveAsync(T item);
}
