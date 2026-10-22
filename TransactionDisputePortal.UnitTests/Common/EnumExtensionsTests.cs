using TransactionDisputePortal.Application.Common.Extensions;
using TransactionDisputePortal.Domain.Enums;

namespace TransactionDisputePortal.UnitTests.Common;

[TestFixture]
public class EnumExtensionsTests
{
    [TestCase(DisputeStatus.UnderReview, "Under review")]
    [TestCase(DisputeStatus.Approved, "Approved")]
    public void GetDisplayName_Given_A_Dispute_Status_With_A_Description_Should_Return_The_Description(DisputeStatus status, string expected)
    {
        //------------------------------------- Arrange ------------------------------------------------------

        //------------------------------------- Act     ------------------------------------------------------
        var actual = status.GetDisplayName();

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Should().Be(expected);
    }

    [Test]
    public void GetDisplayName_Given_A_Dispute_Reason_With_A_Description_Should_Return_The_Description()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var reason = DisputeReason.GoodsOrServicesNotReceived;

        //------------------------------------- Act     ------------------------------------------------------
        var actual = reason.GetDisplayName();

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Should().Be("Goods or services not received");
    }

    [Test]
    public void GetDisplayName_Given_A_Value_Without_A_Description_Should_Return_The_Value_Name()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var category = TransactionCategory.CashWithdrawal;

        //------------------------------------- Act     ------------------------------------------------------
        var actual = category.GetDisplayName();

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Should().Be("CashWithdrawal");
    }
}
