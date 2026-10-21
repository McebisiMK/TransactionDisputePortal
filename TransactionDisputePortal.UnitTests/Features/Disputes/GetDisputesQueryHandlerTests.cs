using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Features.Disputes.Queries.GetDisputes;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Domain.Enums;
using TransactionDisputePortal.UnitTests.TestSupport;

namespace TransactionDisputePortal.UnitTests.Features.Disputes;

[TestFixture]
public class GetDisputesQueryHandlerTests
{
    private static readonly DateTime BaseDate = new(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task Handle_Given_Disputes_Of_Several_Customers_Should_Return_Only_The_Current_Customers_Newest_First()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var account = customer.AddAccount();
        var oldest = account.AddTransaction(BaseDate.AddDays(-30)).AddDispute(BaseDate.AddDays(-20));
        var newest = account.AddTransaction(BaseDate.AddDays(-10)).AddDispute(BaseDate);
        var middle = account.AddTransaction(BaseDate.AddDays(-20)).AddDispute(BaseDate.AddDays(-10));

        var otherCustomer = TestData.CreateCustomer("Other Customer");
        otherCustomer.AddAccount().AddTransaction(BaseDate.AddDays(-5)).AddDispute(BaseDate.AddDays(-1));

        using var context = await CreateContext(customer, otherCustomer);
        var handler = CreateHandler(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new GetDisputesQuery(), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Items.Select(dispute => dispute.Id).Should().Equal(newest.Id, middle.Id, oldest.Id);
        actual.TotalCount.Should().Be(3);
    }

    [Test]
    public async Task Handle_Given_A_Dispute_Should_Summarise_It_With_Its_Transaction_And_Latest_Status_Change()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var transaction = customer.AddAccount().AddTransaction(BaseDate.AddDays(-5), merchant: "Takealot.com");
        var dispute = transaction.AddDispute(BaseDate.AddDays(-4), DisputeReason.IncorrectAmount);
        dispute.StartReview(null, "test.agent", BaseDate.AddDays(-2));

        using var context = await CreateContext(customer);
        var handler = CreateHandler(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new GetDisputesQuery(), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            dispute.Id,
            dispute.Reference,
            TransactionId = transaction.Id,
            TransactionReference = transaction.Reference,
            Merchant = "Takealot.com",
            transaction.Amount,
            transaction.Currency,
            transaction.TransactionDate,
            Reason = DisputeReason.IncorrectAmount,
            Status = DisputeStatus.UnderReview,
            SubmittedAt = BaseDate.AddDays(-4),
            LastUpdatedAt = BaseDate.AddDays(-2)
        });
    }

    [Test]
    public async Task Handle_Given_A_Status_Filter_Should_Return_Only_Disputes_With_That_Status()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var account = customer.AddAccount();
        account.AddTransaction(BaseDate.AddDays(-5)).AddDispute(BaseDate);
        var withdrawn = account.AddTransaction(BaseDate.AddDays(-6)).AddDispute(BaseDate);
        withdrawn.Withdraw(null, "test.customer", BaseDate.AddHours(1));

        using var context = await CreateContext(customer);
        var handler = CreateHandler(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new GetDisputesQuery { Status = DisputeStatus.Withdrawn }, CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Items.Should().ContainSingle(dispute => dispute.Id == withdrawn.Id);
    }

    [Test]
    public async Task Handle_Given_A_Date_Range_Should_Filter_On_The_Submission_Date_In_South_African_Time()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        // South African time is UTC+2, so the 10th starts at 22:00 UTC on the 9th and the 12th ends at 22:00 UTC on the 12th.
        var customer = TestData.CreateCustomer();
        var account = customer.AddAccount();
        account.AddTransaction(BaseDate.AddDays(-20)).AddDispute(new DateTime(2026, 9, 9, 21, 59, 59, DateTimeKind.Utc));
        var firstDay = account.AddTransaction(BaseDate.AddDays(-20)).AddDispute(new DateTime(2026, 9, 9, 22, 0, 0, DateTimeKind.Utc));
        var lastDay = account.AddTransaction(BaseDate.AddDays(-20)).AddDispute(new DateTime(2026, 9, 12, 21, 59, 59, DateTimeKind.Utc));
        account.AddTransaction(BaseDate.AddDays(-20)).AddDispute(new DateTime(2026, 9, 12, 22, 0, 0, DateTimeKind.Utc));

        using var context = await CreateContext(customer);
        var handler = CreateHandler(context, customer.Id);
        var query = new GetDisputesQuery { FromDate = new DateOnly(2026, 9, 10), ToDate = new DateOnly(2026, 9, 12) };

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(query, CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Items.Select(dispute => dispute.Id).Should().Equal(lastDay.Id, firstDay.Id);
    }

    [Test]
    public async Task Handle_Given_A_Page_Is_Requested_Should_Return_That_Page_And_The_Totals()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var account = customer.AddAccount();
        var disputes = Enumerable.Range(0, 5).Select(day => account.AddTransaction(BaseDate.AddDays(-10)).AddDispute(BaseDate.AddDays(-day))).ToList();

        using var context = await CreateContext(customer);
        var handler = CreateHandler(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new GetDisputesQuery { PageNumber = 2, PageSize = 2 }, CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Items.Select(dispute => dispute.Id).Should().Equal(disputes[2].Id, disputes[3].Id);
        actual.TotalCount.Should().Be(5);
        actual.TotalPages.Should().Be(3);
    }

    [Test]
    public async Task Handle_Given_The_Customer_Has_No_Disputes_Should_Return_An_Empty_Page()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        customer.AddAccount().AddTransaction(BaseDate);

        using var context = await CreateContext(customer);
        var handler = CreateHandler(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new GetDisputesQuery(), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Items.Should().BeEmpty();
        actual.TotalCount.Should().Be(0);
    }

    private static async Task<TestDbContext> CreateContext(params Customer[] customers)
    {
        var context = TestDbContext.Create(Substitute.For<IDomainEventDispatcher>());
        context.Customers.AddRange(customers);
        await context.SaveChangesAsync();

        return context;
    }

    private static GetDisputesQuery.GetDisputesQueryHandler CreateHandler(IApplicationDbContext context, Guid customerId)
    {
        var currentCustomerService = Substitute.For<ICurrentCustomerService>();
        currentCustomerService.GetCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(customerId);

        return new GetDisputesQuery.GetDisputesQueryHandler(TestData.CreateMapper(), context, currentCustomerService);
    }
}
