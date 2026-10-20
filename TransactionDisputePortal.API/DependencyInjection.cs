using System.Diagnostics;
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
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.RegisterCors(configuration);
        services.RegisterAuthentication(configuration, environment);
        services.RegisterAuthorization();
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.RegisterSwagger(configuration);

        return services;
    }
}
