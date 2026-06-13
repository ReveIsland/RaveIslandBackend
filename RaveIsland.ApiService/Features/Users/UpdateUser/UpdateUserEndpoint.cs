using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Identity;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Tenancy;

namespace RaveIsland.ApiService.Features.Users.UpdateUser;

public sealed class UpdateUserEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPatch("/api/users/{membershipId:guid}", Handle).RequireAuthorization(AuthorizationPolicies.TenantAdminOrAdmin);

    private static async Task<IResult> Handle(
        Guid membershipId,
        UpdateUserRequest request,
        AppDbContext db,
        ITenantContext tenantContext,
        IKeycloakAdminService keycloakAdmin,
        CancellationToken cancellationToken)
    {
        var membership = await db.TenantMemberships
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == membershipId, cancellationToken);

        if (membership is null)
        {
            return Results.NotFound(new { error = "User membership not found." });
        }

        if (!tenantContext.IsAdmin && membership.TenantId != tenantContext.TenantId)
        {
            return Results.Forbid();
        }

        if (tenantContext.IsTenantAdmin && !tenantContext.IsAdmin &&
            string.Equals(membership.Role, AppRoles.TenantAdmin, StringComparison.OrdinalIgnoreCase))
        {
            return Results.Forbid();
        }

        if (request.IsActive.HasValue)
        {
            membership.IsActive = request.IsActive.Value;
            await keycloakAdmin.SetUserEnabledAsync(membership.KeycloakUserId, request.IsActive.Value, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(request.Role) &&
            AppRoles.TenantRoles.Contains(request.Role, StringComparer.OrdinalIgnoreCase))
        {
            if (tenantContext.IsTenantAdmin && !tenantContext.IsAdmin &&
                string.Equals(request.Role, AppRoles.TenantAdmin, StringComparison.OrdinalIgnoreCase))
            {
                return Results.Forbid();
            }

            membership.Role = request.Role;
            await keycloakAdmin.UpdateUserRoleAsync(membership.KeycloakUserId, request.Role, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(new
        {
            membership.Id,
            membership.TenantId,
            membership.KeycloakUserId,
            membership.Role,
            membership.IsActive,
        });
    }

    public sealed record UpdateUserRequest(bool? IsActive, string? Role);
}
