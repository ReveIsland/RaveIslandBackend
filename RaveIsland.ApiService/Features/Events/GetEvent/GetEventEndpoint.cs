using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Tenancy;

namespace RaveIsland.ApiService.Features.Events.GetEvent;

public sealed class GetEventEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/events/{eventId:guid}", Handle).RequireAuthorization(AuthorizationPolicies.TenantMember);

    private static async Task<IResult> Handle(
        Guid eventId,
        AppDbContext db,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var eventEntity = tenantContext.IsAdmin
            ? await db.Events
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Include(e => e.Tenant)
                .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken)
            : await db.Events
                .AsNoTracking()
                .Include(e => e.Tenant)
                .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        if (eventEntity is null)
        {
            return Results.NotFound(new { error = "Event not found." });
        }

        if (tenantContext.IsTenantUser && !tenantContext.IsAdmin && !tenantContext.IsTenantAdmin &&
            !string.Equals(tenantContext.UserId, eventEntity.CreatedByUserId, StringComparison.Ordinal))
        {
            return Results.Forbid();
        }

        return Results.Ok(new
        {
            eventEntity.Id,
            eventEntity.TenantId,
            tenantName = eventEntity.Tenant.Name,
            eventEntity.Title,
            eventEntity.Description,
            eventEntity.CreatedByUserId,
            eventEntity.CreatedByName,
            eventEntity.CreatedAt,
            eventEntity.UpdatedAt,
        });
    }
}
