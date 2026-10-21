using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Features.Disputes.Commands.WithdrawDispute;
using TransactionDisputePortal.Domain.Common;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Domain.Enums;
using TransactionDisputePortal.Domain.Events;
using TransactionDisputePortal.Domain.Exceptions;
using TransactionDisputePortal.UnitTests.TestSupport;

namespace TransactionDisputePortal.UnitTests.Features.Disputes;

[TestFixture]
public class WithdrawDisputeCommandHandlerTests
{
    private const string Username = "test.customer";

    private static readonly DateTime UtcNow = new(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task Handle_Given_The_Customers_Submitted_Dispute_Should_Save_It_As_Withdrawn_And_Return_The_Timeline()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var dispute = customer.AddAccount().AddTransaction(UtcNow.AddDays(-5)).AddDispute(UtcNow.AddDays(-2));

        using var context = await CreateContext(customer);
        var handler = CreateHandler(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new WithdrawDisputeCommand(dispute.Id, "The merchant refunded me."), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Id.Should().Be(dispute.Id);
        actual.Status.Should().Be(DisputeStatus.Withdrawn);
        actual.Timeline.Select(history => history.ToStatus).Should().Equal(DisputeStatus.Submitted, DisputeStatus.Withdrawn);
        actual.Timeline[^1].Should().BeEquivalentTo(new
        {
            FromStatus = (DisputeStatus?)DisputeStatus.Submitted,
            Comment = "The merchant refunded me.",
            ChangedBy = Username,
            ChangedAt = UtcNow
        });

        context.ChangeTracker.Clear();
        var saved = await context.Disputes.SingleAsync(saved => saved.Id == dispute.Id);
        saved.Status.Should().Be(DisputeStatus.Withdrawn);
        saved.IsOpen.Should().BeFalse();
    }

    [Test]
    public async Task Handle_Given_A_Dispute_Is_Withdrawn_Should_Dispatch_Dispute_Withdrawn()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var dispute = customer.AddAccount().AddTransaction(UtcNow.AddDays(-5)).AddDispute(UtcNow.AddDays(-2));
        var domainEventDispatcher = Substitute.For<IDomainEventDispatcher>();

        using var context = await CreateContext(domainEventDispatcher, customer);
        domainEventDispatcher.ClearReceivedCalls();
        var handler = CreateHandler(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        await handler.Handle(new WithdrawDisputeCommand(dispute.Id, null), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        await domainEventDispatcher.Received(1).DispatchAsync(
            Arg.Is<IEnumerable<DomainEvent>>(events => events.OfType<DisputeWithdrawn>().Single().DisputeId == dispute.Id),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_Given_A_Dispute_Under_Review_Should_Throw_Domain_Exception_And_Save_Nothing()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var dispute = customer.AddAccount().AddTransaction(UtcNow.AddDays(-5)).AddDispute(UtcNow.AddDays(-2));
        dispute.StartReview(null, "test.agent", UtcNow.AddDays(-1));

        using var context = await CreateContext(customer);
        var handler = CreateHandler(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var act = () => handler.Handle(new WithdrawDisputeCommand(dispute.Id, null), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        await act.Should().ThrowAsync<DomainException>().WithMessage($"Dispute {dispute.Reference} is UnderReview and can only be withdrawn while it is Submitted.");

        context.ChangeTracker.Clear();
        var saved = await context.Disputes.Include(saved => saved.StatusHistory).SingleAsync(saved => saved.Id == dispute.Id);
        saved.Status.Should().Be(DisputeStatus.UnderReview);
        saved.StatusHistory.Should().HaveCount(2);
    }

    [Test]
    public async Task Handle_Given_Another_Customers_Dispute_Should_Not_Withdraw_It()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var otherCustomer = TestData.CreateCustomer("Other Customer");
        var otherDispute = otherCustomer.AddAccount().AddTransaction(UtcNow.AddDays(-5)).AddDispute(UtcNow.AddDays(-2));

        using var context = await CreateContext(customer, otherCustomer);
        var handler = CreateHandler(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var act = () => handler.Handle(new WithdrawDisputeCommand(otherDispute.Id, null), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        await act.Should().ThrowAsync<InvalidOperationException>();

        context.ChangeTracker.Clear();
        (await context.Disputes.SingleAsync(saved => saved.Id == otherDispute.Id)).Status.Should().Be(DisputeStatus.Submitted);
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

    private static WithdrawDisputeCommand.WithdrawDisputeCommandHandler CreateHandler(IApplicationDbContext context, Guid customerId)
    {
        var currentCustomerService = Substitute.For<ICurrentCustomerService>();
        currentCustomerService.GetCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(customerId);

        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.Username.Returns(Username);

        return new WithdrawDisputeCommand.WithdrawDisputeCommandHandler(TestData.CreateMapper(), context, currentCustomerService, currentUserService,
            TestData.CreateTimeProvider(UtcNow));
    }
}
