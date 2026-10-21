using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TransactionDisputePortal.Application.Common.Exceptions;
using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Domain.Common;
using TransactionDisputePortal.Domain.Entities;
using TransactionDisputePortal.Infrastructure.Persistence.Configurations;
using TransactionDisputePortal.Infrastructure.Persistence.Converters;

namespace TransactionDisputePortal.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IDomainEventDispatcher domainEventDispatcher) : DbContext(options), IApplicationDbContext
{
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    private readonly IDomainEventDispatcher domainEventDispatcher = domainEventDispatcher;

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Dispute> Disputes => Set<Dispute>();
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

        modelBuilder.HasSequence<long>(DisputeConfiguration.ReferenceSequence);

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
        int result;

        try
        {
            result = await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: UniqueIndexViolation or UniqueConstraintViolation })
        {
            throw new DuplicateRecordException(exception);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyException(exception);
        }

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
