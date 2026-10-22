using TransactionDisputePortal.Application.Common.Models;
using TransactionDisputePortal.Application.Features.Lookups.Queries.GetLookups;
using TransactionDisputePortal.Domain.Enums;

namespace TransactionDisputePortal.UnitTests.Features.Lookups;

[TestFixture]
public class GetLookupsQueryHandlerTests
{
    [Test]
    public async Task Handle_Should_Return_Every_Dispute_Reason_In_Declaration_Order_With_Its_Display_Name()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var handler = CreateHandler();
        var expected = new List<LookupDTO<DisputeReason>>
        {
            new(DisputeReason.UnauthorisedTransaction, "Unauthorised transaction"),
            new(DisputeReason.DuplicateCharge, "Duplicate charge"),
            new(DisputeReason.IncorrectAmount, "Incorrect amount"),
            new(DisputeReason.GoodsOrServicesNotReceived, "Goods or services not received"),
            new(DisputeReason.NotAsDescribed, "Not as described"),
            new(DisputeReason.CancelledSubscription, "Cancelled subscription"),
            new(DisputeReason.RefundNotReceived, "Refund not received"),
            new(DisputeReason.Other, "Other")
        };

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new GetLookupsQuery(), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.DisputeReasons.Should().Equal(expected);
    }

    [Test]
    public async Task Handle_Should_Return_Every_Dispute_Status_In_Declaration_Order_With_Its_Display_Name()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var handler = CreateHandler();
        var expected = new List<LookupDTO<DisputeStatus>>
        {
            new(DisputeStatus.Submitted, "Submitted"),
            new(DisputeStatus.UnderReview, "Under review"),
            new(DisputeStatus.Approved, "Approved"),
            new(DisputeStatus.Rejected, "Rejected"),
            new(DisputeStatus.Withdrawn, "Withdrawn")
        };

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new GetLookupsQuery(), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.DisputeStatuses.Should().Equal(expected);
    }

    [Test]
    public async Task Handle_Should_Return_Every_Transaction_Status_In_Declaration_Order_With_Its_Display_Name()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var handler = CreateHandler();
        var expected = new List<LookupDTO<TransactionStatus>>
        {
            new(TransactionStatus.Pending, "Pending"),
            new(TransactionStatus.Posted, "Posted")
        };

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await handler.Handle(new GetLookupsQuery(), CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.TransactionStatuses.Should().Equal(expected);
    }

    private static GetLookupsQuery.GetLookupsQueryHandler CreateHandler()
    {
        return new GetLookupsQuery.GetLookupsQueryHandler();
    }
}
