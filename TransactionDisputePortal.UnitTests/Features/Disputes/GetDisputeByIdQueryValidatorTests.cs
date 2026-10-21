using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Common.Validators;
using TransactionDisputePortal.Application.Features.Disputes.Queries.GetDisputeById;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.UnitTests.TestSupport;

namespace TransactionDisputePortal.UnitTests.Features.Disputes;

[TestFixture]
public class GetDisputeByIdQueryValidatorTests
{
    private static readonly DateTime BaseDate = new(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task ValidateAsync_Given_The_Customers_Own_Dispute_Should_Be_Valid()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var dispute = customer.AddAccount().AddTransaction(BaseDate).AddDispute(BaseDate);

        using var context = await CreateContext(customer);
        var validator = CreateValidator(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await validator.ValidateAsync(new GetDisputeByIdQuery(dispute.Id));

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task ValidateAsync_Given_Another_Customers_Dispute_Should_Return_Not_Found_Error()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        customer.AddAccount().AddTransaction(BaseDate).AddDispute(BaseDate);

        var otherCustomer = TestData.CreateCustomer("Other Customer");
        var otherDispute = otherCustomer.AddAccount().AddTransaction(BaseDate).AddDispute(BaseDate);

        using var context = await CreateContext(customer, otherCustomer);
        var validator = CreateValidator(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await validator.ValidateAsync(new GetDisputeByIdQuery(otherDispute.Id));

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsValid.Should().BeFalse();
        actual.Errors.Should().ContainSingle(error => error.ErrorCode == ValidationErrorCodes.NotFound
            && error.ErrorMessage == $"Entity \"Dispute\" ({otherDispute.Id}) was not found.");
    }

    [Test]
    public async Task ValidateAsync_Given_An_Empty_Dispute_Id_Should_Return_Id_Error_Without_Checking_The_Database()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        using var context = await CreateContext();
        var currentCustomerService = Substitute.For<ICurrentCustomerService>();
        var validator = new GetDisputeByIdQueryValidator(context, currentCustomerService);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await validator.ValidateAsync(new GetDisputeByIdQuery(Guid.Empty));

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsValid.Should().BeFalse();
        actual.Errors.Should().ContainSingle(error => error.PropertyName == nameof(GetDisputeByIdQuery.Id)
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

    private static GetDisputeByIdQueryValidator CreateValidator(IApplicationDbContext context, Guid customerId)
    {
        var currentCustomerService = Substitute.For<ICurrentCustomerService>();
        currentCustomerService.GetCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(customerId);

        return new GetDisputeByIdQueryValidator(context, currentCustomerService);
    }
}
