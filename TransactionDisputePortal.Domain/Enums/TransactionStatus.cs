using System.ComponentModel;

namespace TransactionDisputePortal.Domain.Enums;

public enum TransactionStatus
{
    [Description("Pending")]
    Pending,

    [Description("Posted")]
    Posted
}
