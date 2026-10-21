using MediatR;
using Microsoft.Extensions.Logging;
using TransactionDisputePortal.Application.Common.Models;
using TransactionDisputePortal.Domain.Events;

namespace TransactionDisputePortal.Application.Features.Disputes.EventHandlers;

// Stands in for an email/SMS to the customer, like DisputeSubmittedEventHandler.
public class DisputeStatusChangedEventHandler(ILogger<DisputeStatusChangedEventHandler> logger) : INotificationHandler<DomainEventNotification<DisputeStatusChanged>>
{
    private readonly ILogger<DisputeStatusChangedEventHandler> logger = logger;

    public Task Handle(DomainEventNotification<DisputeStatusChanged> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        logger.LogInformation("Simulated notification to customer {CustomerId}: Dispute {DisputeReference} has moved from {FromStatus} to {ToStatus}",
            domainEvent.CustomerId, domainEvent.Reference, domainEvent.FromStatus, domainEvent.ToStatus);

        return Task.CompletedTask;
    }
}
