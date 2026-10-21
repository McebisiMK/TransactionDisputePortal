using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Features.Transactions.Models;

namespace TransactionDisputePortal.Application.Features.Transactions.Queries.GetTransactionById;

public record GetTransactionByIdQuery(Guid Id) : IRequest<TransactionDTO>
{
    public class GetTransactionByIdQueryHandler(IMapper mapper, IApplicationDbContext applicationDbContext, ICurrentCustomerService currentCustomerService, TimeProvider timeProvider)
        : IRequestHandler<GetTransactionByIdQuery, TransactionDTO>
    {
        private readonly IMapper mapper = mapper;
        private readonly TimeProvider timeProvider = timeProvider;
        private readonly IApplicationDbContext applicationDbContext = applicationDbContext;
        private readonly ICurrentCustomerService currentCustomerService = currentCustomerService;

        public async Task<TransactionDTO> Handle(GetTransactionByIdQuery request, CancellationToken cancellationToken)
        {
            var customerId = await currentCustomerService.GetCustomerIdAsync(cancellationToken);

            var transaction = await applicationDbContext.Transactions
                                    .Where(transaction => transaction.Id == request.Id && transaction.Account.CustomerId == customerId)
                                    .ProjectTo<TransactionDTO>(mapper.ConfigurationProvider, TransactionDTO.ProjectionParameters(timeProvider.GetUtcNow().UtcDateTime))
                                    .FirstAsync(cancellationToken);

            return transaction;
        }
    }
}
