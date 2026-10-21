using MediatR;
using Microsoft.Extensions.Logging;
using TransactionDisputePortal.Application.Common.Models;
using TransactionDisputePortal.Domain.Events;

namespace TransactionDisputePortal.Application.Features.Disputes.EventHandlers;

// Stands in for an email/SMS to the customer. In production this would go through an outbox so the message
// is only sent once the dispute is committed, and is retried if the notification service is down.
public class DisputeSubmittedEventHandler(ILogger<DisputeSubmittedEventHandler> logger) : INotificationHandler<DomainEventNotification<DisputeSubmitted>>
{
    private readonly ILogger<DisputeSubmittedEventHandler> logger = logger;

    public Task Handle(DomainEventNotification<DisputeSubmitted> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        logger.LogInformation("Simulated notification to customer {CustomerId}: Dispute {DisputeReference} received for transaction {TransactionId} and queued for review",
            domainEvent.CustomerId, domainEvent.Reference, domainEvent.TransactionId);

        return Task.CompletedTask;
    }
}
