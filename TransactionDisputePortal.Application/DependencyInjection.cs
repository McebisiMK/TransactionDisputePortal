using MediatR;
using FluentValidation;
using System.Reflection;
using TransactionDisputePortal.Application.Common.Behaviors;
using TransactionDisputePortal.Application.Common.Extensions;
using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Common.Services;
using Microsoft.Extensions.DependencyInjection;

namespace TransactionDisputePortal.Application;

public static class DependencyInjection
{
    public static IServiceCollection RegisterApplication(this IServiceCollection services, string? configuredLicenseKey)
    {
        var licenseKey = configuredLicenseKey.IsBlank() ? null : configuredLicenseKey;

        services.AddAutoMapper(config => config.LicenseKey = licenseKey, Assembly.GetExecutingAssembly());
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddMediatR(config =>
        {
            config.LicenseKey = licenseKey;
            config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });
        services.AddScoped<ICurrentCustomerService, CurrentCustomerService>();
        return services;
    }
}
