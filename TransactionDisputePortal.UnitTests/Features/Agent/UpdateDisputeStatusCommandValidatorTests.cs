using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Common.Validators;
using TransactionDisputePortal.Application.Features.Agent.Commands.UpdateDisputeStatus;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Domain.Enums;
using TransactionDisputePortal.UnitTests.TestSupport;

namespace TransactionDisputePortal.UnitTests.Features.Agent;

[TestFixture]
public class UpdateDisputeStatusCommandValidatorTests
{
    private static readonly DateTime BaseDate = new(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc);

    private Dispute dispute = null!;
    private TestDbContext context = null!;
    private UpdateDisputeStatusCommandValidator validator = null!;

    [SetUp]
    public async Task SetUp()
    {
        var customer = TestData.CreateCustomer();
        dispute = customer.AddAccount().AddTransaction(BaseDate).AddDispute(BaseDate);

        context = TestDbContext.Create(Substitute.For<IDomainEventDispatcher>());
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        validator = new UpdateDisputeStatusCommandValidator(context);
    }

    [TearDown]
    public void TearDown() => context.Dispose();

    [TestCase(DisputeStatus.UnderReview, null)]
    [TestCase(DisputeStatus.Approved, "Refunded.")]
    [TestCase(DisputeStatus.Rejected, "The charge was correct.")]
    public async Task ValidateAsync_Given_A_Status_An_Agent_Can_Set_Should_Be_Valid(DisputeStatus status, string? comment)
    {
        //------------------------------------- Act     ------------------------------------------------------
        var actual = await validator.ValidateAsync(new UpdateDisputeStatusCommand(dispute.Id, status, comment, []));

        //------------------------------------- Assert  ------------------------------------------------------
        actual.IsValid.Should().BeTrue();
    }

    [Test]
    public async Task ValidateAsync_Given_An_Unknown_Dispute_Should_Return_Not_Found_Error()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var id = Guid.NewGuid();

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await validator.ValidateAsync(new UpdateDisputeStatusCommand(id, DisputeStatus.UnderReview, null, []));

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Errors.Should().ContainSingle(error => error.ErrorCode == ValidationErrorCodes.NotFound
            && error.ErrorMessage == $"Entity \"Dispute\" ({id}) was not found.");
    }

    [TestCase(DisputeStatus.Submitted)]
    [TestCase(DisputeStatus.Withdrawn)]
    [TestCase((DisputeStatus)99)]
    [TestCase(null)]
    public async Task ValidateAsync_Given_A_Status_An_Agent_Cannot_Set_Should_Return_Status_Error(DisputeStatus? status)
    {
        //------------------------------------- Act     ------------------------------------------------------
        var actual = await validator.ValidateAsync(new UpdateDisputeStatusCommand(dispute.Id, status, "Comment.", []));

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Errors.Should().ContainSingle(error => error.PropertyName == nameof(UpdateDisputeStatusCommand.Status));
    }

    [TestCase(DisputeStatus.Approved, null)]
    [TestCase(DisputeStatus.Rejected, "   ")]
    public async Task ValidateAsync_Given_A_Resolution_Without_A_Comment_Should_Return_Comment_Error(DisputeStatus status, string? comment)
    {
        //------------------------------------- Act     ------------------------------------------------------
        var actual = await validator.ValidateAsync(new UpdateDisputeStatusCommand(dispute.Id, status, comment, []));

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Errors.Should().ContainSingle(error => error.PropertyName == nameof(UpdateDisputeStatusCommand.Comment)
            && error.ErrorMessage == "'Comment' is required when resolving a dispute.");
    }

    [Test]
    public async Task ValidateAsync_Given_A_Comment_That_Is_Too_Long_Should_Return_Comment_Error()
    {
        //------------------------------------- Act     ------------------------------------------------------
        var actual = await validator.ValidateAsync(new UpdateDisputeStatusCommand(dispute.Id, DisputeStatus.UnderReview,
            new string('a', DisputeStatusHistory.CommentMaxLength + 1), []));

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Errors.Should().ContainSingle(error => error.PropertyName == nameof(UpdateDisputeStatusCommand.Comment));
    }

    [Test]
    public async Task ValidateAsync_Given_No_Row_Version_Should_Return_Row_Version_Error()
    {
        //------------------------------------- Act     ------------------------------------------------------
        var actual = await validator.ValidateAsync(new UpdateDisputeStatusCommand(dispute.Id, DisputeStatus.UnderReview, null, null));

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Errors.Should().ContainSingle(error => error.PropertyName == nameof(UpdateDisputeStatusCommand.RowVersion));
    }
}
