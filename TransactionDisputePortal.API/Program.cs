using Serilog;
using TransactionDisputePortal.API;
using TransactionDisputePortal.API.Extensions;
using TransactionDisputePortal.API.Middleware;
using TransactionDisputePortal.Application;
using TransactionDisputePortal.Infrastructure;
using TransactionDisputePortal.Infrastructure.Persistence.DataSeeding;

Log.Logger = new LoggerConfiguration()
                  .WriteTo.Console()
                  .CreateBootstrapLogger();

try
{
    Log.Information("Starting TransactionDisputePortal.API");

    var builder = WebApplication.CreateBuilder(args);

    builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services
     .RegisterApplication(builder.Configuration["LuckyPennySoftware:LicenseKey"])
     .RegisterInfrastructure(builder.Configuration)
     .RegisterAPI(builder.Configuration, builder.Environment);

    var app = builder.Build();

    await app.Services.SeedDataAsync();

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    app.UseStatusCodePages();

    if (app.Configuration.GetValue<bool>("Swagger:Enabled"))
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.OAuthUsePkce();
            options.OAuthScopes("openid");
            options.OAuthClientId(builder.Configuration["Keycloak:SwaggerClientId"]);
        });
    }

    if (app.Configuration.GetValue("Hosting:UseHttpsRedirection", true))
    {
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    app.UseRouting();
    app.UseCors(CorsExtensions.PolicyName);
    app.UseAuthentication();
    app.UseRateLimiter();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthCheckEndpoints();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "TransactionDisputePortal.API terminated unexpectedly");
    Environment.ExitCode = 1;
}
finally
{
    Log.CloseAndFlush();
}
