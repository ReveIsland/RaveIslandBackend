using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Persistence.Entities;
using RaveIsland.ApiService.Infrastructure.Tenancy;

namespace RaveIsland.ApiService.Features.Users.ListUsers;

public sealed class ListUsersEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/users", Handle).RequireAuthorization(AuthorizationPolicies.TenantAdminOrAdmin);

    private static async Task<IResult> Handle(
        AppDbContext db,
        ITenantContext tenantContext,
        Guid? tenantId,
        CancellationToken cancellationToken)
    {
        var effectiveTenantId = tenantContext.IsAdmin ? tenantId : tenantContext.TenantId;
        if (!tenantContext.IsAdmin && !effectiveTenantId.HasValue)
        {
            return Results.Forbid();
        }

        var membershipsQuery = db.TenantMemberships
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(m => m.Tenant)
            .AsQueryable();

        if (effectiveTenantId.HasValue)
        {
            membershipsQuery = membershipsQuery.Where(m => m.TenantId == effectiveTenantId.Value);
        }

        var memberships = await membershipsQuery
            .OrderBy(m => m.Tenant.Name)
            .ThenBy(m => m.CreatedAt)
            .Select(m => new
            {
                id = m.Id,
                type = "member",
                m.TenantId,
                tenantName = m.Tenant.Name,
                keycloakUserId = m.KeycloakUserId,
                m.Role,
                m.IsActive,
                status = m.IsActive ? "Registered" : "Disabled",
                m.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        var invitationsQuery = db.UserInvitations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(i => i.Tenant)
            .Where(i => i.Status != InvitationStatus.Registered)
            .AsQueryable();

        if (effectiveTenantId.HasValue)
        {
            invitationsQuery = invitationsQuery.Where(i => i.TenantId == effectiveTenantId.Value);
        }

        var invitations = await invitationsQuery
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new
            {
                id = i.Id,
                type = "invitation",
                i.TenantId,
                tenantName = i.Tenant.Name,
                keycloakUserId = i.KeycloakUserId,
                role = i.IntendedRole,
                isActive = i.Status != InvitationStatus.Revoked && i.Status != InvitationStatus.Expired,
                email = i.Email,
                firstName = i.FirstName,
                lastName = i.LastName,
                status = i.Status.ToString(),
                i.ExpiresAt,
                i.SentAt,
                i.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(new { members = memberships, invitations });
    }
}
