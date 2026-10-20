using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TransactionDisputePortal.Application.Common.Security;

namespace TransactionDisputePortal.IntegrationTests.TestSupport;

/// <summary>
/// Stands in for Keycloak: a request carrying "Authorization: Test {userId}" is signed in as that customer user.
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

        var userId = header[(SchemeName.Length + 1)..];
        var claims = new[]
        {
            new Claim("sub", userId),
            new Claim("preferred_username", "integration.customer"),
            new Claim("roles", Roles.Customer)
        };
        var identity = new ClaimsIdentity(claims, SchemeName, "preferred_username", "roles");

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
