using TransactionDisputePortal.Application.Common.Mappings;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Domain.Enums;

namespace TransactionDisputePortal.Application.Features.Disputes.Models;

public record DisputeDTO : IMapFrom<Dispute>
{
    public Guid Id { get; init; }
    public string Reference { get; init; } = string.Empty;
    public Guid TransactionId { get; init; }
    public DisputeReason Reason { get; init; }
    public string? Description { get; init; }
    public DisputeStatus Status { get; init; }
    public DateTime SubmittedAt { get; init; }
}
