using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Persistence.Entities;
using RaveIsland.ApiService.Infrastructure.Tenancy;

namespace RaveIsland.ApiService.Features.Events.Selections;

public sealed class UpdateEventSelectionsEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/api/events/{eventId:guid}/selections/{lookupTypeCode}", Handle)
            .RequireAuthorization(AuthorizationPolicies.TenantMember);

    private static async Task<IResult> Handle(
        Guid eventId,
        string lookupTypeCode,
        UpdateSelectionsRequest request,
        AppDbContext db,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var eventEntity = await EventQueryHelper.FindEventAsync(db, tenantContext, eventId, cancellationToken);
        if (eventEntity is null) return Results.NotFound(new { error = "Event not found." });
        var access = EventQueryHelper.CheckAccess(tenantContext, eventEntity);
        if (access is not null) return access;

        var existing = await db.EventLookupSelections
            .Where(s => s.EventId == eventId && s.LookupTypeCode == lookupTypeCode)
            .ToListAsync(cancellationToken);
        db.EventLookupSelections.RemoveRange(existing);

        foreach (var valueId in request.LookupValueIds.Distinct())
        {
            var isValid = await db.LookupValues
                .AnyAsync(v => v.Id == valueId && v.LookupType.Code == lookupTypeCode && v.IsActive, cancellationToken);
            if (!isValid)
            {
                return Results.BadRequest(new { error = $"Invalid lookup value {valueId} for type {lookupTypeCode}." });
            }

            db.EventLookupSelections.Add(new EventLookupSelection
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                LookupValueId = valueId,
                LookupTypeCode = lookupTypeCode,
            });
        }

        eventEntity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { count = request.LookupValueIds.Count });
    }

    public sealed record UpdateSelectionsRequest(IReadOnlyList<Guid> LookupValueIds);
}
