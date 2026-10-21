using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using TransactionDisputePortal.Application.Common.Extensions;
using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Common.Models;
using TransactionDisputePortal.Application.Features.Disputes.Models;
using TransactionDisputePortal.Domain.Enums;

namespace TransactionDisputePortal.Application.Features.Disputes.Queries.GetDisputes;

public record GetDisputesQuery : PagedQuery, IRequest<PaginatedList<DisputeSummaryDTO>>
{
    public DateOnly? FromDate { get; init; }

    public DateOnly? ToDate { get; init; }

    public DisputeStatus? Status { get; init; }

    public class GetDisputesQueryHandler(IMapper mapper, IApplicationDbContext applicationDbContext, ICurrentCustomerService currentCustomerService)
        : IRequestHandler<GetDisputesQuery, PaginatedList<DisputeSummaryDTO>>
    {
        private readonly IMapper mapper = mapper;
        private readonly IApplicationDbContext applicationDbContext = applicationDbContext;
        private readonly ICurrentCustomerService currentCustomerService = currentCustomerService;

        public async Task<PaginatedList<DisputeSummaryDTO>> Handle(GetDisputesQuery request, CancellationToken cancellationToken)
        {
            var from = request.FromDate?.ToUtcStartOfBusinessDay();
            var customerId = await currentCustomerService.GetCustomerIdAsync(cancellationToken);
            var toExclusive = request.ToDate?.AddDays(1).ToUtcStartOfBusinessDay();

            var disputes = await applicationDbContext.Disputes
                                 .Where(dispute => dispute.CustomerId == customerId)
                                 .WhereIf(from.HasValue, dispute => dispute.SubmittedAt >= from)
                                 .WhereIf(toExclusive.HasValue, dispute => dispute.SubmittedAt < toExclusive)
                                 .WhereIf(request.Status.HasValue, dispute => dispute.Status == request.Status)
                                 .OrderByDescending(dispute => dispute.SubmittedAt)
                                 .ThenByDescending(dispute => dispute.Reference)
                                 .ProjectTo<DisputeSummaryDTO>(mapper.ConfigurationProvider)
                                 .ToPaginatedListAsync(request, cancellationToken);

            return disputes;
        }
    }
}
