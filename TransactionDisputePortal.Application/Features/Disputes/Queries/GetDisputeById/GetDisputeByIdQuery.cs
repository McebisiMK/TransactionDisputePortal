using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Features.Disputes.Models;

namespace TransactionDisputePortal.Application.Features.Disputes.Queries.GetDisputeById;

public record GetDisputeByIdQuery(Guid Id) : IRequest<DisputeDTO>
{
    public class GetDisputeByIdQueryHandler(IMapper mapper, IApplicationDbContext applicationDbContext, ICurrentCustomerService currentCustomerService)
        : IRequestHandler<GetDisputeByIdQuery, DisputeDTO>
    {
        private readonly IMapper mapper = mapper;
        private readonly IApplicationDbContext applicationDbContext = applicationDbContext;
        private readonly ICurrentCustomerService currentCustomerService = currentCustomerService;

        public async Task<DisputeDTO> Handle(GetDisputeByIdQuery request, CancellationToken cancellationToken)
        {
            var customerId = await currentCustomerService.GetCustomerIdAsync(cancellationToken);

            var dispute = await applicationDbContext.Disputes
                                .Where(dispute => dispute.Id == request.Id && dispute.CustomerId == customerId)
                                .ProjectTo<DisputeDTO>(mapper.ConfigurationProvider)
                                .FirstAsync(cancellationToken);

            return dispute;
        }
    }
}
