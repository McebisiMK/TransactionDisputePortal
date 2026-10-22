using System.ComponentModel;

namespace TransactionDisputePortal.Domain.Enums;

public enum DisputeStatus
{
    [Description("Submitted")]
    Submitted,

    [Description("Under review")]
    UnderReview,

    [Description("Approved")]
    Approved,

    [Description("Rejected")]
    Rejected,

    [Description("Withdrawn")]
    Withdrawn
}
