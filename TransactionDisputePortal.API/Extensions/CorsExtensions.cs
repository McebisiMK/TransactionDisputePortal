using TransactionDisputePortal.Application.Common.Extensions;

namespace TransactionDisputePortal.API.Extensions;

public static class CorsExtensions
{
    public const string PolicyName = "EnableCors";

    public static void RegisterCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName,
                policy =>
                {
                    var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

                    if (allowedOrigins.IsNull() || allowedOrigins.Length == 0)
                    {
                        throw new InvalidOperationException(
                            "CORS AllowedOrigins must be configured. Update appsettings.json with specific trusted origins.");
                    }

                    policy.WithOrigins(allowedOrigins)
                          .AllowCredentials()
                          .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS", "PATCH")
                          .WithHeaders("Content-Type", "Authorization", "Accept", "Origin")
                          .WithExposedHeaders("Content-Length", "X-Total-Count");
                });
        });
    }
}
