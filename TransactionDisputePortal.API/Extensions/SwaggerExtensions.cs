using Microsoft.OpenApi;
using TransactionDisputePortal.API.Helpers;

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
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Transaction Dispute Portal API", Version = "v1" });

            // Schema and operation ids become the generated TypeScript type and method names (see TransactionDisputePortal.API.Client).
            options.CustomSchemaIds(GetSchemaId);
            options.CustomOperationIds(api => $"{api.ActionDescriptor.RouteValues["controller"]}_{api.ActionDescriptor.RouteValues["action"]}");
            options.SupportNonNullableReferenceTypes();
            options.UseAllOfToExtendReferenceSchemas();
            options.SchemaFilter<NonNullablePropertiesRequiredSchemaFilter>();

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

    // PaginatedList<DisputeDTO> -> PaginatedListOfDisputeModel; DTO stays a backend naming convention.
    private static string GetSchemaId(Type type) => type.IsGenericType
        ? $"{type.Name[..type.Name.IndexOf('`')]}Of{string.Join("And", type.GetGenericArguments().Select(GetSchemaId))}"
        : type.Name.EndsWith("DTO", StringComparison.Ordinal) ? $"{type.Name[..^3]}Model" : type.Name;
}
