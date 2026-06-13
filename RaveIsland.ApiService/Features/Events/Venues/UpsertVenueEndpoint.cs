using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Lookups;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Persistence.Entities;
using RaveIsland.ApiService.Infrastructure.Tenancy;

namespace RaveIsland.ApiService.Features.Events.Venues;

public sealed class UpsertVenueEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/api/events/{eventId:guid}/venue", Handle).RequireAuthorization(AuthorizationPolicies.TenantMember);

    private static async Task<IResult> Handle(
        Guid eventId,
        UpsertVenueRequest request,
        AppDbContext db,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.VenueName))
        {
            return Results.BadRequest(new { error = "Venue name is required." });
        }

        if (!await LookupHelper.IsValidValueAsync(db, request.DistrictId, LookupTypeCodes.District, cancellationToken))
        {
            return Results.BadRequest(new { error = "Invalid district." });
        }

        var eventEntity = await EventQueryHelper.FindEventAsync(db, tenantContext, eventId, cancellationToken);
        if (eventEntity is null) return Results.NotFound(new { error = "Event not found." });
        var access = EventQueryHelper.CheckAccess(tenantContext, eventEntity);
        if (access is not null) return access;

        var venue = await db.Venues.FirstOrDefaultAsync(v => v.EventId == eventId, cancellationToken);
        if (venue is null)
        {
            venue = new Venue
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                VenueName = string.Empty,
                Address = string.Empty,
                City = string.Empty,
            };
            db.Venues.Add(venue);
        }

        venue.VenueName = request.VenueName.Trim();
        venue.Address = request.Address.Trim();
        venue.City = request.City.Trim();
        venue.DistrictId = request.DistrictId;
        venue.Province = string.IsNullOrWhiteSpace(request.Province) ? null : request.Province.Trim();
        venue.GoogleMapsUrl = string.IsNullOrWhiteSpace(request.GoogleMapsUrl) ? null : request.GoogleMapsUrl.Trim();
        venue.Latitude = request.Latitude;
        venue.Longitude = request.Longitude;
        venue.LandmarkInstructions = string.IsNullOrWhiteSpace(request.LandmarkInstructions)
            ? null
            : request.LandmarkInstructions.Trim();

        eventEntity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { venue.Id });
    }

    public sealed record UpsertVenueRequest(
        string VenueName,
        string Address,
        string City,
        Guid DistrictId,
        string? Province,
        string? GoogleMapsUrl,
        double Latitude,
        double Longitude,
        string? LandmarkInstructions);
}
