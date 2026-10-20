using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Infrastructure.Persistence;
using TransactionDisputePortal.Infrastructure.Persistence.DataSeeding;
using TransactionDisputePortal.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TransactionDisputePortal.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection RegisterInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")!;

        services.AddScoped<AuditableEntitySaveChangesInterceptor>();

        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            options.UseSqlServer(connectionString)
                .AddInterceptors(serviceProvider.GetRequiredService<AuditableEntitySaveChangesInterceptor>());
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetService<ApplicationDbContext>()!);

        services.AddOptions<DataSeedingOptions>()
            .Bind(configuration.GetSection(nameof(DataSeedingOptions)));

        return services;
    }
}
