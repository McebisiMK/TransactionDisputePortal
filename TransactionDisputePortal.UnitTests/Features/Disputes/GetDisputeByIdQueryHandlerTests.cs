using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Features.Disputes.Queries.GetDisputeById;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Domain.Enums;
using TransactionDisputePortal.UnitTests.TestSupport;

namespace TransactionDisputePortal.UnitTests.Features.Disputes;

[TestFixture]
public class GetDisputeByIdQueryHandlerTests
{
    private static readonly DateTime BaseDate = new(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task Handle_Given_The_Customers_Own_Dispute_Should_Return_It()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var transaction = customer.AddAccount().AddTransaction(BaseDate.AddDays(-2));
        var dispute = transaction.AddDispute(BaseDate, DisputeReason.Other, "I don't recognise this merchant.");

        using var context = await CreateContext(customer);
        var handler = CreateHandler(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new GetDisputeByIdQuery(dispute.Id), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Id.Should().Be(dispute.Id);
        actual.Reference.Should().Be(dispute.Reference);
        actual.TransactionId.Should().Be(transaction.Id);
        actual.Reason.Should().Be(DisputeReason.Other);
        actual.Description.Should().Be("I don't recognise this merchant.");
        actual.Status.Should().Be(DisputeStatus.Submitted);
        actual.SubmittedAt.Should().Be(BaseDate);
    }

    [Test]
    public async Task Handle_Given_A_Dispute_Should_Include_A_Summary_Of_The_Transaction_With_A_Masked_Account_Number()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var transaction = customer.AddAccount(AccountType.CreditCard, "4111222233334444").AddTransaction(BaseDate.AddDays(-2), merchant: "Takealot.com");
        var dispute = transaction.AddDispute(BaseDate);

        using var context = await CreateContext(customer);
        var handler = CreateHandler(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new GetDisputeByIdQuery(dispute.Id), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Transaction.Should().BeEquivalentTo(new
        {
            transaction.Id,
            transaction.Reference,
            AccountNumber = "******4444",
            AccountType = AccountType.CreditCard,
            transaction.Amount,
            transaction.Currency,
            Merchant = "Takealot.com",
            transaction.Category,
            transaction.TransactionDate
        });
    }

    [Test]
    public async Task Handle_Given_A_Dispute_With_Several_Status_Changes_Should_Return_The_Timeline_Oldest_First()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var dispute = customer.AddAccount().AddTransaction(BaseDate.AddDays(-2)).AddDispute(BaseDate);
        dispute.StartReview("Checking with the merchant.", "test.agent", BaseDate.AddDays(1));
        dispute.Resolve(DisputeOutcome.Approved, "Refunded.", "test.agent", BaseDate.AddDays(3));

        using var context = await CreateContext(customer);
        var handler = CreateHandler(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new GetDisputeByIdQuery(dispute.Id), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Status.Should().Be(DisputeStatus.Approved);
        actual.Timeline.Should().BeEquivalentTo(new[]
        {
            new { FromStatus = (DisputeStatus?)null, ToStatus = DisputeStatus.Submitted, Comment = (string?)null, ChangedBy = "test.customer", ChangedAt = BaseDate },
            new { FromStatus = (DisputeStatus?)DisputeStatus.Submitted, ToStatus = DisputeStatus.UnderReview, Comment = (string?)"Checking with the merchant.", ChangedBy = "test.agent", ChangedAt = BaseDate.AddDays(1) },
            new { FromStatus = (DisputeStatus?)DisputeStatus.UnderReview, ToStatus = DisputeStatus.Approved, Comment = (string?)"Refunded.", ChangedBy = "test.agent", ChangedAt = BaseDate.AddDays(3) }
        }, options => options.WithStrictOrdering());
    }

    private static async Task<TestDbContext> CreateContext(params Customer[] customers)
    {
        var context = TestDbContext.Create(Substitute.For<IDomainEventDispatcher>());
        context.Customers.AddRange(customers);
        await context.SaveChangesAsync();

        return context;
    }

    private static GetDisputeByIdQuery.GetDisputeByIdQueryHandler CreateHandler(IApplicationDbContext context, Guid customerId)
    {
        var currentCustomerService = Substitute.For<ICurrentCustomerService>();
        currentCustomerService.GetCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(customerId);

        return new GetDisputeByIdQuery.GetDisputeByIdQueryHandler(TestData.CreateMapper(), context, currentCustomerService);
    }
}
