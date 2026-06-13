using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Identity;
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

        var tenantId = tenantContext.IsAdmin ? request.TenantId ?? tenantContext.TenantId : tenantContext.TenantId;
        if (!tenantId.HasValue)
        {
            return Results.BadRequest(new { error = "Tenant context is required to create an event." });
        }

        if (!tenantContext.IsAdmin)
        {
            var tenantExists = await db.Tenants.AnyAsync(t => t.Id == tenantId.Value && t.IsActive, cancellationToken);
            if (!tenantExists)
            {
                return Results.NotFound(new { error = "Tenant not found." });
            }
        }
        else
        {
            var tenantExists = await db.Tenants.IgnoreQueryFilters()
                .AnyAsync(t => t.Id == tenantId.Value && t.IsActive, cancellationToken);
            if (!tenantExists)
            {
                return Results.NotFound(new { error = "Tenant not found." });
            }
        }

        var userId = KeycloakClaims.GetUserId(user);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Results.Json(
                new { error = "User identity could not be resolved from the access token." },
                statusCode: StatusCodes.Status400BadRequest);
        }

        var now = DateTimeOffset.UtcNow;
        var eventEntity = new EventEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CreatedByUserId = userId,
            CreatedByName = KeycloakClaims.GetDisplayName(user),
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Events.Add(eventEntity);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/events/{eventEntity.Id}", new
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

    public sealed record CreateEventRequest(string Title, string? Description, Guid? TenantId);
}
