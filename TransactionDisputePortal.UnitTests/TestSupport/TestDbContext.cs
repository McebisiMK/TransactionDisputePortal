using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Domain.Common;
using TransactionDisputePortal.Infrastructure.Persistence;
using TransactionDisputePortal.Infrastructure.Persistence.Interceptors;

namespace TransactionDisputePortal.UnitTests.TestSupport;

public record TestEntityCreated(Guid EntityId) : DomainEvent;

public class TestEntity : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
}

public class TestDbContext(DbContextOptions<ApplicationDbContext> options, IDomainEventDispatcher domainEventDispatcher)
    : ApplicationDbContext(options, domainEventDispatcher)
{
    public DbSet<TestEntity> TestEntities => Set<TestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<TestEntity>().Ignore(entity => entity.DomainEvents);
    }

    public static TestDbContext Create(IDomainEventDispatcher domainEventDispatcher, ICurrentUserService? currentUserService = null)
    {
        currentUserService ??= Substitute.For<ICurrentUserService>();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(new AuditableEntitySaveChangesInterceptor(currentUserService))
            .Options;

        return new TestDbContext(options, domainEventDispatcher);
    }
}
