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

    private static Transaction CreateTransaction(DateTime transactionDate, TransactionStatus status = TransactionStatus.Posted)
    {
        return TestData.CreateCustomer().AddAccount().AddTransaction(transactionDate, status: status);
    }
}
