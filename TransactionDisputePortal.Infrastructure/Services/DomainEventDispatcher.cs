using MediatR;
using Microsoft.Extensions.Logging;
using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Common.Models;
using TransactionDisputePortal.Domain.Common;

namespace TransactionDisputePortal.Infrastructure.Services;

public class DomainEventDispatcher(IPublisher publisher, ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
{
    private readonly IPublisher publisher = publisher;
    private readonly ILogger<DomainEventDispatcher> logger = logger;

    public async Task DispatchAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in domainEvents)
        {
            logger.LogDebug("Dispatching domain event {DomainEvent}", domainEvent.GetType().Name);

            var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
            var notification = (INotification)Activator.CreateInstance(notificationType, domainEvent)!;

            await publisher.Publish(notification, cancellationToken);
        }
    }
}
