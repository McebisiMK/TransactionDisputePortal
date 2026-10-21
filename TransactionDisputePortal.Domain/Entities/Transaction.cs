using TransactionDisputePortal.Domain.Common;
using TransactionDisputePortal.Domain.Enums;

namespace TransactionDisputePortal.Domain.Entities;

public class Transaction : AuditableEntity
{
    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;

    public string Reference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Merchant { get; set; } = string.Empty;
    public TransactionCategory Category { get; set; }
    public DateTime TransactionDate { get; set; }
    public TransactionStatus Status { get; set; }

    public List<Dispute> Disputes { get; set; } = [];
}
