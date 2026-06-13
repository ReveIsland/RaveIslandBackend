using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Persistence;

namespace RaveIsland.ApiService.Features.Admin.GetStats;

public sealed class GetStatsEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/admin/stats", Handle).RequireAuthorization(AuthorizationPolicies.AdminOnly);

    private static async Task<IResult> Handle(
        IDistributedCache cache,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        const string cacheKey = "admin:stats:views";
        var raw = await cache.GetStringAsync(cacheKey, cancellationToken);
        var count = int.TryParse(raw, out var current) ? current + 1 : 1;
        await cache.SetStringAsync(cacheKey, count.ToString(), cancellationToken);

        var eventCount = await db.Events.IgnoreQueryFilters().CountAsync(cancellationToken);
        var tenantCount = await db.Tenants.IgnoreQueryFilters().CountAsync(cancellationToken);
        var userCount = await db.TenantMemberships.IgnoreQueryFilters().CountAsync(m => m.IsActive, cancellationToken);
        var pendingInvites = await db.UserInvitations.IgnoreQueryFilters()
            .CountAsync(i => i.Status == Infrastructure.Persistence.Entities.InvitationStatus.InviteSent ||
                             i.Status == Infrastructure.Persistence.Entities.InvitationStatus.Pending,
                cancellationToken);

        return Results.Ok(new
        {
            viewCount = count,
            eventCount,
            tenantCount,
            userCount,
            pendingInvites,
            cached = true,
        });
    }
}
