using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Domain.Common;
using TransactionDisputePortal.UnitTests.TestSupport;

namespace TransactionDisputePortal.UnitTests.Infrastructure;

[TestFixture]
public class ApplicationDbContextTests
{
    [Test]
    public async Task SaveChangesAsync_Given_An_Entity_Raised_A_Domain_Event_Should_Dispatch_And_Clear_It()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var domainEventDispatcher = Substitute.For<IDomainEventDispatcher>();
        using var context = CreateApplicationDbContext(domainEventDispatcher);

        var entity = new TestEntity { Name = "first" };
        var expectedDomainEvent = new TestEntityCreated(entity.Id);
        entity.AddDomainEvent(expectedDomainEvent);
        context.TestEntities.Add(entity);

        //------------------------------------- Act     ------------------------------------------------------
        await context.SaveChangesAsync();

        //------------------------------------- Assert  ------------------------------------------------------
        await domainEventDispatcher.Received(1).DispatchAsync(
            Arg.Is<IEnumerable<DomainEvent>>(events => events.SequenceEqual(new DomainEvent[] { expectedDomainEvent })),
            Arg.Any<CancellationToken>());
        entity.DomainEvents.Should().HaveCount(0);
    }

    [Test]
    public async Task SaveChangesAsync_Given_Events_Were_Dispatched_On_A_Previous_Save_Should_Not_Dispatch_Them_Again()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var domainEventDispatcher = Substitute.For<IDomainEventDispatcher>();
        using var context = CreateApplicationDbContext(domainEventDispatcher);

        var entity = new TestEntity { Name = "first" };
        entity.AddDomainEvent(new TestEntityCreated(entity.Id));
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();
        domainEventDispatcher.ClearReceivedCalls();

        entity.Name = "renamed";

        //------------------------------------- Act     ------------------------------------------------------
        await context.SaveChangesAsync();

        //------------------------------------- Assert  ------------------------------------------------------
        await domainEventDispatcher.Received(1).DispatchAsync(
            Arg.Is<IEnumerable<DomainEvent>>(events => !events.Any()),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SaveChangesAsync_Given_A_New_Entity_Should_Set_Created_Audit_Fields()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var expectedCreatedBy = "test.customer";
        var currentUserService = Substitute.For<ICurrentUserService>();
        using var context = CreateApplicationDbContext(currentUserService: currentUserService);

        currentUserService.Username.Returns(expectedCreatedBy);
        var before = DateTime.UtcNow;
        var entity = new TestEntity { Name = "first", CreatedBy = "someone else", CreatedDate = DateTime.MinValue };
        context.TestEntities.Add(entity);

        //------------------------------------- Act     ------------------------------------------------------
        await context.SaveChangesAsync();

        //------------------------------------- Assert  ------------------------------------------------------
        entity.CreatedBy.Should().Be(expectedCreatedBy);
        entity.CreatedDate.Should().BeOnOrAfter(before);
        entity.UpdatedBy.Should().BeNull();
        entity.UpdatedDate.Should().BeNull();
    }

    [Test]
    public async Task SaveChangesAsync_Given_A_Modified_Entity_Should_Set_Updated_Audit_Fields_And_Keep_Created_Audit_Fields()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var expectedCreatedBy = "test.customer";
        var expectedUpdatedBy = "test.agent";
        var currentUserService = Substitute.For<ICurrentUserService>();
        using var context = CreateApplicationDbContext(currentUserService: currentUserService);

        currentUserService.Username.Returns(expectedCreatedBy);
        var entity = new TestEntity { Name = "first" };
        context.TestEntities.Add(entity);
        await context.SaveChangesAsync();
        var expectedCreatedDate = entity.CreatedDate;

        currentUserService.Username.Returns(expectedUpdatedBy);
        entity.Name = "renamed";

        //------------------------------------- Act     ------------------------------------------------------
        await context.SaveChangesAsync();

        //------------------------------------- Assert  ------------------------------------------------------
        entity.UpdatedBy.Should().Be(expectedUpdatedBy);
        entity.UpdatedDate.Should().NotBeNull();
        entity.CreatedBy.Should().Be(expectedCreatedBy);
        entity.CreatedDate.Should().Be(expectedCreatedDate);
    }

    [Test]
    public async Task SaveChangesAsync_Given_No_Authenticated_User_Should_Audit_As_System()
    {
        //------------------------------------- Arrange ------------------------------------------------------
        var currentUserService = Substitute.For<ICurrentUserService>();
        using var context = CreateApplicationDbContext(currentUserService: currentUserService);

        currentUserService.Username.Returns((string?)null);
        var entity = new TestEntity { Name = "seeded" };
        context.TestEntities.Add(entity);

        //------------------------------------- Act     ------------------------------------------------------
        await context.SaveChangesAsync();

        //------------------------------------- Assert  ------------------------------------------------------
        entity.CreatedBy.Should().Be("System");
    }

    private static TestDbContext CreateApplicationDbContext(IDomainEventDispatcher? domainEventDispatcher = null, ICurrentUserService? currentUserService = null)
    {
        return TestDbContext.Create(domainEventDispatcher ?? Substitute.For<IDomainEventDispatcher>(), currentUserService);
    }
}
