using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Application.Common.Exceptions;
using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Features.Agent.Commands.UpdateDisputeStatus;
using TransactionDisputePortal.Domain.Common;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Domain.Enums;
using TransactionDisputePortal.Domain.Events;
using TransactionDisputePortal.Domain.Exceptions;
using TransactionDisputePortal.UnitTests.TestSupport;

namespace TransactionDisputePortal.UnitTests.Features.Agent;

[TestFixture]
public class UpdateDisputeStatusCommandHandlerTests
{
    private const string Username = "test.agent";

    private static readonly DateTime UtcNow = new(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task Handle_Given_A_Submitted_Dispute_And_Under_Review_Should_Start_The_Review_And_Return_The_Dispute()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer("Thandi Nkosi");
        var dispute = customer.AddAccount().AddTransaction(UtcNow.AddDays(-5)).AddDispute(UtcNow.AddDays(-2));

        using var context = await CreateContext(customer);
        var handler = CreateHandler(context);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new UpdateDisputeStatusCommand(dispute.Id, DisputeStatus.UnderReview, "Requested records from the merchant.", []), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Id.Should().Be(dispute.Id);
        actual.Status.Should().Be(DisputeStatus.UnderReview);
        actual.CustomerName.Should().Be("Thandi Nkosi");
        actual.Timeline[^1].Should().BeEquivalentTo(new
        {
            FromStatus = (DisputeStatus?)DisputeStatus.Submitted,
            ToStatus = DisputeStatus.UnderReview,
            Comment = "Requested records from the merchant.",
            ChangedBy = Username,
            ChangedAt = UtcNow
        });

        context.ChangeTracker.Clear();
        (await context.Disputes.SingleAsync(saved => saved.Id == dispute.Id)).Status.Should().Be(DisputeStatus.UnderReview);
    }

    [TestCase(DisputeStatus.Approved)]
    [TestCase(DisputeStatus.Rejected)]
    public async Task Handle_Given_A_Dispute_Under_Review_And_An_Outcome_Should_Resolve_It_With_The_Comment(DisputeStatus status)
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var dispute = customer.AddAccount().AddTransaction(UtcNow.AddDays(-5)).AddDispute(UtcNow.AddDays(-2));
        dispute.StartReview(null, Username, UtcNow.AddDays(-1));

        using var context = await CreateContext(customer);
        var handler = CreateHandler(context);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new UpdateDisputeStatusCommand(dispute.Id, status, "Merchant records checked.", []), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Status.Should().Be(status);
        actual.Timeline.Select(history => history.ToStatus).Should().Equal(DisputeStatus.Submitted, DisputeStatus.UnderReview, status);
        actual.Timeline[^1].Comment.Should().Be("Merchant records checked.");
    }

    [Test]
    public async Task Handle_Given_A_Status_Change_Is_Saved_Should_Dispatch_Dispute_Status_Changed()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var dispute = customer.AddAccount().AddTransaction(UtcNow.AddDays(-5)).AddDispute(UtcNow.AddDays(-2));
        var domainEventDispatcher = Substitute.For<IDomainEventDispatcher>();

        using var context = await CreateContext(domainEventDispatcher, customer);
        domainEventDispatcher.ClearReceivedCalls();
        var handler = CreateHandler(context);

        //------------------------------------- Act     ------------------------------------------------------
        await handler.Handle(new UpdateDisputeStatusCommand(dispute.Id, DisputeStatus.UnderReview, null, []), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        await domainEventDispatcher.Received(1).DispatchAsync(
            Arg.Is<IEnumerable<DomainEvent>>(events => events.OfType<DisputeStatusChanged>().Single().ToStatus == DisputeStatus.UnderReview),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_Given_A_Change_The_Current_Status_Does_Not_Allow_Should_Throw_Domain_Exception_And_Save_Nothing()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var dispute = customer.AddAccount().AddTransaction(UtcNow.AddDays(-5)).AddDispute(UtcNow.AddDays(-2));

        using var context = await CreateContext(customer);
        var handler = CreateHandler(context);

        //------------------------------------- Act     ------------------------------------------------------
        var act = () => handler.Handle(new UpdateDisputeStatusCommand(dispute.Id, DisputeStatus.Approved, "Refunded.", []), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        await act.Should().ThrowAsync<DomainException>().WithMessage($"Dispute {dispute.Reference} is Submitted and can only be resolved while it is UnderReview.");

        context.ChangeTracker.Clear();
        var saved = await context.Disputes.Include(saved => saved.StatusHistory).SingleAsync(saved => saved.Id == dispute.Id);
        saved.Status.Should().Be(DisputeStatus.Submitted);
        saved.StatusHistory.Should().ContainSingle();
    }

    [Test]
    public async Task Handle_Given_A_Stale_Row_Version_Should_Throw_Concurrency_Exception_And_Save_Nothing()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var dispute = customer.AddAccount().AddTransaction(UtcNow.AddDays(-5)).AddDispute(UtcNow.AddDays(-2));

        using var context = await CreateContext(customer);
        var handler = CreateHandler(context);

        //------------------------------------- Act     ------------------------------------------------------
        var act = () => handler.Handle(new UpdateDisputeStatusCommand(dispute.Id, DisputeStatus.UnderReview, null, [1, 2, 3]), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        await act.Should().ThrowAsync<ConcurrencyException>();

        context.ChangeTracker.Clear();
        (await context.Disputes.SingleAsync(saved => saved.Id == dispute.Id)).Status.Should().Be(DisputeStatus.Submitted);
    }

    private static Task<TestDbContext> CreateContext(params Customer[] customers) => CreateContext(Substitute.For<IDomainEventDispatcher>(), customers);

    private static async Task<TestDbContext> CreateContext(IDomainEventDispatcher domainEventDispatcher, params Customer[] customers)
    {
        var context = TestDbContext.Create(domainEventDispatcher);
        context.Customers.AddRange(customers);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        return context;
    }

    private static UpdateDisputeStatusCommand.UpdateDisputeStatusCommandHandler CreateHandler(IApplicationDbContext context)
    {
        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.Username.Returns(Username);

        return new UpdateDisputeStatusCommand.UpdateDisputeStatusCommandHandler(TestData.CreateMapper(), context, currentUserService, TestData.CreateTimeProvider(UtcNow));
    }
}
