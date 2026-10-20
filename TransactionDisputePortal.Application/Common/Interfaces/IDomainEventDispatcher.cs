using TransactionDisputePortal.Domain.Common;

namespace TransactionDisputePortal.Application.Common.Interfaces;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken);
}
