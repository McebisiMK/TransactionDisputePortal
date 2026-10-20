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

    private static async Task<TestDbContext> CreateContext(params Customer[] customers)
    {
        var context = TestDbContext.Create(Substitute.For<IDomainEventDispatcher>());
        context.Customers.AddRange(customers);
        await context.SaveChangesAsync();

        return context;
    }

    private static GetTransactionByIdQuery.GetTransactionByIdQueryHandler CreateHandler(IApplicationDbContext context, Guid customerId)
    {
        var currentCustomerService = Substitute.For<ICurrentCustomerService>();
        currentCustomerService.GetCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(customerId);

        return new GetTransactionByIdQuery.GetTransactionByIdQueryHandler(TestData.CreateMapper(), context, currentCustomerService);
    }
}
