using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Tenancy;

namespace RaveIsland.ApiService.Features.Events.ListEvents;

public sealed class ListEventsEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/events", Handle).RequireAuthorization(AuthorizationPolicies.TenantMember);

    private static async Task<IResult> Handle(
        AppDbContext db,
        ITenantContext tenantContext,
        Guid? tenantId,
        CancellationToken cancellationToken)
    {
        var query = tenantContext.IsAdmin
            ? db.Events.IgnoreQueryFilters().AsNoTracking()
            : db.Events.AsNoTracking();

        if (tenantContext.IsAdmin && tenantId.HasValue)
        {
            query = query.Where(e => e.TenantId == tenantId.Value);
        }

        if (tenantContext.IsTenantUser && !tenantContext.IsAdmin && !tenantContext.IsTenantAdmin)
        {
            query = query.Where(e => e.CreatedByUserId == tenantContext.UserId);
        }

        var events = await query
            .Include(e => e.Tenant)
            .Include(e => e.EventCategory)
            .Include(e => e.EventStatus)
            .Include(e => e.VisibilityType)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        return Results.Ok(events.Select(EventMapper.MapSummary));
    }
}
