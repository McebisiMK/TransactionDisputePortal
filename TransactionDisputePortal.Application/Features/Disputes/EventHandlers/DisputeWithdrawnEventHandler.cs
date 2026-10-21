using MediatR;
using Microsoft.Extensions.Logging;
using TransactionDisputePortal.Application.Common.Models;
using TransactionDisputePortal.Domain.Events;

namespace TransactionDisputePortal.Application.Features.Disputes.EventHandlers;

// Stands in for an email/SMS to the customer, like DisputeSubmittedEventHandler.
public class DisputeWithdrawnEventHandler(ILogger<DisputeWithdrawnEventHandler> logger) : INotificationHandler<DomainEventNotification<DisputeWithdrawn>>
{
    private readonly ILogger<DisputeWithdrawnEventHandler> logger = logger;

    public Task Handle(DomainEventNotification<DisputeWithdrawn> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        logger.LogInformation("Simulated notification to customer {CustomerId}: Dispute {DisputeReference} for transaction {TransactionId} has been withdrawn",
            domainEvent.CustomerId, domainEvent.Reference, domainEvent.TransactionId);

        return Task.CompletedTask;
    }
}
