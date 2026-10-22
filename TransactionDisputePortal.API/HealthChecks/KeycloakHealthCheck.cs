using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TransactionDisputePortal.API.HealthChecks;

public class KeycloakHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IHealthCheck
{
    public const string HttpClientName = "KeycloakHealthCheck";

    private readonly IConfiguration configuration = configuration;
    private readonly IHttpClientFactory httpClientFactory = httpClientFactory;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var metadataAddress = GetMetadataAddress();

        try
        {
            using var client = httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.GetAsync(metadataAddress, cancellationToken);

            if (response.IsSuccessStatusCode)
                return HealthCheckResult.Healthy();

            return new HealthCheckResult(context.Registration.FailureStatus, $"Keycloak returned {(int)response.StatusCode}.");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Keycloak could not be reached.", exception);
        }
    }

    private string GetMetadataAddress()
    {
        var metadataAddress = configuration["Keycloak:MetadataAddress"];
        if (!string.IsNullOrWhiteSpace(metadataAddress))
            return metadataAddress;

        return $"{configuration["Keycloak:Authority"]?.TrimEnd('/')}/.well-known/openid-configuration";
    }
}
