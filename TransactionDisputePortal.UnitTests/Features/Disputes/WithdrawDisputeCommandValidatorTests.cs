using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Common.Validators;
using TransactionDisputePortal.Application.Features.Disputes.Commands.WithdrawDispute;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.UnitTests.TestSupport;

namespace TransactionDisputePortal.UnitTests.Features.Disputes;

[TestFixture]
public class WithdrawDisputeCommandValidatorTests
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
        var actual = await validator.ValidateAsync(new WithdrawDisputeCommand(dispute.Id, "No longer needed."));

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task ValidateAsync_Given_Another_Customers_Dispute_Should_Return_Not_Found_Error()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var otherCustomer = TestData.CreateCustomer("Other Customer");
        var otherDispute = otherCustomer.AddAccount().AddTransaction(BaseDate).AddDispute(BaseDate);

        using var context = await CreateContext(customer, otherCustomer);
        var validator = CreateValidator(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await validator.ValidateAsync(new WithdrawDisputeCommand(otherDispute.Id, null));

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
        var validator = new WithdrawDisputeCommandValidator(context, currentCustomerService);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await validator.ValidateAsync(new WithdrawDisputeCommand(Guid.Empty, null));

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsValid.Should().BeFalse();
        actual.Errors.Should().ContainSingle(error => error.PropertyName == nameof(WithdrawDisputeCommand.Id)
            && error.ErrorCode != ValidationErrorCodes.NotFound);
        await currentCustomerService.DidNotReceiveWithAnyArgs().GetCustomerIdAsync(default);
    }

    [Test]
    public async Task ValidateAsync_Given_A_Comment_That_Is_Too_Long_Should_Return_Comment_Error()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var dispute = customer.AddAccount().AddTransaction(BaseDate).AddDispute(BaseDate);

        using var context = await CreateContext(customer);
        var validator = CreateValidator(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await validator.ValidateAsync(new WithdrawDisputeCommand(dispute.Id, new string('a', DisputeStatusHistory.CommentMaxLength + 1)));

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsValid.Should().BeFalse();
        actual.Errors.Should().ContainSingle(error => error.PropertyName == nameof(WithdrawDisputeCommand.Comment));
    }

    private static async Task<TestDbContext> CreateContext(params Customer[] customers)
    {
        var context = TestDbContext.Create(Substitute.For<IDomainEventDispatcher>());
        context.Customers.AddRange(customers);
        await context.SaveChangesAsync();

        return context;
    }

    private static WithdrawDisputeCommandValidator CreateValidator(IApplicationDbContext context, Guid customerId)
    {
        var currentCustomerService = Substitute.For<ICurrentCustomerService>();
        currentCustomerService.GetCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(customerId);

        return new WithdrawDisputeCommandValidator(context, currentCustomerService);
    }
}
