using System.Diagnostics;
using System.Text.Json.Serialization;
using TransactionDisputePortal.API.ExceptionHandling;
using TransactionDisputePortal.API.Extensions;
using TransactionDisputePortal.API.Helpers;
using TransactionDisputePortal.Application.Common.Interfaces;

namespace TransactionDisputePortal.API;

public static class DependencyInjection
{
    public static IServiceCollection RegisterAPI(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
            context.ProblemDetails.Extensions.TryAdd("traceId", Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
            context.ProblemDetails.Extensions.TryAdd("correlationId", context.HttpContext.TraceIdentifier);
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.RegisterHealthChecks();
        services.RegisterRateLimiting(configuration);
        services.RegisterCors(configuration);
        services.RegisterAuthentication(configuration, environment);
        services.RegisterAuthorization();
        services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = InvalidModelStateResponse.Create);
        services.AddEndpointsApiExplorer();
        services.RegisterSwagger(configuration);

        return services;
    }
}
