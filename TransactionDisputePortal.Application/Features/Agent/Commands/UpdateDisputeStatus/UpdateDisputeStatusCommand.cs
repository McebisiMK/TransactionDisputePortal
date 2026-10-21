using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Application.Common.Exceptions;
using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Features.Agent.Models;
using TransactionDisputePortal.Domain.Enums;

namespace TransactionDisputePortal.Application.Features.Agent.Commands.UpdateDisputeStatus;

public record UpdateDisputeStatusCommand(Guid Id, DisputeStatus? Status, string? Comment, byte[]? RowVersion) : IRequest<AgentDisputeDTO>
{
    public static readonly IReadOnlyList<DisputeStatus> AllowedStatuses = [DisputeStatus.UnderReview, DisputeStatus.Approved, DisputeStatus.Rejected];

    public class UpdateDisputeStatusCommandHandler(IMapper mapper, IApplicationDbContext applicationDbContext, ICurrentUserService currentUserService,
        TimeProvider timeProvider)
        : IRequestHandler<UpdateDisputeStatusCommand, AgentDisputeDTO>
    {
        private readonly IMapper mapper = mapper;
        private readonly TimeProvider timeProvider = timeProvider;
        private readonly ICurrentUserService currentUserService = currentUserService;
        private readonly IApplicationDbContext applicationDbContext = applicationDbContext;

        public async Task<AgentDisputeDTO> Handle(UpdateDisputeStatusCommand request, CancellationToken cancellationToken)
        {
            var dispute = await applicationDbContext.Disputes
                                .Include(dispute => dispute.StatusHistory)
                                .FirstAsync(dispute => dispute.Id == request.Id, cancellationToken);

            if (!dispute.RowVersion.AsSpan().SequenceEqual(request.RowVersion))
                throw new ConcurrencyException();

            var changedBy = currentUserService.Username ?? "System";
            var utcNow = timeProvider.GetUtcNow().UtcDateTime;

            switch (request.Status)
            {
                case DisputeStatus.UnderReview:
                    dispute.StartReview(request.Comment, changedBy, utcNow);
                    break;
                case DisputeStatus.Approved:
                    dispute.Resolve(DisputeOutcome.Approved, request.Comment!, changedBy, utcNow);
                    break;
                case DisputeStatus.Rejected:
                    dispute.Resolve(DisputeOutcome.Rejected, request.Comment!, changedBy, utcNow);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(request), request.Status, "The validator only lets through statuses an agent can set.");
            }

            await applicationDbContext.SaveChangesAsync(cancellationToken);

            return await applicationDbContext.Disputes
                         .Where(updated => updated.Id == dispute.Id)
                         .ProjectTo<AgentDisputeDTO>(mapper.ConfigurationProvider)
                         .FirstAsync(cancellationToken);
        }
    }
}
