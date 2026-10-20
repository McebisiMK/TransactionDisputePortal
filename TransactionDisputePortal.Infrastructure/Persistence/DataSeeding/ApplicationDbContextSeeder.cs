using Microsoft.Extensions.Logging;

namespace TransactionDisputePortal.Infrastructure.Persistence.DataSeeding;

public static class ApplicationDbContextSeeder
{
    public static Task SeedAsync(ApplicationDbContext context, ILogger logger, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
