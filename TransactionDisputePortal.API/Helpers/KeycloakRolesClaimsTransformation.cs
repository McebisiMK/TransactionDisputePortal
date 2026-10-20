using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using TransactionDisputePortal.Application.Common.Extensions;

namespace TransactionDisputePortal.API.Helpers;

public class KeycloakRolesClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        if (identity.IsNull() || !identity.IsAuthenticated || identity.HasClaim(c => c.Type == "roles-mapped"))
            return Task.FromResult(principal);

        var realmAccess = identity.FindFirst("realm_access")?.Value;
        if (realmAccess.IsNotBlank())
        {
            using var document = JsonDocument.Parse(realmAccess);
            if (document.RootElement.TryGetProperty("roles", out var roles))
            {
                foreach (var role in roles.EnumerateArray())
                {
                    var value = role.GetString();
                    if (value.IsNotBlank())
                        identity.AddClaim(new Claim("roles", value));
                }
            }
        }

        identity.AddClaim(new Claim("roles-mapped", "true"));

        return Task.FromResult(principal);
    }
}
