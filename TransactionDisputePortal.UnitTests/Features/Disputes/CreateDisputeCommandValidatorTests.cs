using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Common.Validators;
using TransactionDisputePortal.Application.Features.Disputes.Commands.CreateDispute;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Domain.Enums;
using TransactionDisputePortal.UnitTests.TestSupport;

namespace TransactionDisputePortal.UnitTests.Features.Disputes;

[TestFixture]
public class CreateDisputeCommandValidatorTests
{
    private static readonly DateTime BaseDate = new(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task ValidateAsync_Given_A_Valid_Command_For_The_Customers_Own_Transaction_Should_Be_Valid()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var transaction = customer.AddAccount().AddTransaction(BaseDate);

        using var context = await CreateContext(customer);
        var validator = CreateValidator(context, customer.Id);
        var command = new CreateDisputeCommand { TransactionId = transaction.Id, Reason = DisputeReason.DuplicateCharge };

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await validator.ValidateAsync(command);

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
        var command = new CreateDisputeCommand { TransactionId = otherTransaction.Id, Reason = DisputeReason.DuplicateCharge };

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await validator.ValidateAsync(command);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsValid.Should().BeFalse();
        actual.Errors.Should().ContainSingle(error => error.ErrorCode == ValidationErrorCodes.NotFound
            && error.ErrorMessage == $"Entity \"Transaction\" ({otherTransaction.Id}) was not found.");
    }

    [Test]
    public async Task ValidateAsync_Given_An_Empty_Transaction_Id_Should_Return_Transaction_Id_Error_Without_Checking_The_Database()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        using var context = await CreateContext();
        var currentCustomerService = Substitute.For<ICurrentCustomerService>();
        var validator = new CreateDisputeCommandValidator(context, currentCustomerService);
        var command = new CreateDisputeCommand { TransactionId = Guid.Empty, Reason = DisputeReason.DuplicateCharge };

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await validator.ValidateAsync(command);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsValid.Should().BeFalse();
        actual.Errors.Should().ContainSingle(error => error.PropertyName == nameof(CreateDisputeCommand.TransactionId)
            && error.ErrorCode != ValidationErrorCodes.NotFound);
        await currentCustomerService.DidNotReceiveWithAnyArgs().GetCustomerIdAsync(default);
    }

    [TestCase(null)]
    [TestCase((DisputeReason)99)]
    public async Task ValidateAsync_Given_A_Missing_Or_Undefined_Reason_Should_Return_Reason_Error(DisputeReason? reason)
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var transaction = customer.AddAccount().AddTransaction(BaseDate);

        using var context = await CreateContext(customer);
        var validator = CreateValidator(context, customer.Id);
        var command = new CreateDisputeCommand { TransactionId = transaction.Id, Reason = reason };

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await validator.ValidateAsync(command);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsValid.Should().BeFalse();
        actual.Errors.Should().ContainSingle(error => error.PropertyName == nameof(CreateDisputeCommand.Reason));
    }

    [TestCase(null)]
    [TestCase("   ")]
    public async Task ValidateAsync_Given_Reason_Other_Without_A_Description_Should_Return_Description_Error(string? description)
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var transaction = customer.AddAccount().AddTransaction(BaseDate);

        using var context = await CreateContext(customer);
        var validator = CreateValidator(context, customer.Id);
        var command = new CreateDisputeCommand { TransactionId = transaction.Id, Reason = DisputeReason.Other, Description = description };

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await validator.ValidateAsync(command);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsValid.Should().BeFalse();
        actual.Errors.Should().ContainSingle(error => error.PropertyName == nameof(CreateDisputeCommand.Description)
            && error.ErrorMessage == "'Description' is required when the reason is 'Other'.");
    }

    [Test]
    public async Task ValidateAsync_Given_A_Description_Over_The_Maximum_Length_Should_Return_Description_Error()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var transaction = customer.AddAccount().AddTransaction(BaseDate);

        using var context = await CreateContext(customer);
        var validator = CreateValidator(context, customer.Id);
        var command = new CreateDisputeCommand
        {
            TransactionId = transaction.Id,
            Reason = DisputeReason.DuplicateCharge,
            Description = new string('a', Dispute.DescriptionMaxLength + 1)
        };

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await validator.ValidateAsync(command);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsValid.Should().BeFalse();
        actual.Errors.Should().ContainSingle(error => error.PropertyName == nameof(CreateDisputeCommand.Description));
    }

    private static async Task<TestDbContext> CreateContext(params Customer[] customers)
    {
        var context = TestDbContext.Create(Substitute.For<IDomainEventDispatcher>());
        context.Customers.AddRange(customers);
        await context.SaveChangesAsync();

        return context;
    }

    private static CreateDisputeCommandValidator CreateValidator(IApplicationDbContext context, Guid customerId)
    {
        var currentCustomerService = Substitute.For<ICurrentCustomerService>();
        currentCustomerService.GetCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(customerId);

        return new CreateDisputeCommandValidator(context, currentCustomerService);
    }
}
