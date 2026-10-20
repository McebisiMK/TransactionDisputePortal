using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Common.Validators;
using TransactionDisputePortal.Application.Features.Transactions.Queries.GetTransactionById;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.UnitTests.TestSupport;

namespace TransactionDisputePortal.UnitTests.Features.Transactions;

[TestFixture]
public class GetTransactionByIdQueryValidatorTests
{
    private static readonly DateTime BaseDate = new(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task ValidateAsync_Given_The_Customers_Own_Transaction_Should_Be_Valid()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var transaction = customer.AddAccount().AddTransaction(BaseDate);

        using var context = await CreateContext(customer);
        var validator = CreateValidator(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await validator.ValidateAsync(new GetTransactionByIdQuery(transaction.Id));

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task ValidateAsync_Given_Another_Customers_Transaction_Should_Return_Not_Found_Error()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        customer.AddAccount().AddTransaction(BaseDate);

        var otherCustomer = TestData.CreateCustomer("Other Customer");
        var otherTransaction = otherCustomer.AddAccount().AddTransaction(BaseDate);

        using var context = await CreateContext(customer, otherCustomer);
        var validator = CreateValidator(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await validator.ValidateAsync(new GetTransactionByIdQuery(otherTransaction.Id));

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsValid.Should().BeFalse();
        actual.Errors.Should().ContainSingle(error => error.ErrorCode == ValidationErrorCodes.NotFound
            && error.ErrorMessage == $"Entity \"Transaction\" ({otherTransaction.Id}) was not found.");
    }

    [Test]
    public async Task ValidateAsync_Given_A_Transaction_That_Does_Not_Exist_Should_Return_Not_Found_Error()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        customer.AddAccount().AddTransaction(BaseDate);

        using var context = await CreateContext(customer);
        var validator = CreateValidator(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await validator.ValidateAsync(new GetTransactionByIdQuery(Guid.NewGuid()));

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsValid.Should().BeFalse();
        actual.Errors.Should().ContainSingle(error => error.ErrorCode == ValidationErrorCodes.NotFound);
    }

    [Test]
    public async Task ValidateAsync_Given_An_Empty_Transaction_Id_Should_Return_Id_Error_Without_Checking_The_Database()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        using var context = await CreateContext();
        var currentCustomerService = Substitute.For<ICurrentCustomerService>();
        var validator = new GetTransactionByIdQueryValidator(context, currentCustomerService);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await validator.ValidateAsync(new GetTransactionByIdQuery(Guid.Empty));

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsValid.Should().BeFalse();
        actual.Errors.Should().ContainSingle(error => error.PropertyName == nameof(GetTransactionByIdQuery.Id)
            && error.ErrorCode != ValidationErrorCodes.NotFound);
        await currentCustomerService.DidNotReceiveWithAnyArgs().GetCustomerIdAsync(default);
    }

    private static async Task<TestDbContext> CreateContext(params Customer[] customers)
    {
        var context = TestDbContext.Create(Substitute.For<IDomainEventDispatcher>());
        context.Customers.AddRange(customers);
        await context.SaveChangesAsync();

        return context;
    }

    private static GetTransactionByIdQueryValidator CreateValidator(IApplicationDbContext context, Guid customerId)
    {
        var currentCustomerService = Substitute.For<ICurrentCustomerService>();
        currentCustomerService.GetCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(customerId);

        return new GetTransactionByIdQueryValidator(context, currentCustomerService);
    }
}
