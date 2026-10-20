using TransactionDisputePortal.Domain.Common;

namespace TransactionDisputePortal.Domain.Entities;

public class Customer : AuditableEntity
{
    public Guid ExternalUserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public List<Account> Accounts { get; set; } = [];
}
