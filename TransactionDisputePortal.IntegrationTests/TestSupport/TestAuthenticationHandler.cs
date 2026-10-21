using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TransactionDisputePortal.Application.Common.Security;

namespace TransactionDisputePortal.IntegrationTests.TestSupport;

/// <summary>
/// Stands in for Keycloak: a request carrying "Authorization: Test {userId} {role}" is signed in as that user with that role.
/// The role is optional and defaults to customer.
/// </summary>
public class TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith($"{SchemeName} ", StringComparison.Ordinal))
            return Task.FromResult(AuthenticateResult.NoResult());

        var parts = header[(SchemeName.Length + 1)..].Split(' ');
        var userId = parts[0];
        var role = parts.Length > 1 ? parts[1] : Roles.Customer;
        var claims = new[]
        {
            new Claim("sub", userId),
            new Claim("preferred_username", $"integration.{role}"),
            new Claim("roles", role)
        };
        var identity = new ClaimsIdentity(claims, SchemeName, "preferred_username", "roles");

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
