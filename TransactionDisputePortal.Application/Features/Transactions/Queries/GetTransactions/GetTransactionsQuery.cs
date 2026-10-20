using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using TransactionDisputePortal.Application.Common.Extensions;
using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Common.Models;
using TransactionDisputePortal.Application.Features.Transactions.Models;
using TransactionDisputePortal.Domain.Enums;

namespace TransactionDisputePortal.Application.Features.Transactions.Queries.GetTransactions;

public record GetTransactionsQuery : PagedQuery, IRequest<PaginatedList<TransactionDTO>>
{
    public const int SearchMaxLength = 100;

    public DateOnly? FromDate { get; init; }

    public DateOnly? ToDate { get; init; }

    public string? Search { get; init; }

    public TransactionStatus? Status { get; init; }

    public Guid? AccountId { get; init; }

    public class GetTransactionsQueryHandler(IMapper mapper, IApplicationDbContext applicationDbContext, ICurrentCustomerService currentCustomerService)
        : IRequestHandler<GetTransactionsQuery, PaginatedList<TransactionDTO>>
    {
        private readonly IMapper mapper = mapper;
        private readonly IApplicationDbContext context = applicationDbContext;
        private readonly ICurrentCustomerService currentCustomerService = currentCustomerService;

        public async Task<PaginatedList<TransactionDTO>> Handle(GetTransactionsQuery request, CancellationToken cancellationToken)
        {
            var search = request.Search?.Trim();
            var from = request.FromDate?.ToUtcStartOfBusinessDay();
            var customerId = await currentCustomerService.GetCustomerIdAsync(cancellationToken);
            var toExclusive = request.ToDate?.AddDays(1).ToUtcStartOfBusinessDay();

            var transactions = await context.Transactions
                                      .Where(transaction => transaction.Account.CustomerId == customerId)
                                      .WhereIf(request.AccountId.HasValue, transaction => transaction.AccountId == request.AccountId)
                                      .WhereIf(from.HasValue, transaction => transaction.TransactionDate >= from)
                                      .WhereIf(toExclusive.HasValue, transaction => transaction.TransactionDate < toExclusive)
                                      .WhereIf(request.Status.HasValue, transaction => transaction.Status == request.Status)
                                      .WhereIf(search.IsNotBlank(), transaction => transaction.Merchant.Contains(search!) || transaction.Reference.Contains(search!))
                                      .OrderByDescending(transaction => transaction.TransactionDate)
                                      .ThenByDescending(transaction => transaction.Reference)
                                      .ProjectTo<TransactionDTO>(mapper.ConfigurationProvider)
                                      .ToPaginatedListAsync(request, cancellationToken);

            return transactions;
        }
    }
}
