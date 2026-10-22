using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TransactionDisputePortal.Application.Common.Interfaces;
using TransactionDisputePortal.Application.Common.Security;
using TransactionDisputePortal.Infrastructure.Persistence;
using TransactionDisputePortal.Infrastructure.Persistence.Interceptors;

namespace TransactionDisputePortal.IntegrationTests.TestSupport;

/// <summary>
/// Runs the real API pipeline (routing, filters, model binding, exception handling) with an in-memory database
/// and <see cref="TestAuthenticationHandler"/> in place of Keycloak.
/// </summary>
public class ApiFactory(int writePermitLimit = 1000) : WebApplicationFactory<Program>
{
    public const string UnreachableKeycloakMetadataAddress = "http://127.0.0.1:1/realms/test/.well-known/openid-configuration";

    private readonly int writePermitLimit = writePermitLimit;
    private readonly string databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("RateLimiting:Writes:PermitLimit", writePermitLimit.ToString(CultureInfo.InvariantCulture));
        builder.UseSetting("Keycloak:MetadataAddress", UnreachableKeycloakMetadataAddress);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
            {
                options.UseInMemoryDatabase(databaseName)
                    .AddInterceptors(serviceProvider.GetRequiredService<AuditableEntitySaveChangesInterceptor>());
            });

            services.RemoveAll<IDisputeReferenceGenerator>();
            services.AddSingleton<IDisputeReferenceGenerator, InMemoryDisputeReferenceGenerator>();

            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = TestAuthenticationHandler.SchemeName;
                    options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.SchemeName, null);
        });
    }

    public async Task SeedAsync(params object[] entities)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.AddRange(entities);
        await context.SaveChangesAsync();
    }

    public HttpClient CreateClientFor(Guid userId, string role = Roles.Customer)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new(TestAuthenticationHandler.SchemeName, $"{userId} {role}");

        return client;
    }
}
