using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Common.Validators;
using TransactionDisputePortal.Application.Features.Agent.Queries.GetAgentDisputeById;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Domain.Enums;
using TransactionDisputePortal.UnitTests.TestSupport;

namespace TransactionDisputePortal.UnitTests.Features.Agent;

[TestFixture]
public class GetAgentDisputeByIdQueryTests
{
    private static readonly DateTime BaseDate = new(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task Handle_Given_Any_Customers_Dispute_Should_Return_It_With_The_Customer_Transaction_And_Timeline()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer("Thandi Nkosi");
        var transaction = customer.AddAccount(accountNumber: "4111222233334444").AddTransaction(BaseDate.AddDays(-5));
        var dispute = transaction.AddDispute(BaseDate.AddDays(-4), DisputeReason.Other, "I never received the order.");
        dispute.StartReview("Contacted the merchant.", "test.agent", BaseDate.AddDays(-2));

        using var context = await CreateContext(customer);
        var handler = new GetAgentDisputeByIdQuery.GetAgentDisputeByIdQueryHandler(TestData.CreateMapper(), context);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new GetAgentDisputeByIdQuery(dispute.Id), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Should().BeEquivalentTo(new
        {
            dispute.Id,
            CustomerId = customer.Id,
            CustomerName = "Thandi Nkosi",
            CustomerEmail = customer.Email,
            Description = "I never received the order.",
            Status = DisputeStatus.UnderReview
        }, options => options.ExcludingMissingMembers());
        actual.Transaction.AccountNumber.Should().Be("******4444");
        actual.Timeline.Select(history => history.ToStatus).Should().Equal(DisputeStatus.Submitted, DisputeStatus.UnderReview);
    }

    [Test]
    public async Task ValidateAsync_Given_An_Unknown_Dispute_Should_Return_Not_Found_Error()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        using var context = await CreateContext();
        var validator = new GetAgentDisputeByIdQueryValidator(context);
        var id = Guid.NewGuid();

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await validator.ValidateAsync(new GetAgentDisputeByIdQuery(id));

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Errors.Should().ContainSingle(error => error.ErrorCode == ValidationErrorCodes.NotFound
            && error.ErrorMessage == $"Entity \"Dispute\" ({id}) was not found.");
    }

    private static async Task<TestDbContext> CreateContext(params Customer[] customers)
    {
        var context = TestDbContext.Create(Substitute.For<IDomainEventDispatcher>());
        context.Customers.AddRange(customers);
        await context.SaveChangesAsync();

        return context;
    }
}
