using System.Security.Claims;
using RaveIsland.ApiService.Infrastructure.Identity;

namespace RaveIsland.ApiService.Common;

public static class AuthorizationRoleHelper
{
    public static bool HasAnyRole(ClaimsPrincipal user, params string[] roles)
    {
        var assignedRoles = KeycloakClaims.GetRoles(user);
        return roles.Any(role =>
            assignedRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
    }
}
