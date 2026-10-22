using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Features.Transactions.Queries.GetTransactionById;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Domain.Enums;
using TransactionDisputePortal.UnitTests.TestSupport;

namespace TransactionDisputePortal.UnitTests.Features.Transactions;

[TestFixture]
public class GetTransactionByIdQueryHandlerTests
{
    private static readonly DateTime BaseDate = new(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task Handle_Given_The_Customers_Own_Transaction_Should_Return_It_With_Account_Details()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var account = customer.AddAccount(AccountType.CreditCard, accountNumber: "4111222233334444");
        var transaction = account.AddTransaction(BaseDate, merchant: "Takealot.com", status: TransactionStatus.Pending);

        using var context = await CreateContext(customer);
        var handler = CreateHandler(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new GetTransactionByIdQuery(transaction.Id), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Id.Should().Be(transaction.Id);
        actual.Reference.Should().Be(transaction.Reference);
        actual.AccountId.Should().Be(account.Id);
        actual.AccountNumber.Should().Be("******4444");
        actual.AccountType.Should().Be(AccountType.CreditCard);
        actual.Amount.Should().Be(transaction.Amount);
        actual.Currency.Should().Be(transaction.Currency);
        actual.Merchant.Should().Be("Takealot.com");
        actual.Category.Should().Be(transaction.Category);
        actual.TransactionDate.Should().Be(BaseDate);
        actual.Status.Should().Be(TransactionStatus.Pending);
    }

    [TestCase("1234", "******")]
    [TestCase("12345", "******2345")]
    [TestCase("******8834", "******8834")]
    public async Task Handle_Given_An_Account_Number_Should_Only_Return_The_Last_Four_Digits(string accountNumber, string expected)
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var transaction = customer.AddAccount(accountNumber: accountNumber).AddTransaction(BaseDate);

        using var context = await CreateContext(customer);
        var handler = CreateHandler(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new GetTransactionByIdQuery(transaction.Id), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.AccountNumber.Should().Be(expected);
    }

    [TestCase(0)]
    [TestCase(Dispute.DisputeWindowDays)]
    public async Task Handle_Given_A_Posted_Transaction_Inside_The_Dispute_Window_Should_Be_Disputable(int daysAgo)
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var transaction = customer.AddAccount().AddTransaction(BaseDate.AddDays(-daysAgo));

        using var context = await CreateContext(customer);
        var handler = CreateHandler(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new GetTransactionByIdQuery(transaction.Id), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsDisputable.Should().BeTrue();
        actual.OpenDisputeId.Should().BeNull();
    }

    [Test]
    public async Task Handle_Given_A_Posted_Transaction_Older_Than_The_Dispute_Window_Should_Not_Be_Disputable()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var transaction = customer.AddAccount().AddTransaction(BaseDate.AddDays(-Dispute.DisputeWindowDays).AddSeconds(-1));

        using var context = await CreateContext(customer);
        var handler = CreateHandler(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new GetTransactionByIdQuery(transaction.Id), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsDisputable.Should().BeFalse();
    }

    [Test]
    public async Task Handle_Given_A_Pending_Transaction_Should_Not_Be_Disputable()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var transaction = customer.AddAccount().AddTransaction(BaseDate, status: TransactionStatus.Pending);

        using var context = await CreateContext(customer);
        var handler = CreateHandler(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new GetTransactionByIdQuery(transaction.Id), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsDisputable.Should().BeFalse();
    }

    [Test]
    public async Task Handle_Given_A_Transaction_With_An_Open_Dispute_Should_Not_Be_Disputable_And_Return_The_Open_Dispute()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var transaction = customer.AddAccount().AddTransaction(BaseDate.AddDays(-1));
        var dispute = transaction.AddDispute(BaseDate);

        using var context = await CreateContext(customer);
        var handler = CreateHandler(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new GetTransactionByIdQuery(transaction.Id), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsDisputable.Should().BeFalse();
        actual.OpenDisputeId.Should().Be(dispute.Id);
    }

    [TestCase(DisputeOutcome.Approved, false)]
    [TestCase(DisputeOutcome.Rejected, true)]
    public async Task Handle_Given_A_Transaction_With_A_Resolved_Dispute_Should_Only_Be_Disputable_When_It_Was_Rejected(DisputeOutcome outcome, bool expected)
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var transaction = customer.AddAccount().AddTransaction(BaseDate.AddDays(-10));
        var dispute = transaction.AddDispute(BaseDate.AddDays(-5));
        dispute.StartReview(null, "test.agent", BaseDate.AddDays(-4));
        dispute.Resolve(outcome, "Merchant replied.", "test.agent", BaseDate.AddDays(-3));

        using var context = await CreateContext(customer);
        var handler = CreateHandler(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new GetTransactionByIdQuery(transaction.Id), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsDisputable.Should().Be(expected);
        actual.OpenDisputeId.Should().BeNull();
    }

    [Test]
    public async Task Handle_Given_Time_Has_Moved_On_Past_The_Dispute_Window_Should_No_Longer_Be_Disputable()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var transaction = customer.AddAccount().AddTransaction(BaseDate);

        using var context = await CreateContext(customer);
        var handler = CreateHandler(context, customer.Id, utcNow: BaseDate.AddDays(Dispute.DisputeWindowDays + 1));

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new GetTransactionByIdQuery(transaction.Id), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsDisputable.Should().BeFalse();
    }

    private static async Task<TestDbContext> CreateContext(params Customer[] customers)
    {
        var context = TestDbContext.Create(Substitute.For<IDomainEventDispatcher>());
        context.Customers.AddRange(customers);
        await context.SaveChangesAsync();

        return context;
    }

    private static GetTransactionByIdQuery.GetTransactionByIdQueryHandler CreateHandler(IApplicationDbContext context, Guid customerId, DateTime? utcNow = null)
    {
        var currentCustomerService = Substitute.For<ICurrentCustomerService>();
        currentCustomerService.GetCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(customerId);

        return new GetTransactionByIdQuery.GetTransactionByIdQueryHandler(TestData.CreateMapper(), context, currentCustomerService, TestData.CreateTimeProvider(utcNow ?? BaseDate));
    }
}
