using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Lookups;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Persistence.Entities;
using RaveIsland.ApiService.Infrastructure.Tenancy;

namespace RaveIsland.ApiService.Features.Events.Tickets;

public sealed class ManageTicketTypesEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/api/events/{eventId:guid}/ticket-types", Handle).RequireAuthorization(AuthorizationPolicies.TenantMember);

    private static async Task<IResult> Handle(
        Guid eventId,
        UpsertTicketTypesRequest request,
        AppDbContext db,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var eventEntity = await EventQueryHelper.FindEventAsync(db, tenantContext, eventId, cancellationToken);
        if (eventEntity is null) return Results.NotFound(new { error = "Event not found." });
        var access = EventQueryHelper.CheckAccess(tenantContext, eventEntity);
        if (access is not null) return access;

        var existingIds = request.TicketTypes.Where(t => t.Id.HasValue).Select(t => t.Id!.Value).ToList();
        var toRemove = await db.EventTicketTypes
            .Where(t => t.EventId == eventId && !existingIds.Contains(t.Id))
            .ToListAsync(cancellationToken);
        db.EventTicketTypes.RemoveRange(toRemove);

        foreach (var item in request.TicketTypes)
        {
            EventTicketType ticketType;
            if (item.Id.HasValue)
            {
                ticketType = await db.EventTicketTypes.FirstAsync(t => t.Id == item.Id.Value && t.EventId == eventId, cancellationToken);
            }
            else
            {
                ticketType = new EventTicketType { Id = Guid.NewGuid(), EventId = eventId, Name = string.Empty };
                db.EventTicketTypes.Add(ticketType);
            }

            if (item.DefaultLookupValueId.HasValue &&
                !await LookupHelper.IsValidValueAsync(db, item.DefaultLookupValueId.Value, LookupTypeCodes.TicketType, cancellationToken))
            {
                return Results.BadRequest(new { error = "Invalid ticket type lookup value." });
            }

            ticketType.Name = item.Name.Trim();
            ticketType.Description = string.IsNullOrWhiteSpace(item.Description) ? null : item.Description.Trim();
            ticketType.Price = item.Price;
            ticketType.Quantity = item.Quantity;
            ticketType.SaleStart = item.SaleStart;
            ticketType.SaleEnd = item.SaleEnd;
            ticketType.MaxPerUser = item.MaxPerUser;
            ticketType.IsActive = item.IsActive;
            ticketType.DefaultLookupValueId = item.DefaultLookupValueId;
            ticketType.DisplayOrder = item.DisplayOrder;
        }

        eventEntity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { count = request.TicketTypes.Count });
    }

    public sealed record TicketTypeItem(
        Guid? Id,
        string Name,
        string? Description,
        decimal Price,
        int Quantity,
        DateTimeOffset? SaleStart,
        DateTimeOffset? SaleEnd,
        int? MaxPerUser,
        bool IsActive,
        Guid? DefaultLookupValueId,
        int DisplayOrder);

    public sealed record UpsertTicketTypesRequest(IReadOnlyList<TicketTypeItem> TicketTypes);
}
