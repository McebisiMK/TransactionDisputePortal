using System.Linq.Expressions;
using TransactionDisputePortal.Application.Common.Models;

namespace TransactionDisputePortal.Application.Common.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> WhereIf<T>(this IQueryable<T> source, bool condition, Expression<Func<T, bool>> predicate)
    {
        return condition ? source.Where(predicate) : source;
    }

    public static Task<PaginatedList<T>> ToPaginatedListAsync<T>(this IQueryable<T> source, PagedQuery query, CancellationToken cancellationToken)
    {
        return PaginatedList<T>.CreateAsync(source, query.PageNumber, query.PageSize, cancellationToken);
    }
}
