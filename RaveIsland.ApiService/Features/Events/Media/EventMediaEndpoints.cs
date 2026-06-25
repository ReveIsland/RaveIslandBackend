using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Media;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Persistence.Entities;
using RaveIsland.ApiService.Infrastructure.Tenancy;

namespace RaveIsland.ApiService.Features.Events.Media;

public sealed class UploadEventMediaEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/events/{eventId:guid}/media", Handle)
            .RequireAuthorization(AuthorizationPolicies.TenantMember)
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data")
            .WithMetadata(new RequestSizeLimitAttribute(20 * 1024 * 1024));

    private static async Task<IResult> Handle(
        Guid eventId,
        HttpContext httpContext,
        AppDbContext db,
        ITenantContext tenantContext,
        IMediaStorageService mediaStorage,
        ILogger<UploadEventMediaEndpoint> logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = httpContext.Request;
            if (!request.HasFormContentType)
            {
                return Results.BadRequest(new { error = "Multipart form data is required." });
            }

            var form = await request.ReadFormAsync(cancellationToken);
            var file = form.Files.GetFile("file");
            var mediaType = form["mediaType"].ToString();

            if (file is null || file.Length == 0)
            {
                return Results.BadRequest(new { error = "File is required." });
            }

            if (!Enum.TryParse<EventMediaType>(mediaType, ignoreCase: true, out var parsedType))
            {
                return Results.BadRequest(new { error = "Invalid media type." });
            }

            var eventEntity = await EventQueryHelper.FindEventAsync(db, tenantContext, eventId, cancellationToken);
            if (eventEntity is null) return Results.NotFound(new { error = "Event not found." });
            var access = EventQueryHelper.CheckAccess(tenantContext, eventEntity);
            if (access is not null) return access;

            if (parsedType is EventMediaType.Cover or EventMediaType.Banner)
            {
                var existing = await db.EventMedia
                    .IgnoreQueryFilters()
                    .Where(m => m.EventId == eventId && m.MediaType == parsedType)
                    .ToListAsync(cancellationToken);

                foreach (var item in existing)
                {
                    await mediaStorage.DeleteAsync(item.StorageUrl, cancellationToken);
                    db.EventMedia.Remove(item);
                }
            }

            var upload = await mediaStorage.UploadAsync(file, $"events/{eventId}", cancellationToken);
            var maxOrder = await db.EventMedia
                .IgnoreQueryFilters()
                .Where(m => m.EventId == eventId)
                .MaxAsync(m => (int?)m.DisplayOrder, cancellationToken) ?? 0;

            var media = new EventMedia
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                MediaType = parsedType,
                StorageUrl = upload.StorageUrl,
                ThumbnailUrl = upload.ThumbnailUrl,
                DisplayOrder = maxOrder + 1,
                FileName = upload.FileName,
                Event = eventEntity,
            };

            db.EventMedia.Add(media);
            eventEntity.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            return Results.Created($"/api/events/{eventId}/media/{media.Id}", new
            {
                media.Id,
                mediaType = media.MediaType.ToString(),
                media.StorageUrl,
                media.ThumbnailUrl,
                media.DisplayOrder,
                media.FileName,
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload media for event {EventId}", eventId);
            return Results.Problem(
                detail: httpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()
                    ? ex.ToString()
                    : "An error occurred while uploading the file.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}

public sealed class DeleteEventMediaEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/api/events/{eventId:guid}/media/{mediaId:guid}", Handle)
            .RequireAuthorization(AuthorizationPolicies.TenantMember);

    private static async Task<IResult> Handle(
        Guid eventId,
        Guid mediaId,
        AppDbContext db,
        ITenantContext tenantContext,
        IMediaStorageService mediaStorage,
        CancellationToken cancellationToken)
    {
        var eventEntity = await EventQueryHelper.FindEventAsync(db, tenantContext, eventId, cancellationToken);
        if (eventEntity is null) return Results.NotFound(new { error = "Event not found." });
        var access = EventQueryHelper.CheckAccess(tenantContext, eventEntity);
        if (access is not null) return access;

        var media = await db.EventMedia
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == mediaId && m.EventId == eventId, cancellationToken);
        if (media is null) return Results.NotFound(new { error = "Media not found." });

        await mediaStorage.DeleteAsync(media.StorageUrl, cancellationToken);
        db.EventMedia.Remove(media);
        eventEntity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Results.NoContent();
    }
}

public sealed class ReorderEventMediaEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/api/events/{eventId:guid}/media/reorder", Handle)
            .RequireAuthorization(AuthorizationPolicies.TenantMember);

    private static async Task<IResult> Handle(
        Guid eventId,
        ReorderMediaRequest request,
        AppDbContext db,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var eventEntity = await EventQueryHelper.FindEventAsync(db, tenantContext, eventId, cancellationToken);
        if (eventEntity is null) return Results.NotFound(new { error = "Event not found." });
        var access = EventQueryHelper.CheckAccess(tenantContext, eventEntity);
        if (access is not null) return access;

        var mediaItems = await db.EventMedia
            .IgnoreQueryFilters()
            .Where(m => m.EventId == eventId)
            .ToListAsync(cancellationToken);
        for (var i = 0; i < request.MediaIds.Count; i++)
        {
            var item = mediaItems.FirstOrDefault(m => m.Id == request.MediaIds[i]);
            if (item is not null)
            {
                item.DisplayOrder = i + 1;
            }
        }

        eventEntity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { reordered = request.MediaIds.Count });
    }

    public sealed record ReorderMediaRequest(IReadOnlyList<Guid> MediaIds);
}
