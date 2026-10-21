using TransactionDisputePortal.Domain.Common;

namespace TransactionDisputePortal.Domain.Events;

public record DisputeSubmitted(Guid DisputeId, string Reference, Guid CustomerId, Guid TransactionId) : DomainEvent;
