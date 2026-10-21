using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Features.Disputes.Queries.GetDisputeById;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Domain.Enums;
using TransactionDisputePortal.UnitTests.TestSupport;

namespace TransactionDisputePortal.UnitTests.Features.Disputes;

[TestFixture]
public class GetDisputeByIdQueryHandlerTests
{
    private static readonly DateTime BaseDate = new(2026, 9, 15, 10, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task Handle_Given_The_Customers_Own_Dispute_Should_Return_It()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var customer = TestData.CreateCustomer();
        var transaction = customer.AddAccount().AddTransaction(BaseDate.AddDays(-2));
        var dispute = transaction.AddDispute(BaseDate, DisputeReason.Other, "I don't recognise this merchant.");

        using var context = await CreateContext(customer);
        var handler = CreateHandler(context, customer.Id);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new GetDisputeByIdQuery(dispute.Id), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Id.Should().Be(dispute.Id);
        actual.Reference.Should().Be(dispute.Reference);
        actual.TransactionId.Should().Be(transaction.Id);
        actual.Reason.Should().Be(DisputeReason.Other);
        actual.Description.Should().Be("I don't recognise this merchant.");
        actual.Status.Should().Be(DisputeStatus.Submitted);
        actual.SubmittedAt.Should().Be(BaseDate);
    }

    private static async Task<TestDbContext> CreateContext(params Customer[] customers)
    {
        var context = TestDbContext.Create(Substitute.For<IDomainEventDispatcher>());
        context.Customers.AddRange(customers);
        await context.SaveChangesAsync();

        return context;
    }

    private static GetDisputeByIdQuery.GetDisputeByIdQueryHandler CreateHandler(IApplicationDbContext context, Guid customerId)
    {
        var currentCustomerService = Substitute.For<ICurrentCustomerService>();
        currentCustomerService.GetCustomerIdAsync(Arg.Any<CancellationToken>()).Returns(customerId);

        return new GetDisputeByIdQuery.GetDisputeByIdQueryHandler(TestData.CreateMapper(), context, currentCustomerService);
    }
}
