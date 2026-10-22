using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using TransactionDisputePortal.API.HealthChecks;
using TransactionDisputePortal.Infrastructure.Persistence;

namespace TransactionDisputePortal.API.Extensions;

public static class HealthCheckExtensions
{
    public const string LivenessPath = "/health/live";
    public const string ReadinessPath = "/health";

    private const string ReadyTag = "ready";

    public static void RegisterHealthChecks(this IServiceCollection services)
    {
        services.AddHttpClient(KeycloakHealthCheck.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(5));

        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>("database", tags: [ReadyTag])
            .AddCheck<KeycloakHealthCheck>("keycloak", tags: [ReadyTag]);
    }

    public static void MapHealthCheckEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks(LivenessPath, new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = HealthCheckResponseWriter.WriteAsync
        }).AllowAnonymous().DisableRateLimiting();

        endpoints.MapHealthChecks(ReadinessPath, new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(ReadyTag),
            ResponseWriter = HealthCheckResponseWriter.WriteAsync
        }).AllowAnonymous().DisableRateLimiting();
    }
}
