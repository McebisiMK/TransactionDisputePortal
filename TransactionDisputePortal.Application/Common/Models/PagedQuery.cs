namespace TransactionDisputePortal.Application.Common.Models;

public abstract record PagedQuery
{
    public const int MaxPageSize = 100;
    public const int DefaultPageSize = 20;

    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = DefaultPageSize;
}
