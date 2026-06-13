using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Tenancy;

namespace RaveIsland.ApiService.Features.Events.UpdateEvent;

public sealed class UpdateEventEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPatch("/api/events/{eventId:guid}", Handle).RequireAuthorization(AuthorizationPolicies.TenantMember);

    private static async Task<IResult> Handle(
        Guid eventId,
        UpdateEventRequest request,
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

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            eventEntity.Title = request.Title.Trim();
        }

        if (request.Description is not null)
        {
            eventEntity.Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim();
        }

        eventEntity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(new
        {
            eventEntity.Id,
            eventEntity.TenantId,
            eventEntity.Title,
            eventEntity.Description,
            eventEntity.CreatedByUserId,
            eventEntity.CreatedByName,
            eventEntity.CreatedAt,
            eventEntity.UpdatedAt,
        });
    }

    public sealed record UpdateEventRequest(string? Title, string? Description);
}
