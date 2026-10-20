using System.Reflection;
using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Domain.Common;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Infrastructure.Persistence.Converters;

namespace TransactionDisputePortal.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IDomainEventDispatcher domainEventDispatcher) : DbContext(options), IApplicationDbContext
{
    private readonly IDomainEventDispatcher domainEventDispatcher = domainEventDispatcher;

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(type => typeof(BaseEntity).IsAssignableFrom(type.ClrType)).ToList())
        {
            modelBuilder.Entity(entityType.ClrType).Ignore(nameof(BaseEntity.DomainEvents));

            if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType).Property(nameof(AuditableEntity.CreatedBy)).HasMaxLength(256);
                modelBuilder.Entity(entityType.ClrType).Property(nameof(AuditableEntity.UpdatedBy)).HasMaxLength(256);
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);

        await DispatchDomainEventsAsync(cancellationToken);

        return result;
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        var entities = ChangeTracker.Entries<BaseEntity>()
            .Select(entry => entry.Entity)
            .Where(entity => entity.DomainEvents.Count != 0)
            .ToList();

        var domainEvents = entities.SelectMany(entity => entity.DomainEvents).ToList();

        entities.ForEach(entity => entity.ClearDomainEvents());

        await domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);
    }
}
