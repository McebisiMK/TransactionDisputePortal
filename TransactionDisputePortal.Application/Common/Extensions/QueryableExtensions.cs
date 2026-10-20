using TransactionDisputePortal.Application.Common.Models;

namespace TransactionDisputePortal.Application.Common.Extensions;

public static class QueryableExtensions
{
    public static Task<PaginatedList<T>> ToPaginatedListAsync<T>(this IQueryable<T> source, PagedQuery query, CancellationToken cancellationToken)
    {
        return PaginatedList<T>.CreateAsync(source, query.PageNumber, query.PageSize, cancellationToken);
    }
}
