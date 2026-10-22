using TransactionDisputePortal.Application.Common.Models;
using TransactionDisputePortal.Domain.Enums;

namespace TransactionDisputePortal.Application.Features.Lookups.Models;

public record LookupsDTO
{
    public IReadOnlyList<LookupDTO<DisputeReason>> DisputeReasons { get; init; } = [];
    public IReadOnlyList<LookupDTO<DisputeStatus>> DisputeStatuses { get; init; } = [];
    public IReadOnlyList<LookupDTO<TransactionStatus>> TransactionStatuses { get; init; } = [];
}
