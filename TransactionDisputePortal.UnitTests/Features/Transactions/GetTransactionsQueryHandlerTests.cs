using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Features.Transactions.Queries.GetTransactions;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Domain.Enums;
using TransactionDisputePortal.UnitTests.TestSupport;

namespace TransactionDisputePortal.UnitTests.Features.Transactions;

[TestFixture]
public class GetTransactionsQueryHandlerTests
{
    private static readonly DateTime BaseDate = new(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task Handle_Given_Transactions_Of_Several_Customers_Should_Return_Only_The_Current_Customers_Newest_First()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var chequeAccount = customer.AddAccount(AccountType.Cheque);
        var creditCardAccount = customer.AddAccount(AccountType.CreditCard);
        var oldest = chequeAccount.AddTransaction(BaseDate.AddDays(-10));
        var newest = creditCardAccount.AddTransaction(BaseDate);
        var middle = chequeAccount.AddTransaction(BaseDate.AddDays(-5));

        var otherCustomer = TestData.CreateCustomer("Other Customer");
        otherCustomer.AddAccount().AddTransaction(BaseDate.AddDays(-1));

        using var context = await CreateContext(customer, otherCustomer);
        var handler = CreateHandler(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new GetTransactionsQuery(), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Items.Select(transaction => transaction.Id).Should().Equal(newest.Id, middle.Id, oldest.Id);
        actual.TotalCount.Should().Be(3);
    }

    [Test]
    public async Task Handle_Given_A_Date_Range_Should_Return_Transactions_From_The_Start_Of_The_First_Day_To_The_End_Of_The_Last_Day_In_South_African_Time()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        // South African time is UTC+2, so the 10th starts at 22:00 UTC on the 9th and the 12th ends at 22:00 UTC on the 12th.
        var customer = TestData.CreateCustomer();
        var account = customer.AddAccount();
        account.AddTransaction(new DateTime(2026, 9, 9, 21, 59, 59, DateTimeKind.Utc));
        var firstDay = account.AddTransaction(new DateTime(2026, 9, 9, 22, 0, 0, DateTimeKind.Utc));
        var lastDay = account.AddTransaction(new DateTime(2026, 9, 12, 21, 59, 59, DateTimeKind.Utc));
        account.AddTransaction(new DateTime(2026, 9, 12, 22, 0, 0, DateTimeKind.Utc));

        using var context = await CreateContext(customer);
        var handler = CreateHandler(context, customer.Id);
        var query = new GetTransactionsQuery { FromDate = new DateOnly(2026, 9, 10), ToDate = new DateOnly(2026, 9, 12) };

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(query, CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Items.Select(transaction => transaction.Id).Should().Equal(lastDay.Id, firstDay.Id);
    }

    [Test]
    public async Task Handle_Given_A_Status_Filter_Should_Return_Only_Transactions_With_That_Status()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var account = customer.AddAccount();
        var pending = account.AddTransaction(BaseDate, status: TransactionStatus.Pending);
        account.AddTransaction(BaseDate.AddDays(-3), status: TransactionStatus.Posted);

        using var context = await CreateContext(customer);
        var handler = CreateHandler(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new GetTransactionsQuery { Status = TransactionStatus.Pending }, CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Items.Should().ContainSingle(transaction => transaction.Id == pending.Id);
    }

    [TestCase("Takealot")]
    [TestCase("TXN-SEARCH")]
    public async Task Handle_Given_A_Search_Term_Should_Match_Part_Of_The_Merchant_Or_Reference(string search)
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var account = customer.AddAccount();
        var expected = account.AddTransaction(BaseDate, merchant: "Takealot.com", reference: "TXN-SEARCH-0001");
        account.AddTransaction(BaseDate.AddDays(-1), merchant: "Woolworths Food");

        using var context = await CreateContext(customer);
        var handler = CreateHandler(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new GetTransactionsQuery { Search = $"  {search} " }, CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Items.Should().ContainSingle(transaction => transaction.Id == expected.Id);
    }

    [Test]
    public async Task Handle_Given_An_Account_Filter_Should_Return_Only_That_Accounts_Transactions()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var chequeAccount = customer.AddAccount(AccountType.Cheque);
        var creditCardAccount = customer.AddAccount(AccountType.CreditCard);
        chequeAccount.AddTransaction(BaseDate);
        var expected = creditCardAccount.AddTransaction(BaseDate.AddDays(-1));

        using var context = await CreateContext(customer);
        var handler = CreateHandler(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new GetTransactionsQuery { AccountId = creditCardAccount.Id }, CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Items.Should().ContainSingle(transaction => transaction.Id == expected.Id);
    }

    [Test]
    public async Task Handle_Given_An_Account_Filter_For_Another_Customers_Account_Should_Return_Nothing()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        customer.AddAccount().AddTransaction(BaseDate);

        var otherCustomer = TestData.CreateCustomer("Other Customer");
        var otherAccount = otherCustomer.AddAccount();
        otherAccount.AddTransaction(BaseDate);

        using var context = await CreateContext(customer, otherCustomer);
        var handler = CreateHandler(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new GetTransactionsQuery { AccountId = otherAccount.Id }, CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Items.Should().BeEmpty();
        actual.TotalCount.Should().Be(0);
    }

    [Test]
    public async Task Handle_Given_A_Page_Is_Requested_Should_Return_That_Page_And_The_Totals()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var account = customer.AddAccount();
        var transactions = Enumerable.Range(0, 7).Select(day => account.AddTransaction(BaseDate.AddDays(-day))).ToList();

        using var context = await CreateContext(customer);
        var handler = CreateHandler(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new GetTransactionsQuery { PageNumber = 2, PageSize = 3 }, CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Items.Select(transaction => transaction.Id).Should().Equal(transactions[3].Id, transactions[4].Id, transactions[5].Id);
        actual.TotalCount.Should().Be(7);
        actual.TotalPages.Should().Be(3);
        actual.HasPreviousPage.Should().BeTrue();
        actual.HasNextPage.Should().BeTrue();
    }

    private static async Task<TestDbContext> CreateContext(params Customer[] customers)
    {
        var context = TestDbContext.Create(Substitute.For<IDomainEventDispatcher>());
        context.Customers.AddRange(customers);
        await context.SaveChangesAsync();

        return context;
    }

    private static GetTransactionsQuery.GetTransactionsQueryHandler CreateHandler(IApplicationDbContext context, Guid customerId)
    {
        var currentCustomerService = Substitute.For<ICurrentCustomerService>();
        currentCustomerService.GetCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(customerId);

        return new GetTransactionsQuery.GetTransactionsQueryHandler(TestData.CreateMapper(), context, currentCustomerService);
    }
}
