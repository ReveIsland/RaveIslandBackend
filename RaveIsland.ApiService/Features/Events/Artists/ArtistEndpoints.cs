using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Lookups;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Persistence.Entities;
using RaveIsland.ApiService.Infrastructure.Tenancy;

namespace RaveIsland.ApiService.Features.Events.Artists;

public sealed class ListArtistsEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/artists", Handle).RequireAuthorization(AuthorizationPolicies.TenantMember);

    private static async Task<IResult> Handle(
        AppDbContext db,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var query = tenantContext.IsAdmin
            ? db.Artists.IgnoreQueryFilters().AsNoTracking()
            : db.Artists.AsNoTracking();

        var artists = await query.OrderBy(a => a.Name).ToListAsync(cancellationToken);
        return Results.Ok(artists);
    }
}

public sealed class CreateArtistEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/artists", Handle).RequireAuthorization(AuthorizationPolicies.TenantMember);

    private static async Task<IResult> Handle(
        CreateArtistRequest request,
        AppDbContext db,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { error = "Artist name is required." });
        }

        var tenantId = tenantContext.TenantId;
        if (!tenantId.HasValue && !tenantContext.IsAdmin)
        {
            return Results.BadRequest(new { error = "Tenant context is required." });
        }

        if (request.ArtistTypeId.HasValue &&
            !await LookupHelper.IsValidValueAsync(db, request.ArtistTypeId.Value, LookupTypeCodes.ArtistType, cancellationToken))
        {
            return Results.BadRequest(new { error = "Invalid artist type." });
        }

        if (request.PrimaryGenreId.HasValue &&
            !await LookupHelper.IsValidValueAsync(db, request.PrimaryGenreId.Value, LookupTypeCodes.MusicGenre, cancellationToken))
        {
            return Results.BadRequest(new { error = "Invalid genre." });
        }

        var artist = new Artist
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId ?? request.TenantId ?? Guid.Empty,
            Name = request.Name.Trim(),
            StageName = string.IsNullOrWhiteSpace(request.StageName) ? null : request.StageName.Trim(),
            Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim(),
            ProfileImageUrl = string.IsNullOrWhiteSpace(request.ProfileImageUrl) ? null : request.ProfileImageUrl.Trim(),
            ArtistTypeId = request.ArtistTypeId,
            PrimaryGenreId = request.PrimaryGenreId,
            SocialLinksJson = request.SocialLinksJson,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.Artists.Add(artist);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/artists/{artist.Id}", new { artist.Id });
    }

    public sealed record CreateArtistRequest(
        string Name,
        string? StageName,
        string? Bio,
        string? ProfileImageUrl,
        Guid? ArtistTypeId,
        Guid? PrimaryGenreId,
        string? SocialLinksJson,
        Guid? TenantId);
}

public sealed class ManageEventLineupEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/api/events/{eventId:guid}/lineup", Handle).RequireAuthorization(AuthorizationPolicies.TenantMember);

    private static async Task<IResult> Handle(
        Guid eventId,
        UpdateLineupRequest request,
        AppDbContext db,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var eventEntity = await EventQueryHelper.FindEventAsync(db, tenantContext, eventId, cancellationToken);
        if (eventEntity is null) return Results.NotFound(new { error = "Event not found." });
        var access = EventQueryHelper.CheckAccess(tenantContext, eventEntity);
        if (access is not null) return access;

        var existing = await db.EventArtists.Where(a => a.EventId == eventId).ToListAsync(cancellationToken);
        db.EventArtists.RemoveRange(existing);

        foreach (var item in request.Artists)
        {
            var artistExists = await db.Artists.AnyAsync(a => a.Id == item.ArtistId, cancellationToken);
            if (!artistExists)
            {
                return Results.BadRequest(new { error = $"Artist {item.ArtistId} not found." });
            }

            db.EventArtists.Add(new EventArtist
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                ArtistId = item.ArtistId,
                StageNameOverride = item.StageNameOverride,
                PrimaryGenreId = item.PrimaryGenreId,
                SetStart = item.SetStart,
                SetEnd = item.SetEnd,
                DisplayOrder = item.DisplayOrder,
            });
        }

        eventEntity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { count = request.Artists.Count });
    }

    public sealed record LineupItem(
        Guid ArtistId,
        string? StageNameOverride,
        Guid? PrimaryGenreId,
        TimeOnly? SetStart,
        TimeOnly? SetEnd,
        int DisplayOrder);

    public sealed record UpdateLineupRequest(IReadOnlyList<LineupItem> Artists);
}
