using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Domain.Enums;

namespace TransactionDisputePortal.Infrastructure.Persistence.DataSeeding;

internal static class DisputeSeedData
{
    public const string AgentUsername = "test.agent";

    private sealed record SeedDispute(int TransactionIndex, DisputeReason Reason, string Description, DisputeStatus Status,
        string? ReviewComment = null, string? ClosingComment = null);

    private static readonly SeedDispute[] Disputes =
    [
        new(18, DisputeReason.UnauthorisedTransaction, "I don't recognise this transaction and still have my card.", DisputeStatus.Rejected,
            "Requested the transaction records from the merchant.",
            "The transaction was approved with your card and PIN at the merchant's terminal. If you still don't recognise it, please call us to block your card."),
        new(10, DisputeReason.DuplicateCharge, "The same amount went off twice on the same day.", DisputeStatus.Approved,
            "Checking the merchant's settlement records.",
            "The merchant confirmed the duplicate charge. The amount has been refunded to your account."),
        new(7, DisputeReason.IncorrectAmount, "I was charged more than the amount on my till slip.", DisputeStatus.Withdrawn,
            ClosingComment: "I found a second item on the slip, so the amount is correct."),
        new(4, DisputeReason.UnauthorisedTransaction, "I didn't make this purchase.", DisputeStatus.UnderReview,
            "Requested the transaction records from the merchant."),
        new(1, DisputeReason.DuplicateCharge, "I was charged twice for one purchase.", DisputeStatus.Submitted)
    ];

    public static int Count => Disputes.Length;

    public static List<Dispute> CreateDisputes(IReadOnlyList<Transaction> postedTransactions, Guid customerId, string customerUsername,
        IReadOnlyList<string> references, DateTime utcNow)
    {
        return Disputes
            .Select((seed, index) => (seed, reference: references[index]))
            .Where(item => item.seed.TransactionIndex < postedTransactions.Count)
            .Select(item => CreateDispute(item.seed, item.reference, postedTransactions[item.seed.TransactionIndex], customerId, customerUsername, utcNow))
            .ToList();
    }

    private static Dispute CreateDispute(SeedDispute seed, string reference, Transaction transaction, Guid customerId, string customerUsername, DateTime utcNow)
    {
        var submittedAt = NotAfter(transaction.TransactionDate.AddDays(1).AddHours(2), utcNow);
        var dispute = Dispute.Submit(reference, transaction, customerId, seed.Reason, seed.Description, customerUsername, submittedAt);

        if (seed.Status == DisputeStatus.Withdrawn)
            dispute.Withdraw(seed.ClosingComment, customerUsername, NotAfter(submittedAt.AddDays(2), utcNow));

        if (seed.Status is DisputeStatus.UnderReview or DisputeStatus.Approved or DisputeStatus.Rejected)
        {
            var reviewStartedAt = NotAfter(submittedAt.AddDays(1), utcNow);
            dispute.StartReview(seed.ReviewComment, AgentUsername, reviewStartedAt);

            if (seed.Status != DisputeStatus.UnderReview)
            {
                var outcome = seed.Status == DisputeStatus.Approved ? DisputeOutcome.Approved : DisputeOutcome.Rejected;
                dispute.Resolve(outcome, seed.ClosingComment!, AgentUsername, NotAfter(reviewStartedAt.AddDays(3), utcNow));
            }
        }

        dispute.ClearDomainEvents();

        return dispute;
    }

    private static DateTime NotAfter(DateTime value, DateTime utcNow) => value > utcNow ? utcNow : value;
}
