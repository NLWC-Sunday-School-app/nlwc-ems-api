using System.Linq.Expressions;

namespace nlwc.ems.Base.Core;

public interface IFilterable<TEntity, TFilter> where TEntity : BaseEntity
{
    static abstract Expression<Func<TEntity, bool>>? BuildPredicate(TFilter? filter);

    static abstract Expression<Func<TEntity, bool>>? BuildPredicate(string searchText);
}
