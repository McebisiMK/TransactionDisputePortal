using TransactionDisputePortal.Domain.Common;

namespace TransactionDisputePortal.Domain.Events;

public record DisputeWithdrawn(Guid DisputeId, string Reference, Guid CustomerId, Guid TransactionId) : DomainEvent;
