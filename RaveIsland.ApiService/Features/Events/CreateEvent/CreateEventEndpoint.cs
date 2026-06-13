using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Identity;
using RaveIsland.ApiService.Infrastructure.Lookups;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Persistence.Entities;
using RaveIsland.ApiService.Infrastructure.Tenancy;

namespace RaveIsland.ApiService.Features.Events.CreateEvent;

public sealed class CreateEventEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/events", Handle).RequireAuthorization(AuthorizationPolicies.TenantMember);

    private static async Task<IResult> Handle(
        CreateEventRequest request,
        AppDbContext db,
        ITenantContext tenantContext,
        System.Security.Claims.ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return Results.BadRequest(new { error = "Title is required." });
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            return Results.BadRequest(new { error = "Description is required." });
        }

        if (!request.EventCategoryId.HasValue)
        {
            return Results.BadRequest(new { error = "Event category is required." });
        }

        if (!await LookupHelper.IsValidValueAsync(db, request.EventCategoryId.Value, LookupTypeCodes.EventCategory, cancellationToken))
        {
            return Results.BadRequest(new { error = "Invalid event category." });
        }

        var tenantId = tenantContext.IsAdmin ? request.TenantId ?? tenantContext.TenantId : tenantContext.TenantId;
        if (!tenantId.HasValue)
        {
            return Results.BadRequest(new { error = "Tenant context is required to create an event." });
        }

        var tenantQuery = tenantContext.IsAdmin
            ? db.Tenants.IgnoreQueryFilters()
            : db.Tenants;
        if (!await tenantQuery.AnyAsync(t => t.Id == tenantId.Value && t.IsActive, cancellationToken))
        {
            return Results.NotFound(new { error = "Tenant not found." });
        }

        var userId = KeycloakClaims.GetUserId(user);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Json(
                new { error = "User identity could not be resolved from the access token." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        var draftStatusId = await EventDefaults.GetDraftStatusIdAsync(db, cancellationToken);
        var publicVisibilityId = await EventDefaults.GetPublicVisibilityIdAsync(db, cancellationToken);
        if (!draftStatusId.HasValue || !publicVisibilityId.HasValue)
        {
            return Results.Problem("Required lookup values are not seeded.");
        }

        Guid eventStatusId = draftStatusId.Value;
        if (request.EventStatusId.HasValue)
        {
            if (!await LookupHelper.IsValidValueAsync(db, request.EventStatusId.Value, LookupTypeCodes.EventStatus, cancellationToken))
            {
                return Results.BadRequest(new { error = "Invalid event status." });
            }

            eventStatusId = request.EventStatusId.Value;
        }

        var now = DateTimeOffset.UtcNow;
        var eventEntity = new EventEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            Title = request.Title.Trim(),
            Tagline = string.IsNullOrWhiteSpace(request.Tagline) ? null : request.Tagline.Trim(),
            Description = request.Description.Trim(),
            EventCategoryId = request.EventCategoryId.Value,
            Theme = string.IsNullOrWhiteSpace(request.Theme) ? null : request.Theme.Trim(),
            EventStatusId = eventStatusId,
            OrganizerReference = string.IsNullOrWhiteSpace(request.OrganizerReference) ? null : request.OrganizerReference.Trim(),
            VisibilityTypeId = publicVisibilityId.Value,
            CreatedByUserId = userId,
            CreatedByName = KeycloakClaims.GetDisplayName(user),
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Events.Add(eventEntity);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/events/{eventEntity.Id}", new { eventEntity.Id });
    }

    public sealed record CreateEventRequest(
        string Title,
        string Description,
        Guid? EventCategoryId,
        string? Tagline,
        string? Theme,
        Guid? EventStatusId,
        string? OrganizerReference,
        Guid? TenantId);
}
