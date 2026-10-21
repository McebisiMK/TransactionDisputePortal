using TransactionDisputePortal.Domain.Common;
using TransactionDisputePortal.Domain.Enums;

namespace TransactionDisputePortal.Domain.Events;

public record DisputeStatusChanged(Guid DisputeId, string Reference, Guid CustomerId, DisputeStatus FromStatus, DisputeStatus ToStatus) : DomainEvent;
