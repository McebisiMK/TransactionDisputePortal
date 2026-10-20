using TransactionDisputePortal.Application.Common.Extensions;

namespace TransactionDisputePortal.UnitTests.Common;

[TestFixture]
public class QueryableExtensionsTests
{
    [Test]
    public void WhereIf_Given_The_Condition_Is_True_Should_Apply_The_Filter()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var source = new[] { 1, 2, 3, 4 }.AsQueryable();

        //------------------------------------- Act     ------------------------------------------------------
        var actual = source.WhereIf(true, number => number > 2).ToList();

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Should().Equal(3, 4);
    }

    [Test]
    public void WhereIf_Given_The_Condition_Is_False_Should_Return_The_Source_Unfiltered()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var source = new[] { 1, 2, 3, 4 }.AsQueryable();

        //------------------------------------- Act     ------------------------------------------------------
        var actual = source.WhereIf(false, number => number > 2);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Should().BeSameAs(source);
    }
}
