using TransactionDisputePortal.Domain.Common;
using TransactionDisputePortal.Domain.Enums;
using TransactionDisputePortal.Domain.Events;
using TransactionDisputePortal.Domain.Exceptions;

namespace TransactionDisputePortal.Domain.Entities;

public class Dispute : AuditableEntity
{
    public const int DisputeWindowDays = 120;
    public const int DescriptionMaxLength = 1000;

    private readonly List<DisputeStatusHistory> statusHistory = [];

    private Dispute() { }

    public string Reference { get; private set; } = string.Empty;

    public Guid TransactionId { get; private set; }
    public Transaction Transaction { get; private set; } = null!;

    public Guid CustomerId { get; private set; }
    public Customer Customer { get; private set; } = null!;

    public DisputeReason Reason { get; private set; }
    public string? Description { get; private set; }
    public DisputeStatus Status { get; private set; }
    public DateTime SubmittedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<DisputeStatusHistory> StatusHistory => statusHistory.AsReadOnly();

    public bool IsOpen => Status is DisputeStatus.Submitted or DisputeStatus.UnderReview;

    public static DateTime GetDisputeWindowStart(DateTime utcNow) => utcNow.AddDays(-DisputeWindowDays);

    public static Dispute Submit(string reference, Transaction transaction, Guid customerId, DisputeReason reason, string? description, string submittedBy, DateTime utcNow)
    {
        if (transaction.Status != TransactionStatus.Posted)
            throw new DomainException($"Transaction {transaction.Reference} is still pending and can only be disputed once it has posted.");

        if (transaction.TransactionDate < GetDisputeWindowStart(utcNow))
            throw new DomainException($"Transaction {transaction.Reference} is older than {DisputeWindowDays} days and can no longer be disputed.");

        if (reason == DisputeReason.Other && string.IsNullOrWhiteSpace(description))
            throw new DomainException("A description is required when the reason is 'Other'.");

        var dispute = new Dispute
        {
            Reference = reference,
            TransactionId = transaction.Id,
            CustomerId = customerId,
            Reason = reason,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Status = DisputeStatus.Submitted,
            SubmittedAt = utcNow
        };

        dispute.statusHistory.Add(new DisputeStatusHistory(dispute.Id, null, DisputeStatus.Submitted, null, submittedBy, utcNow));
        dispute.AddDomainEvent(new DisputeSubmitted(dispute.Id, dispute.Reference, customerId, transaction.Id));

        return dispute;
    }

    public void StartReview(string? comment, string changedBy, DateTime utcNow)
    {
        if (Status != DisputeStatus.Submitted)
            throw new DomainException($"Dispute {Reference} is {Status} and can only be reviewed while it is {DisputeStatus.Submitted}.");

        ChangeStatus(DisputeStatus.UnderReview, comment, changedBy, utcNow);
        AddDomainEvent(new DisputeStatusChanged(Id, Reference, CustomerId, DisputeStatus.Submitted, DisputeStatus.UnderReview));
    }

    public void Resolve(DisputeOutcome outcome, string comment, string changedBy, DateTime utcNow)
    {
        if (Status != DisputeStatus.UnderReview)
            throw new DomainException($"Dispute {Reference} is {Status} and can only be resolved while it is {DisputeStatus.UnderReview}.");

        if (string.IsNullOrWhiteSpace(comment))
            throw new DomainException("A comment is required when resolving a dispute.");

        var toStatus = outcome switch
        {
            DisputeOutcome.Approved => DisputeStatus.Approved,
            DisputeOutcome.Rejected => DisputeStatus.Rejected,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null)
        };

        ChangeStatus(toStatus, comment, changedBy, utcNow);
        AddDomainEvent(new DisputeStatusChanged(Id, Reference, CustomerId, DisputeStatus.UnderReview, toStatus));
    }

    public void Withdraw(string? comment, string changedBy, DateTime utcNow)
    {
        if (Status != DisputeStatus.Submitted)
            throw new DomainException($"Dispute {Reference} is {Status} and can only be withdrawn while it is {DisputeStatus.Submitted}.");

        ChangeStatus(DisputeStatus.Withdrawn, comment, changedBy, utcNow);
        AddDomainEvent(new DisputeWithdrawn(Id, Reference, CustomerId, TransactionId));
    }

    private void ChangeStatus(DisputeStatus toStatus, string? comment, string changedBy, DateTime utcNow)
    {
        statusHistory.Add(new DisputeStatusHistory(Id, Status, toStatus, string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(), changedBy, utcNow));
        Status = toStatus;
    }
}
