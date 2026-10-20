using TransactionDisputePortal.Domain.Common;
using TransactionDisputePortal.Domain.Enums;

namespace TransactionDisputePortal.Domain.Entities;

public class Account : AuditableEntity
{
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public string AccountNumber { get; set; } = string.Empty;
    public AccountType Type { get; set; }

    public List<Transaction> Transactions { get; set; } = [];
}
