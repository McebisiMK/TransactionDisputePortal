using MediatR;
using TransactionDisputePortal.Domain.Common;

namespace TransactionDisputePortal.Application.Common.Models;

public record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification where TDomainEvent : DomainEvent;
