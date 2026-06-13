using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Tenancy;

namespace RaveIsland.ApiService.Features.Events.Analytics;

public sealed class GetEventAnalyticsEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/events/{eventId:guid}/analytics", Handle).RequireAuthorization(AuthorizationPolicies.TenantMember);

    private static async Task<IResult> Handle(
        Guid eventId,
        AppDbContext db,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var eventEntity = await EventQueryHelper.FindEventAsync(db, tenantContext, eventId, cancellationToken, track: false);
        if (eventEntity is null) return Results.NotFound(new { error = "Event not found." });

        var ticketTypes = await db.EventTicketTypes
            .AsNoTracking()
            .Where(t => t.EventId == eventId)
            .ToListAsync(cancellationToken);

        var ticketsSold = ticketTypes.Sum(t => t.QuantitySold);
        var totalCapacity = ticketTypes.Sum(t => t.Quantity);
        var revenue = ticketTypes.Sum(t => t.QuantitySold * t.Price);

        var attendance = await db.Tickets
            .AsNoTracking()
            .CountAsync(t => t.EventId == eventId && t.IsCheckedIn, cancellationToken);

        var promoUsage = await db.EventPromoCodes
            .AsNoTracking()
            .Where(p => p.EventId == eventId)
            .SumAsync(p => p.UsageCount, cancellationToken);

        var conversionRate = totalCapacity > 0 ? Math.Round((decimal)ticketsSold / totalCapacity * 100, 2) : 0m;

        return Results.Ok(new
        {
            ticketsSold,
            totalCapacity,
            revenue,
            attendance,
            conversionRate,
            promoUsage,
            ticketTypeBreakdown = ticketTypes.Select(t => new
            {
                t.Id,
                t.Name,
                t.Quantity,
                t.QuantitySold,
                revenue = t.QuantitySold * t.Price,
            }),
        });
    }
}
