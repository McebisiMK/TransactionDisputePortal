using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Application.Common.Exceptions;
using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Features.Disputes.Models;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Domain.Enums;
using TransactionDisputePortal.Domain.Exceptions;

namespace TransactionDisputePortal.Application.Features.Disputes.Commands.CreateDispute;

public record CreateDisputeCommand : IRequest<DisputeDTO>
{
    public Guid TransactionId { get; init; }

    public DisputeReason? Reason { get; init; }

    public string? Description { get; init; }

    public class CreateDisputeCommandHandler(IMapper mapper, IApplicationDbContext applicationDbContext, ICurrentCustomerService currentCustomerService,
        ICurrentUserService currentUserService, IDisputeReferenceGenerator disputeReferenceGenerator, TimeProvider timeProvider)
        : IRequestHandler<CreateDisputeCommand, DisputeDTO>
    {
        private readonly IMapper mapper = mapper;
        private readonly TimeProvider timeProvider = timeProvider;
        private readonly ICurrentUserService currentUserService = currentUserService;
        private readonly IApplicationDbContext applicationDbContext = applicationDbContext;
        private readonly ICurrentCustomerService currentCustomerService = currentCustomerService;
        private readonly IDisputeReferenceGenerator disputeReferenceGenerator = disputeReferenceGenerator;

        public async Task<DisputeDTO> Handle(CreateDisputeCommand request, CancellationToken cancellationToken)
        {
            var customerId = await currentCustomerService.GetCustomerIdAsync(cancellationToken);
            var transaction = await applicationDbContext.Transactions
                                    .FirstAsync(transaction => transaction.Id == request.TransactionId && transaction.Account.CustomerId == customerId, cancellationToken);

            var hasOpenDispute = await applicationDbContext.Disputes
                                       .AnyAsync(dispute => dispute.TransactionId == transaction.Id
                                                         && (dispute.Status == DisputeStatus.Submitted || dispute.Status == DisputeStatus.UnderReview), cancellationToken);

            if (hasOpenDispute)
                throw OpenDisputeExists(transaction);

            var reference = await disputeReferenceGenerator.NextReferenceAsync(cancellationToken);
            var dispute = Dispute.Submit(reference, transaction, customerId, request.Reason!.Value, request.Description,
                currentUserService.Username ?? "System", timeProvider.GetUtcNow().UtcDateTime);

            applicationDbContext.Disputes.Add(dispute);

            try
            {
                await applicationDbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DuplicateRecordException)
            {
                throw OpenDisputeExists(transaction);
            }

            return mapper.Map<DisputeDTO>(dispute);
        }

        private static DomainException OpenDisputeExists(Transaction transaction) => new($"Transaction {transaction.Reference} already has an open dispute.");
    }
}
