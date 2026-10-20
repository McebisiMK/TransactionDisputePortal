using TransactionDisputePortal.Application.Common.Interfaces;

namespace TransactionDisputePortal.API.Helpers;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;

    public IReadOnlyList<string> Roles => GetRoles();

    public string? Username => GetUsername();

    public Guid? UserId => GetUserId();

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    private string? GetClaimValue(string claimType)
    {
        return httpContextAccessor.HttpContext?.User.FindFirst(claimType)?.Value;
    }

    private string? GetUsername()
    {
        var user = httpContextAccessor.HttpContext?.User;

        return user?.FindFirst("preferred_username")?.Value
               ?? user?.FindFirst("name")?.Value
               ?? user?.Identity?.Name;
    }

    private Guid? GetUserId()
    {
        var stringUserId = GetClaimValue("sub");

        return Guid.TryParse(stringUserId, out var userId) ? userId : null;
    }

    private IReadOnlyList<string> GetRoles()
    {
        return httpContextAccessor.HttpContext?.User
                .FindAll("roles")
                .Select(c => c.Value)
                .ToList()
                .AsReadOnly() ?? new List<string>().AsReadOnly();
    }
}
