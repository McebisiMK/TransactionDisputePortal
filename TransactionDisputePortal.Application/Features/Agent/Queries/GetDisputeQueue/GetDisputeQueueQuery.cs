using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using TransactionDisputePortal.Application.Common.Extensions;
using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Common.Models;
using TransactionDisputePortal.Application.Features.Agent.Models;
using TransactionDisputePortal.Domain.Enums;

namespace TransactionDisputePortal.Application.Features.Agent.Queries.GetDisputeQueue;

public record GetDisputeQueueQuery : PagedQuery, IRequest<PaginatedList<DisputeQueueItemDTO>>
{
    public DisputeStatus? Status { get; init; }

    public class GetDisputeQueueQueryHandler(IMapper mapper, IApplicationDbContext applicationDbContext)
        : IRequestHandler<GetDisputeQueueQuery, PaginatedList<DisputeQueueItemDTO>>
    {
        private readonly IMapper mapper = mapper;
        private readonly IApplicationDbContext applicationDbContext = applicationDbContext;

        public async Task<PaginatedList<DisputeQueueItemDTO>> Handle(GetDisputeQueueQuery request, CancellationToken cancellationToken)
        {
            var disputes = await applicationDbContext.Disputes
                                 .WhereIf(request.Status.HasValue, dispute => dispute.Status == request.Status)
                                 .WhereIf(!request.Status.HasValue, dispute => dispute.Status == DisputeStatus.Submitted || dispute.Status == DisputeStatus.UnderReview)
                                 .OrderBy(dispute => dispute.SubmittedAt)
                                 .ThenBy(dispute => dispute.Reference)
                                 .ProjectTo<DisputeQueueItemDTO>(mapper.ConfigurationProvider)
                                 .ToPaginatedListAsync(request, cancellationToken);

            return disputes;
        }
    }
}
