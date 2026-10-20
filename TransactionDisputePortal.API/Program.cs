using Serilog;
using TransactionDisputePortal.API;
using TransactionDisputePortal.API.Extensions;
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

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services
     .RegisterApplication()
     .RegisterInfrastructure(builder.Configuration)
     .RegisterAPI(builder.Configuration, builder.Environment);

    var app = builder.Build();

    await app.Services.SeedDataAsync();

    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    app.UseStatusCodePages();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.OAuthUsePkce();
            options.OAuthScopes("openid");
            options.OAuthClientId(builder.Configuration["Keycloak:SwaggerClientId"]);
        });
    }

    if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();

    app.UseRouting();
    app.UseCors(CorsExtensions.PolicyName);
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

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
