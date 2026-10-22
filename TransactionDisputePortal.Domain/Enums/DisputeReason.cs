using System.ComponentModel;

namespace TransactionDisputePortal.Domain.Enums;

public enum DisputeReason
{
    [Description("Unauthorised transaction")]
    UnauthorisedTransaction,

    [Description("Duplicate charge")]
    DuplicateCharge,

    [Description("Incorrect amount")]
    IncorrectAmount,

    [Description("Goods or services not received")]
    GoodsOrServicesNotReceived,

    [Description("Not as described")]
    NotAsDescribed,

    [Description("Cancelled subscription")]
    CancelledSubscription,

    [Description("Refund not received")]
    RefundNotReceived,

    [Description("Other")]
    Other
}
