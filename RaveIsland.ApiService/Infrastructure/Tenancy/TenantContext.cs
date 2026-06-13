using System.Security.Claims;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Identity;

namespace RaveIsland.ApiService.Infrastructure.Tenancy;

public sealed class TenantContext(IHttpContextAccessor httpContextAccessor, ITenantIdResolver tenantIdResolver)
    : ITenantContext
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.FindFirst("sub")?.Value;

    public Guid? TenantId => tenantIdResolver.GetTenantId();

    public IReadOnlyList<string> Roles => KeycloakClaims.GetRoles(User ?? new ClaimsPrincipal());

    public bool IsAdmin => Roles.Contains(AppRoles.Admin, StringComparer.OrdinalIgnoreCase);

    public bool IsTenantAdmin => Roles.Contains(AppRoles.TenantAdmin, StringComparer.OrdinalIgnoreCase);

    public bool IsTenantUser => Roles.Contains(AppRoles.TenantUser, StringComparer.OrdinalIgnoreCase);

    public bool BypassTenantFilters => IsAdmin;
}
