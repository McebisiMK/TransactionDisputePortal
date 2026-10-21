using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Domain.Enums;
using TransactionDisputePortal.Domain.Events;
using TransactionDisputePortal.Domain.Exceptions;
using TransactionDisputePortal.UnitTests.TestSupport;

namespace TransactionDisputePortal.UnitTests.Domain;

[TestFixture]
public class DisputeTests
{
    private static readonly DateTime UtcNow = new(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc);

    [Test]
    public void Submit_Given_A_Posted_Transaction_Inside_The_Window_Should_Start_As_Submitted()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customerId = Guid.NewGuid();
        var transaction = CreateTransaction(UtcNow.AddDays(-3));

        //------------------------------------- Act     ------------------------------------------------------
        var actual = Dispute.Submit("DSP-2026-000001", transaction, customerId, DisputeReason.DuplicateCharge, "  Charged twice.  ", "test.customer", UtcNow);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Reference.Should().Be("DSP-2026-000001");
        actual.TransactionId.Should().Be(transaction.Id);
        actual.CustomerId.Should().Be(customerId);
        actual.Reason.Should().Be(DisputeReason.DuplicateCharge);
        actual.Description.Should().Be("Charged twice.");
        actual.Status.Should().Be(DisputeStatus.Submitted);
        actual.SubmittedAt.Should().Be(UtcNow);
        actual.IsOpen.Should().BeTrue();
    }

    [Test]
    public void Submit_Given_A_Valid_Dispute_Should_Record_The_First_History_Entry()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var transaction = CreateTransaction(UtcNow.AddDays(-3));

        //------------------------------------- Act     ------------------------------------------------------
        var actual = Dispute.Submit("DSP-2026-000001", transaction, Guid.NewGuid(), DisputeReason.DuplicateCharge, null, "test.customer", UtcNow);

        //------------------------------------- Assert  ------------------------------------------------------
        var history = actual.StatusHistory.Should().ContainSingle().Subject;
        history.DisputeId.Should().Be(actual.Id);
        history.FromStatus.Should().BeNull();
        history.ToStatus.Should().Be(DisputeStatus.Submitted);
        history.Comment.Should().BeNull();
        history.ChangedBy.Should().Be("test.customer");
        history.ChangedAt.Should().Be(UtcNow);
    }

    [Test]
    public void Submit_Given_A_Valid_Dispute_Should_Raise_Dispute_Submitted()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customerId = Guid.NewGuid();
        var transaction = CreateTransaction(UtcNow.AddDays(-3));

        //------------------------------------- Act     ------------------------------------------------------
        var actual = Dispute.Submit("DSP-2026-000001", transaction, customerId, DisputeReason.DuplicateCharge, null, "test.customer", UtcNow);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.DomainEvents.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new DisputeSubmitted(actual.Id, "DSP-2026-000001", customerId, transaction.Id), options => options.Excluding(domainEvent => domainEvent.OccurredOn));
    }

    [Test]
    public void Submit_Given_A_Pending_Transaction_Should_Throw_Domain_Exception()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var transaction = CreateTransaction(UtcNow, TransactionStatus.Pending);

        //------------------------------------- Act     ------------------------------------------------------
        var act = () => Dispute.Submit("DSP-2026-000001", transaction, Guid.NewGuid(), DisputeReason.DuplicateCharge, null, "test.customer", UtcNow);

        //------------------------------------- Assert  ------------------------------------------------------
        act.Should().Throw<DomainException>().WithMessage($"Transaction {transaction.Reference} is still pending*");
    }

    [Test]
    public void Submit_Given_A_Transaction_Older_Than_The_Dispute_Window_Should_Throw_Domain_Exception()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var transaction = CreateTransaction(UtcNow.AddDays(-Dispute.DisputeWindowDays).AddSeconds(-1));

        //------------------------------------- Act     ------------------------------------------------------
        var act = () => Dispute.Submit("DSP-2026-000001", transaction, Guid.NewGuid(), DisputeReason.DuplicateCharge, null, "test.customer", UtcNow);

        //------------------------------------- Assert  ------------------------------------------------------
        act.Should().Throw<DomainException>().WithMessage($"Transaction {transaction.Reference} is older than {Dispute.DisputeWindowDays} days*");
    }

    [Test]
    public void Submit_Given_A_Transaction_Exactly_At_The_Start_Of_The_Dispute_Window_Should_Succeed()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var transaction = CreateTransaction(Dispute.GetDisputeWindowStart(UtcNow));

        //------------------------------------- Act     ------------------------------------------------------
        var act = () => Dispute.Submit("DSP-2026-000001", transaction, Guid.NewGuid(), DisputeReason.DuplicateCharge, null, "test.customer", UtcNow);

        //------------------------------------- Assert  ------------------------------------------------------
        act.Should().NotThrow();
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Submit_Given_Reason_Other_Without_A_Description_Should_Throw_Domain_Exception(string? description)
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var transaction = CreateTransaction(UtcNow.AddDays(-3));

        //------------------------------------- Act     ------------------------------------------------------
        var act = () => Dispute.Submit("DSP-2026-000001", transaction, Guid.NewGuid(), DisputeReason.Other, description, "test.customer", UtcNow);

        //------------------------------------- Assert  ------------------------------------------------------
        act.Should().Throw<DomainException>().WithMessage("A description is required when the reason is 'Other'.");
    }

    [Test]
    public void Submit_Given_A_Blank_Description_For_A_Reason_That_Does_Not_Need_One_Should_Store_No_Description()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var transaction = CreateTransaction(UtcNow.AddDays(-3));

        //------------------------------------- Act     ------------------------------------------------------
        var actual = Dispute.Submit("DSP-2026-000001", transaction, Guid.NewGuid(), DisputeReason.IncorrectAmount, "   ", "test.customer", UtcNow);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Description.Should().BeNull();
    }

    [Test]
    public void StartReview_Given_A_Submitted_Dispute_Should_Move_It_Under_Review_And_Record_The_Change()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var dispute = CreateDispute();

        //------------------------------------- Act     ------------------------------------------------------
        dispute.StartReview("  Requested records from the merchant.  ", "test.agent", UtcNow.AddDays(1));

        //------------------------------------- Assert  ------------------------------------------------------
        dispute.Status.Should().Be(DisputeStatus.UnderReview);
        dispute.IsOpen.Should().BeTrue();
        dispute.StatusHistory.Last().Should().BeEquivalentTo(new
        {
            DisputeId = dispute.Id,
            FromStatus = (DisputeStatus?)DisputeStatus.Submitted,
            ToStatus = DisputeStatus.UnderReview,
            Comment = "Requested records from the merchant.",
            ChangedBy = "test.agent",
            ChangedAt = UtcNow.AddDays(1)
        });
    }

    [TestCase(DisputeStatus.UnderReview)]
    [TestCase(DisputeStatus.Approved)]
    [TestCase(DisputeStatus.Withdrawn)]
    public void StartReview_Given_A_Dispute_That_Is_Not_Submitted_Should_Throw_Domain_Exception(DisputeStatus status)
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var dispute = CreateDispute(status);

        //------------------------------------- Act     ------------------------------------------------------
        var act = () => dispute.StartReview(null, "test.agent", UtcNow.AddDays(10));

        //------------------------------------- Assert  ------------------------------------------------------
        act.Should().Throw<DomainException>().WithMessage($"Dispute {dispute.Reference} is {status} and can only be reviewed while it is Submitted.");
    }

    [TestCase(DisputeOutcome.Approved, DisputeStatus.Approved)]
    [TestCase(DisputeOutcome.Rejected, DisputeStatus.Rejected)]
    public void Resolve_Given_A_Dispute_Under_Review_Should_Close_It_With_The_Outcome_And_Record_The_Change(DisputeOutcome outcome, DisputeStatus expectedStatus)
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var dispute = CreateDispute(DisputeStatus.UnderReview);

        //------------------------------------- Act     ------------------------------------------------------
        dispute.Resolve(outcome, "Merchant confirmed the duplicate.", "test.agent", UtcNow.AddDays(4));

        //------------------------------------- Assert  ------------------------------------------------------
        dispute.Status.Should().Be(expectedStatus);
        dispute.IsOpen.Should().BeFalse();
        dispute.StatusHistory.Last().Should().BeEquivalentTo(new
        {
            FromStatus = (DisputeStatus?)DisputeStatus.UnderReview,
            ToStatus = expectedStatus,
            Comment = "Merchant confirmed the duplicate.",
            ChangedBy = "test.agent",
            ChangedAt = UtcNow.AddDays(4)
        });
    }

    [TestCase(DisputeStatus.Submitted)]
    [TestCase(DisputeStatus.Rejected)]
    [TestCase(DisputeStatus.Withdrawn)]
    public void Resolve_Given_A_Dispute_That_Is_Not_Under_Review_Should_Throw_Domain_Exception(DisputeStatus status)
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var dispute = CreateDispute(status);

        //------------------------------------- Act     ------------------------------------------------------
        var act = () => dispute.Resolve(DisputeOutcome.Approved, "Refunded.", "test.agent", UtcNow.AddDays(10));

        //------------------------------------- Assert  ------------------------------------------------------
        act.Should().Throw<DomainException>().WithMessage($"Dispute {dispute.Reference} is {status} and can only be resolved while it is UnderReview.");
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Resolve_Given_No_Comment_Should_Throw_Domain_Exception(string comment)
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var dispute = CreateDispute(DisputeStatus.UnderReview);

        //------------------------------------- Act     ------------------------------------------------------
        var act = () => dispute.Resolve(DisputeOutcome.Rejected, comment, "test.agent", UtcNow.AddDays(4));

        //------------------------------------- Assert  ------------------------------------------------------
        act.Should().Throw<DomainException>().WithMessage("A comment is required when resolving a dispute.");
        dispute.Status.Should().Be(DisputeStatus.UnderReview);
    }

    [Test]
    public void Withdraw_Given_A_Submitted_Dispute_Should_Close_It_And_Record_The_Change()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var dispute = CreateDispute();

        //------------------------------------- Act     ------------------------------------------------------
        dispute.Withdraw("   ", "test.customer", UtcNow.AddDays(2));

        //------------------------------------- Assert  ------------------------------------------------------
        dispute.Status.Should().Be(DisputeStatus.Withdrawn);
        dispute.IsOpen.Should().BeFalse();
        dispute.StatusHistory.Last().Should().BeEquivalentTo(new
        {
            FromStatus = (DisputeStatus?)DisputeStatus.Submitted,
            ToStatus = DisputeStatus.Withdrawn,
            Comment = (string?)null,
            ChangedBy = "test.customer",
            ChangedAt = UtcNow.AddDays(2)
        });
    }

    [Test]
    public void Withdraw_Given_A_Comment_Should_Record_It_Trimmed()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var dispute = CreateDispute();

        //------------------------------------- Act     ------------------------------------------------------
        dispute.Withdraw("  The merchant refunded me.  ", "test.customer", UtcNow.AddDays(2));

        //------------------------------------- Assert  ------------------------------------------------------
        dispute.StatusHistory.Last().Comment.Should().Be("The merchant refunded me.");
    }

    [Test]
    public void Withdraw_Given_A_Submitted_Dispute_Should_Raise_Dispute_Withdrawn()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var dispute = CreateDispute();
        dispute.ClearDomainEvents();

        //------------------------------------- Act     ------------------------------------------------------
        dispute.Withdraw(null, "test.customer", UtcNow.AddDays(2));

        //------------------------------------- Assert  ------------------------------------------------------
        dispute.DomainEvents.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new DisputeWithdrawn(dispute.Id, dispute.Reference, dispute.CustomerId, dispute.TransactionId), options => options.Excluding(domainEvent => domainEvent.OccurredOn));
    }

    [TestCase(DisputeStatus.UnderReview)]
    [TestCase(DisputeStatus.Approved)]
    [TestCase(DisputeStatus.Rejected)]
    [TestCase(DisputeStatus.Withdrawn)]
    public void Withdraw_Given_A_Dispute_That_Is_Not_Submitted_Should_Throw_Domain_Exception_And_Leave_It_Unchanged(DisputeStatus status)
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var dispute = CreateDispute(status);
        var historyCount = dispute.StatusHistory.Count;
        dispute.ClearDomainEvents();

        //------------------------------------- Act     ------------------------------------------------------
        var act = () => dispute.Withdraw(null, "test.customer", UtcNow.AddDays(10));

        //------------------------------------- Assert  ------------------------------------------------------
        act.Should().Throw<DomainException>().WithMessage($"Dispute {dispute.Reference} is {status} and can only be withdrawn while it is Submitted.");
        dispute.Status.Should().Be(status);
        dispute.StatusHistory.Should().HaveCount(historyCount);
        dispute.DomainEvents.Should().BeEmpty();
    }

    [Test]
    public void Status_Changes_Should_Build_A_Timeline_Of_Every_Step_In_Order()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var dispute = CreateDispute();

        //------------------------------------- Act     ------------------------------------------------------
        dispute.StartReview(null, "test.agent", UtcNow.AddDays(1));
        dispute.Resolve(DisputeOutcome.Approved, "Refunded.", "test.agent", UtcNow.AddDays(3));

        //------------------------------------- Assert  ------------------------------------------------------
        dispute.StatusHistory.Select(history => history.ToStatus).Should().Equal(DisputeStatus.Submitted, DisputeStatus.UnderReview, DisputeStatus.Approved);
        dispute.DomainEvents.Should().ContainSingle(because: "only submitting raises an event so far");
    }

    private static Dispute CreateDispute(DisputeStatus status = DisputeStatus.Submitted)
    {
        var dispute = Dispute.Submit("DSP-2026-000001", CreateTransaction(UtcNow.AddDays(-3)), Guid.NewGuid(), DisputeReason.DuplicateCharge, null, "test.customer", UtcNow);

        if (status == DisputeStatus.Withdrawn)
            dispute.Withdraw(null, "test.customer", UtcNow.AddHours(1));

        if (status is DisputeStatus.UnderReview or DisputeStatus.Approved or DisputeStatus.Rejected)
            dispute.StartReview(null, "test.agent", UtcNow.AddHours(1));

        if (status is DisputeStatus.Approved or DisputeStatus.Rejected)
            dispute.Resolve(status == DisputeStatus.Approved ? DisputeOutcome.Approved : DisputeOutcome.Rejected, "Resolved.", "test.agent", UtcNow.AddHours(2));

        return dispute;
    }

    private static Transaction CreateTransaction(DateTime transactionDate, TransactionStatus status = TransactionStatus.Posted)
    {
        return TestData.CreateCustomer().AddAccount().AddTransaction(transactionDate, status: status);
    }
}
