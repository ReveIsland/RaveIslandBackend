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
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new
            {
                e.Id,
                e.TenantId,
                tenantName = e.Tenant.Name,
                e.Title,
                e.Description,
                e.CreatedByUserId,
                e.CreatedByName,
                e.CreatedAt,
                e.UpdatedAt,
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(events);
    }
}
