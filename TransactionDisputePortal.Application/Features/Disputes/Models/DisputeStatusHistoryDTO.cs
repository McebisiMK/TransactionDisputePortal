using TransactionDisputePortal.Application.Common.Mappings;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Domain.Enums;

namespace TransactionDisputePortal.Application.Features.Disputes.Models;

public record DisputeStatusHistoryDTO : IMapFrom<DisputeStatusHistory>
{
    public DisputeStatus? FromStatus { get; init; }
    public DisputeStatus ToStatus { get; init; }
    public string? Comment { get; init; }
    public string ChangedBy { get; init; } = string.Empty;
    public DateTime ChangedAt { get; init; }
}
