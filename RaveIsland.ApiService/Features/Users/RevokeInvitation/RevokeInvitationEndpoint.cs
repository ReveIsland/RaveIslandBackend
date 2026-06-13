using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Persistence.Entities;
using RaveIsland.ApiService.Infrastructure.Tenancy;

namespace RaveIsland.ApiService.Features.Users.RevokeInvitation;

public sealed class RevokeInvitationEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/invitations/{invitationId:guid}/revoke", Handle)
            .RequireAuthorization(AuthorizationPolicies.TenantAdminOrAdmin);

    private static async Task<IResult> Handle(
        Guid invitationId,
        AppDbContext db,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var invitation = await db.UserInvitations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == invitationId, cancellationToken);

        if (invitation is null)
        {
            return Results.NotFound(new { error = "Invitation not found." });
        }

        if (!tenantContext.IsAdmin && invitation.TenantId != tenantContext.TenantId)
        {
            return Results.Forbid();
        }

        if (invitation.Status == InvitationStatus.Registered)
        {
            return Results.Conflict(new { error = "Cannot revoke a completed registration." });
        }

        invitation.Status = InvitationStatus.Revoked;
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { invitation.Id, status = invitation.Status.ToString() });
    }
}
