using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Features.Disputes.Models;

namespace TransactionDisputePortal.Application.Features.Disputes.Commands.WithdrawDispute;

public record WithdrawDisputeCommand(Guid Id, string? Comment) : IRequest<DisputeDTO>
{
    public class WithdrawDisputeCommandHandler(IMapper mapper, IApplicationDbContext applicationDbContext, ICurrentCustomerService currentCustomerService,
        ICurrentUserService currentUserService, TimeProvider timeProvider)
        : IRequestHandler<WithdrawDisputeCommand, DisputeDTO>
    {
        private readonly IMapper mapper = mapper;
        private readonly TimeProvider timeProvider = timeProvider;
        private readonly ICurrentUserService currentUserService = currentUserService;
        private readonly IApplicationDbContext applicationDbContext = applicationDbContext;
        private readonly ICurrentCustomerService currentCustomerService = currentCustomerService;

        public async Task<DisputeDTO> Handle(WithdrawDisputeCommand request, CancellationToken cancellationToken)
        {
            var customerId = await currentCustomerService.GetCustomerIdAsync(cancellationToken);
            var dispute = await applicationDbContext.Disputes
                                .Include(dispute => dispute.StatusHistory)
                                .FirstAsync(dispute => dispute.Id == request.Id && dispute.CustomerId == customerId, cancellationToken);

            dispute.Withdraw(request.Comment, currentUserService.Username ?? "System", timeProvider.GetUtcNow().UtcDateTime);

            await applicationDbContext.SaveChangesAsync(cancellationToken);

            return await applicationDbContext.Disputes
                         .Where(withdrawn => withdrawn.Id == dispute.Id)
                         .ProjectTo<DisputeDTO>(mapper.ConfigurationProvider)
                         .FirstAsync(cancellationToken);
        }
    }
}
