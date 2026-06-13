using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Tenancy;

namespace RaveIsland.ApiService.Features.Events.DeleteEvent;

public sealed class DeleteEventEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/api/events/{eventId:guid}", Handle).RequireAuthorization(AuthorizationPolicies.TenantMember);

    private static async Task<IResult> Handle(
        Guid eventId,
        AppDbContext db,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var eventEntity = tenantContext.IsAdmin
            ? await db.Events.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken)
            : await db.Events.FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        if (eventEntity is null)
        {
            return Results.NotFound(new { error = "Event not found." });
        }

        if (!EventAccess.CanModify(tenantContext, eventEntity.CreatedByUserId))
        {
            return Results.Forbid();
        }

        db.Events.Remove(eventEntity);
        await db.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}
