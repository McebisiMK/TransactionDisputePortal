using TransactionDisputePortal.API.Helpers;
using TransactionDisputePortal.Application.Common.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace TransactionDisputePortal.API.Extensions;

public static class AuthenticationExtensions
{
    public static void RegisterAuthentication(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = configuration["Keycloak:Authority"];
                options.Audience = configuration["Keycloak:Audience"];
                options.RequireHttpsMetadata = !environment.IsDevelopment();

                var metadataAddress = configuration["Keycloak:MetadataAddress"];
                if (metadataAddress.IsNotBlank())
                    options.MetadataAddress = metadataAddress;
                options.MapInboundClaims = false;
                options.TokenValidationParameters.NameClaimType = "preferred_username";
                options.TokenValidationParameters.RoleClaimType = "roles";

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtBearerAuthentication");
                        logger.LogWarning(context.Exception, "JWT authentication failed on {Path}", context.HttpContext.Request.Path);
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddTransient<IClaimsTransformation, KeycloakRolesClaimsTransformation>();
    }
}
