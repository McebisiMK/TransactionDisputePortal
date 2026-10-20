using TransactionDisputePortal.Application.Common.Extensions;
using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Common.Models;
using TransactionDisputePortal.UnitTests.TestSupport;

namespace TransactionDisputePortal.UnitTests.Common;

[TestFixture]
public class PaginatedListTests
{
    private sealed record TestPagedQuery : PagedQuery;

    [Test]
    public async Task CreateAsync_Given_A_Middle_Page_Is_Requested_Should_Return_That_Page_And_Totals()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var expectedItems = Enumerable.Range(11, 10).Select(i => $"Item {i:D2}").ToList();
        using var context = await CreateSeededContext(itemCount: 25);
        var source = context.TestEntities.OrderBy(entity => entity.Name).Select(entity => entity.Name);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await PaginatedList<string>.CreateAsync(source, pageNumber: 2, pageSize: 10, CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Items.Should().Equal(expectedItems);
        actual.TotalCount.Should().Be(25);
        actual.TotalPages.Should().Be(3);
        actual.HasPreviousPage.Should().BeTrue();
        actual.HasNextPage.Should().BeTrue();
    }

    [Test]
    public async Task ToPaginatedListAsync_Given_The_Last_Page_Is_Requested_Should_Return_Remaining_Items_Without_Next_Page()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        using var context = await CreateSeededContext(itemCount: 25);
        var source = context.TestEntities.OrderBy(entity => entity.Name).Select(entity => entity.Name);
        var query = new TestPagedQuery { PageNumber = 3, PageSize = 10 };

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await source.ToPaginatedListAsync(query, CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Items.Should().HaveCount(5);
        actual.HasPreviousPage.Should().BeTrue();
        actual.HasNextPage.Should().BeFalse();
    }

    [Test]
    public async Task CreateAsync_Given_The_Database_Has_No_Data_Should_Return_Empty_Page()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        using var context = await CreateSeededContext(itemCount: 0);
        var source = context.TestEntities.Select(entity => entity.Name);

        //------------------------------------- Act     ------------------------------------------------------
        var actual = await PaginatedList<string>.CreateAsync(source, pageNumber: 1, pageSize: 10, CancellationToken.None);

        //------------------------------------- Assert  ------------------------------------------------------
        actual.Items.Should().HaveCount(0);
        actual.TotalCount.Should().Be(0);
        actual.TotalPages.Should().Be(0);
        actual.HasPreviousPage.Should().BeFalse();
        actual.HasNextPage.Should().BeFalse();
    }

    private static async Task<TestDbContext> CreateSeededContext(int itemCount)
    {
        var context = TestDbContext.Create(Substitute.For<IDomainEventDispatcher>());
        context.TestEntities.AddRange(Enumerable.Range(1, itemCount).Select(i => new TestEntity { Name = $"Item {i:D2}" }));
        await context.SaveChangesAsync();

        return context;
    }
}
