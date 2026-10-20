using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TransactionDisputePortal.Infrastructure.Persistence.DataSeeding;

public static class DataSeedingExtensions
{
    public static async Task SeedDataAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var provider = scope.ServiceProvider;
        var seedingOptions = provider.GetRequiredService<IOptions<DataSeedingOptions>>().Value;

        if (!seedingOptions.Enabled) return;

        var context = provider.GetRequiredService<ApplicationDbContext>();
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("DataSeeding");

        await context.Database.MigrateAsync(cancellationToken);
        await ApplicationDbContextSeeder.SeedAsync(context, logger, cancellationToken);
    }
}
