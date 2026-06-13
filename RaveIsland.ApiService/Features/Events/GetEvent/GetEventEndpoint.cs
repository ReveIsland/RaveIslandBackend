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
        var query = tenantContext.IsAdmin
            ? db.Events.IgnoreQueryFilters().AsNoTracking()
            : db.Events.AsNoTracking();

        var eventEntity = await query
            .WithDetails()
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

        return Results.Ok(EventMapper.MapDetail(eventEntity));
    }
}
