using TransactionDisputePortal.Domain.Common;
using TransactionDisputePortal.Domain.Enums;

namespace TransactionDisputePortal.Domain.Entities;

public class DisputeStatusHistory : BaseEntity
{
    public const int CommentMaxLength = 1000;

    private DisputeStatusHistory() { }

    internal DisputeStatusHistory(Guid disputeId, DisputeStatus? fromStatus, DisputeStatus toStatus, string? comment, string changedBy, DateTime changedAt)
    {
        DisputeId = disputeId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Comment = comment;
        ChangedBy = changedBy;
        ChangedAt = changedAt;
    }

    public Guid DisputeId { get; private set; }
    public DisputeStatus? FromStatus { get; private set; }
    public DisputeStatus ToStatus { get; private set; }
    public string? Comment { get; private set; }
    public string ChangedBy { get; private set; } = string.Empty;
    public DateTime ChangedAt { get; private set; }
}
