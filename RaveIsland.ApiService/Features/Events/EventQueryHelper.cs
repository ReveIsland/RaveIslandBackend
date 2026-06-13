using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Persistence.Entities;
using RaveIsland.ApiService.Infrastructure.Tenancy;

namespace RaveIsland.ApiService.Features.Events;

internal static class EventQueryHelper
{
    public static async Task<EventEntity?> FindEventAsync(
        AppDbContext db,
        ITenantContext tenantContext,
        Guid eventId,
        CancellationToken cancellationToken,
        bool track = true)
    {
        var query = tenantContext.IsAdmin
            ? db.Events.IgnoreQueryFilters()
            : db.Events;

        if (!track)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);
    }

    public static IResult? CheckAccess(ITenantContext tenantContext, EventEntity eventEntity)
    {
        if (!EventAccess.CanModify(tenantContext, eventEntity.CreatedByUserId))
        {
            return Results.Forbid();
        }

        return null;
    }
}
