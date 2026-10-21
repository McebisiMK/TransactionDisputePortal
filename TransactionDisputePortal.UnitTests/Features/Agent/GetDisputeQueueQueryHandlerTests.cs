using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Features.Agent.Queries.GetDisputeQueue;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Domain.Enums;
using TransactionDisputePortal.UnitTests.TestSupport;

namespace TransactionDisputePortal.UnitTests.Features.Agent;

[TestFixture]
public class GetDisputeQueueQueryHandlerTests
{
    private static readonly DateTime BaseDate = new(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task Handle_Given_No_Status_Should_Return_Open_Disputes_Of_Every_Customer_Oldest_First()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var account = customer.AddAccount();
        var newest = account.AddTransaction(BaseDate.AddDays(-10)).AddDispute(BaseDate);
        var underReview = account.AddTransaction(BaseDate.AddDays(-30)).AddDispute(BaseDate.AddDays(-20));
        underReview.StartReview(null, "test.agent", BaseDate.AddDays(-19));
        var withdrawn = account.AddTransaction(BaseDate.AddDays(-40)).AddDispute(BaseDate.AddDays(-30));
        withdrawn.Withdraw(null, "test.customer", BaseDate.AddDays(-29));

        var otherCustomer = TestData.CreateCustomer("Other Customer");
        var otherCustomersDispute = otherCustomer.AddAccount().AddTransaction(BaseDate.AddDays(-15)).AddDispute(BaseDate.AddDays(-10));

        using var context = await CreateContext(customer, otherCustomer);
        var handler = new GetDisputeQueueQuery.GetDisputeQueueQueryHandler(TestData.CreateMapper(), context);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new GetDisputeQueueQuery(), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Items.Select(dispute => dispute.Id).Should().Equal(underReview.Id, otherCustomersDispute.Id, newest.Id);
        actual.TotalCount.Should().Be(3);
    }

    [Test]
    public async Task Handle_Given_A_Status_Should_Return_Only_Disputes_With_That_Status()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var account = customer.AddAccount();
        account.AddTransaction(BaseDate.AddDays(-10)).AddDispute(BaseDate);
        var withdrawn = account.AddTransaction(BaseDate.AddDays(-40)).AddDispute(BaseDate.AddDays(-30));
        withdrawn.Withdraw(null, "test.customer", BaseDate.AddDays(-29));

        using var context = await CreateContext(customer);
        var handler = new GetDisputeQueueQuery.GetDisputeQueueQueryHandler(TestData.CreateMapper(), context);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new GetDisputeQueueQuery { Status = DisputeStatus.Withdrawn }, CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Items.Should().ContainSingle().Which.Id.Should().Be(withdrawn.Id);
    }

    [Test]
    public async Task Handle_Given_A_Dispute_Should_Summarise_It_With_The_Customer_And_Transaction()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer("Thandi Nkosi");
        var transaction = customer.AddAccount().AddTransaction(BaseDate.AddDays(-5), merchant: "Takealot.com");
        var dispute = transaction.AddDispute(BaseDate.AddDays(-4), DisputeReason.IncorrectAmount);
        dispute.StartReview(null, "test.agent", BaseDate.AddDays(-2));

        using var context = await CreateContext(customer);
        var handler = new GetDisputeQueueQuery.GetDisputeQueueQueryHandler(TestData.CreateMapper(), context);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new GetDisputeQueueQuery(), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            dispute.Id,
            dispute.Reference,
            CustomerId = customer.Id,
            CustomerName = "Thandi Nkosi",
            TransactionReference = transaction.Reference,
            Merchant = "Takealot.com",
            transaction.Amount,
            transaction.Currency,
            Reason = DisputeReason.IncorrectAmount,
            Status = DisputeStatus.UnderReview,
            SubmittedAt = BaseDate.AddDays(-4),
            LastUpdatedAt = BaseDate.AddDays(-2)
        });
    }

    private static async Task<TestDbContext> CreateContext(params Customer[] customers)
    {
        var context = TestDbContext.Create(Substitute.For<IDomainEventDispatcher>());
        context.Customers.AddRange(customers);
        await context.SaveChangesAsync();

        return context;
    }
}
