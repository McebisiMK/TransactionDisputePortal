using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace TransactionDisputePortal.Infrastructure.Persistence.DataSeeding;

public static class ApplicationDbContextSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, ILogger logger, CancellationToken cancellationToken)
    {
        await SeedCustomersAsync(context, logger, cancellationToken);
    }

    private static async Task SeedCustomersAsync(ApplicationDbContext context, ILogger logger, CancellationToken cancellationToken)
    {
        var existingUserIds = await context.Customers
                                     .Select(customer => customer.ExternalUserId)
                                     .ToListAsync(cancellationToken);

        var customers = CustomerSeedData.ExternalUserIds
                         .Except(existingUserIds)
                         .Select(externalUserId => CustomerSeedData.CreateCustomer(externalUserId, DateTime.UtcNow))
                         .ToList();

        if (customers.Count == 0) return;

        context.Customers.AddRange(customers);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded {CustomerCount} customer(s) with {TransactionCount} transaction(s)",
            customers.Count, customers.SelectMany(customer => customer.Accounts).Sum(account => account.Transactions.Count));
    }
}
