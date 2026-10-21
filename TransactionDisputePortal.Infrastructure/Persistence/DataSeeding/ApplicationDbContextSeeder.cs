using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Domain.Enums;

namespace TransactionDisputePortal.Infrastructure.Persistence.DataSeeding;

public static class ApplicationDbContextSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, IDisputeReferenceGenerator disputeReferenceGenerator, ILogger logger, CancellationToken cancellationToken)
    {
        await SeedCustomersAsync(context, logger, cancellationToken);
        await SeedDisputesAsync(context, disputeReferenceGenerator, logger, cancellationToken);
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

    private static async Task SeedDisputesAsync(ApplicationDbContext context, IDisputeReferenceGenerator disputeReferenceGenerator, ILogger logger, CancellationToken cancellationToken)
    {
        var seedUserIds = CustomerSeedData.ExternalUserIds.ToList();

        var customers = await context.Customers
                              .Where(customer => seedUserIds.Contains(customer.ExternalUserId))
                              .Where(customer => !context.Disputes.Any(dispute => dispute.CustomerId == customer.Id))
                              .Select(customer => new { customer.Id, customer.ExternalUserId })
                              .ToListAsync(cancellationToken);

        if (customers.Count == 0) return;

        var utcNow = DateTime.UtcNow;
        var disputeCount = 0;

        foreach (var customer in customers)
        {
            var postedTransactions = await context.Transactions
                                           .Include(transaction => transaction.Account)
                                           .Where(transaction => transaction.Account.CustomerId == customer.Id && transaction.Status == TransactionStatus.Posted)
                                           .OrderByDescending(transaction => transaction.TransactionDate)
                                           .ToListAsync(cancellationToken);

            var references = new List<string>();
            for (var index = 0; index < DisputeSeedData.Count; index++)
                references.Add(await disputeReferenceGenerator.NextReferenceAsync(cancellationToken));

            var disputes = DisputeSeedData.CreateDisputes(postedTransactions, customer.Id, CustomerSeedData.GetUsername(customer.ExternalUserId), references, utcNow);

            context.Disputes.AddRange(disputes);
            disputeCount += disputes.Count;
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded {DisputeCount} dispute(s) for {CustomerCount} customer(s)", disputeCount, customers.Count);
    }
}
