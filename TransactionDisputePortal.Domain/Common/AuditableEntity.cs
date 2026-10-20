namespace TransactionDisputePortal.Domain.Common;

public abstract class AuditableEntity : BaseEntity
{
    public string CreatedBy { get; set; } = "System";
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
