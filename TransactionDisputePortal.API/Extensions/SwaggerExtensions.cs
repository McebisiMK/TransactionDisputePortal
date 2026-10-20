using Microsoft.OpenApi;

namespace TransactionDisputePortal.API.Extensions;

public static class SwaggerExtensions
{
    public static void RegisterSwagger(this IServiceCollection services, IConfiguration configuration)
    {
        var authority = configuration["Keycloak:Authority"]!.TrimEnd('/');

        var authorizationUrl = $"{authority}/protocol/openid-connect/auth";
        var tokenUrl = $"{authority}/protocol/openid-connect/token";

        services.AddSwaggerGen(options =>
        {
            options.CustomSchemaIds(type => type.FullName!.Replace('+', '-'));

            options.AddSecurityDefinition("Keycloak", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri(authorizationUrl),
                        TokenUrl = new Uri(tokenUrl),
                        Scopes = new Dictionary<string, string> { ["openid"] = "openid" }
                    }
                }
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Keycloak", document)] = []
            });
        });
    }
}
