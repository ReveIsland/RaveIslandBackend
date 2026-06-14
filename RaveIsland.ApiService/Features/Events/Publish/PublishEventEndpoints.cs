using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Lookups;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Tenancy;

namespace RaveIsland.ApiService.Features.Events.Publish;

public sealed class GetPublishReadinessEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/events/{eventId:guid}/publish-readiness", Handle)
            .RequireAuthorization(AuthorizationPolicies.TenantMember);

    private static async Task<IResult> Handle(
        Guid eventId,
        AppDbContext db,
        ITenantContext tenantContext,
        IEventPublishValidator validator,
        CancellationToken cancellationToken)
    {
        var eventEntity = await EventQueryHelper.FindEventAsync(db, tenantContext, eventId, cancellationToken, track: false);
        if (eventEntity is null) return Results.NotFound(new { error = "Event not found." });

        var errors = await validator.ValidateAsync(eventEntity, cancellationToken);
        return Results.Ok(new { isReady = errors.Count == 0, errors });
    }
}

public sealed class PublishEventEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/events/{eventId:guid}/publish", Handle).RequireAuthorization(AuthorizationPolicies.TenantMember);

    private static async Task<IResult> Handle(
        Guid eventId,
        AppDbContext db,
        ITenantContext tenantContext,
        IEventPublishValidator validator,
        CancellationToken cancellationToken)
    {
        var eventEntity = await EventQueryHelper.FindEventAsync(db, tenantContext, eventId, cancellationToken);
        if (eventEntity is null) return Results.NotFound(new { error = "Event not found." });
        var access = EventQueryHelper.CheckAccess(tenantContext, eventEntity);
        if (access is not null) return access;

        var errors = await validator.ValidateAsync(eventEntity, cancellationToken);
        if (errors.Count > 0)
        {
            return Results.BadRequest(new { error = "Event is not ready to publish.", errors });
        }

        var publishedStatusId = await EventDefaults.GetPublishedStatusIdAsync(db, cancellationToken);
        if (!publishedStatusId.HasValue)
        {
            return Results.Problem("Published status lookup value is not seeded.");
        }

        eventEntity.EventStatusId = publishedStatusId.Value;
        if (string.IsNullOrWhiteSpace(eventEntity.Slug))
        {
            eventEntity.Slug = GenerateSlug(eventEntity.Title, eventEntity.Id);
        }

        eventEntity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(new
        {
            eventEntity.Id,
            eventEntity.Slug,
            publicUrl = $"/events/{eventEntity.Slug ?? eventEntity.Id.ToString()}",
        });
    }

    private static string GenerateSlug(string title, Guid id) =>
        $"{Slugify(title)}-{id.ToString()[..8]}".ToLowerInvariant();

    private static string Slugify(string input)
    {
        var chars = input.ToLowerInvariant()
            .Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-')
            .ToArray();
        return new string(chars).Replace(' ', '-').Trim('-');
    }
}
