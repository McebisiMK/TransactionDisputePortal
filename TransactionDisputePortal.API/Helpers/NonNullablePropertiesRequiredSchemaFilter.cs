using System.Reflection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TransactionDisputePortal.API.Helpers;

/// <summary>
/// Marks every non-nullable property as required so generated clients (NSwag) type them as always present
/// instead of optional. Nullable value types and nullable reference types stay optional.
/// </summary>
public class NonNullablePropertiesRequiredSchemaFilter : ISchemaFilter
{
    private readonly NullabilityInfoContext nullabilityContext = new();

    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema { Properties.Count: > 0 } objectSchema || context.Type.IsEnum)
            return;

        var nonNullableProperties = context.Type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => nullabilityContext.Create(property).ReadState == NullabilityState.NotNull)
            .Select(property => property.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        objectSchema.Required ??= new HashSet<string>();

        foreach (var propertyName in objectSchema.Properties.Keys.Where(nonNullableProperties.Contains))
            objectSchema.Required.Add(propertyName);
    }
}
