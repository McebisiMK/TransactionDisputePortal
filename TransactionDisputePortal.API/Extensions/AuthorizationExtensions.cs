using Microsoft.AspNetCore.Authorization;
using TransactionDisputePortal.API.Authorization;
using TransactionDisputePortal.Application.Common.Security;

namespace TransactionDisputePortal.API.Extensions;

public static class AuthorizationExtensions
{
    public static void RegisterAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(Policies.Customer, policy => policy.RequireRole(Roles.Customer))
            .AddPolicy(Policies.Agent, policy => policy.RequireRole(Roles.Agent));
    }
}
