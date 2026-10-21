using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Features.Disputes.Commands.CreateDispute;
using TransactionDisputePortal.Domain.Common;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Domain.Enums;
using TransactionDisputePortal.Domain.Events;
using TransactionDisputePortal.Domain.Exceptions;
using TransactionDisputePortal.UnitTests.TestSupport;

namespace TransactionDisputePortal.UnitTests.Features.Disputes;

[TestFixture]
public class CreateDisputeCommandHandlerTests
{
    private const string Reference = "DSP-2026-000042";
    private const string Username = "test.customer";

    private static readonly DateTime UtcNow = new(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task Handle_Given_A_Posted_Transaction_Inside_The_Window_Should_Save_A_Submitted_Dispute_And_Return_It()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var transaction = customer.AddAccount().AddTransaction(UtcNow.AddDays(-3));

        using var context = await CreateContext(customer);
        var handler = CreateHandler(context, customer.Id);
        var command = new CreateDisputeCommand { TransactionId = transaction.Id, Reason = DisputeReason.IncorrectAmount, Description = "Charged R199.99 instead of R19.99." };

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(command, CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Reference.Should().Be(Reference);
        actual.TransactionId.Should().Be(transaction.Id);
        actual.Reason.Should().Be(DisputeReason.IncorrectAmount);
        actual.Description.Should().Be("Charged R199.99 instead of R19.99.");
        actual.Status.Should().Be(DisputeStatus.Submitted);
        actual.SubmittedAt.Should().Be(UtcNow);

        context.ChangeTracker.Clear();
        var saved = await context.Disputes.Include(dispute => dispute.StatusHistory).SingleAsync(dispute => dispute.Id == actual.Id);
        saved.CustomerId.Should().Be(customer.Id);
        saved.StatusHistory.Should().ContainSingle(history => history.ToStatus == DisputeStatus.Submitted && history.ChangedBy == Username);
    }

    [Test]
    public async Task Handle_Given_A_Dispute_Is_Saved_Should_Dispatch_Dispute_Submitted()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var transaction = customer.AddAccount().AddTransaction(UtcNow.AddDays(-3));
        var domainEventDispatcher = Substitute.For<IDomainEventDispatcher>();

        using var context = await CreateContext(domainEventDispatcher, customer);
        domainEventDispatcher.ClearReceivedCalls();
        var handler = CreateHandler(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new CreateDisputeCommand { TransactionId = transaction.Id, Reason = DisputeReason.DuplicateCharge }, CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        await domainEventDispatcher.Received(1).DispatchAsync(
            Arg.Is<IEnumerable<DomainEvent>>(events => events.OfType<DisputeSubmitted>().Single().DisputeId == actual.Id),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_Given_A_Pending_Transaction_Should_Throw_Domain_Exception_And_Save_Nothing()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var transaction = customer.AddAccount().AddTransaction(UtcNow, status: TransactionStatus.Pending);

        using var context = await CreateContext(customer);
        var handler = CreateHandler(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var act = () => handler.Handle(new CreateDisputeCommand { TransactionId = transaction.Id, Reason = DisputeReason.DuplicateCharge }, CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        await act.Should().ThrowAsync<DomainException>().WithMessage("*still pending*");
        (await context.Disputes.CountAsync()).Should().Be(0);
    }

    [Test]
    public async Task Handle_Given_A_Transaction_Older_Than_The_Dispute_Window_Should_Throw_Domain_Exception_And_Save_Nothing()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var transaction = customer.AddAccount().AddTransaction(UtcNow.AddDays(-Dispute.DisputeWindowDays - 1));

        using var context = await CreateContext(customer);
        var handler = CreateHandler(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var act = () => handler.Handle(new CreateDisputeCommand { TransactionId = transaction.Id, Reason = DisputeReason.DuplicateCharge }, CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        await act.Should().ThrowAsync<DomainException>().WithMessage($"*older than {Dispute.DisputeWindowDays} days*");
        (await context.Disputes.CountAsync()).Should().Be(0);
    }

    [Test]
    public async Task Handle_Given_The_Transaction_Already_Has_An_Open_Dispute_Should_Throw_Domain_Exception_Without_Using_A_Reference()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var transaction = customer.AddAccount().AddTransaction(UtcNow.AddDays(-3));
        transaction.AddDispute(UtcNow.AddDays(-1));

        using var context = await CreateContext(customer);
        var disputeReferenceGenerator = CreateDisputeReferenceGenerator();
        var handler = CreateHandler(context, customer.Id, disputeReferenceGenerator);

        //------------------------------------- Act     ------------------------------------------------------
        var act = () => handler.Handle(new CreateDisputeCommand { TransactionId = transaction.Id, Reason = DisputeReason.DuplicateCharge }, CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        await act.Should().ThrowAsync<DomainException>().WithMessage($"Transaction {transaction.Reference} already has an open dispute.");
        await disputeReferenceGenerator.DidNotReceiveWithAnyArgs().NextReferenceAsync(default);
        (await context.Disputes.CountAsync()).Should().Be(1);
    }

    [Test]
    public async Task Handle_Given_Another_Transaction_Has_An_Open_Dispute_Should_Still_Allow_This_One()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var account = customer.AddAccount();
        account.AddTransaction(UtcNow.AddDays(-5)).AddDispute(UtcNow.AddDays(-1));
        var transaction = account.AddTransaction(UtcNow.AddDays(-3));

        using var context = await CreateContext(customer);
        var handler = CreateHandler(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new CreateDisputeCommand { TransactionId = transaction.Id, Reason = DisputeReason.DuplicateCharge }, CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.TransactionId.Should().Be(transaction.Id);
    }

    private static Task<TestDbContext> CreateContext(params Customer[] customers) => CreateContext(Substitute.For<IDomainEventDispatcher>(), customers);

    private static async Task<TestDbContext> CreateContext(IDomainEventDispatcher domainEventDispatcher, params Customer[] customers)
    {
        var context = TestDbContext.Create(domainEventDispatcher);
        context.Customers.AddRange(customers);
        await context.SaveChangesAsync();

        return context;
    }

    private static IDisputeReferenceGenerator CreateDisputeReferenceGenerator()
    {
        var disputeReferenceGenerator = Substitute.For<IDisputeReferenceGenerator>();
        disputeReferenceGenerator.NextReferenceAsync(Arg.Any<CancellationToken>()).Returns(Reference);

        return disputeReferenceGenerator;
    }

    private static CreateDisputeCommand.CreateDisputeCommandHandler CreateHandler(IApplicationDbContext context, Guid customerId, IDisputeReferenceGenerator? disputeReferenceGenerator = null)
    {
        var currentCustomerService = Substitute.For<ICurrentCustomerService>();
        currentCustomerService.GetCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(customerId);

        var currentUserService = Substitute.For<ICurrentUserService>();
        currentUserService.Username.Returns(Username);

        return new CreateDisputeCommand.CreateDisputeCommandHandler(TestData.CreateMapper(), context, currentCustomerService, currentUserService,
            disputeReferenceGenerator ?? CreateDisputeReferenceGenerator(), TestData.CreateTimeProvider(UtcNow));
    }
}
