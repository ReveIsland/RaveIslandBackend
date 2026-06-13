using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Infrastructure.Lookups;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Persistence.Entities;

namespace RaveIsland.ApiService.Features.Events;

internal static class EventMapper
{
    public static IQueryable<EventEntity> WithDetails(this IQueryable<EventEntity> query) =>
        query
            .Include(e => e.EventCategory)
            .Include(e => e.EventStatus)
            .Include(e => e.VisibilityType)
            .Include(e => e.VenueType)
            .Include(e => e.PrimaryGenre)
            .Include(e => e.SecondaryGenre)
            .Include(e => e.AgeRestriction)
            .Include(e => e.CancellationPolicy)
            .Include(e => e.Venue!).ThenInclude(v => v.District)
            .Include(e => e.Schedules)
            .Include(e => e.Media)
            .Include(e => e.Artists).ThenInclude(a => a.Artist)
            .Include(e => e.TicketTypes)
            .Include(e => e.PromoCodes)
            .Include(e => e.LookupSelections).ThenInclude(s => s.LookupValue);

    public static object MapSummary(EventEntity e) => new
    {
        e.Id,
        e.TenantId,
        tenantName = e.Tenant?.Name,
        e.Title,
        e.Tagline,
        e.Description,
        eventCategoryId = e.EventCategoryId,
        eventCategoryName = e.EventCategory?.Name,
        eventStatusId = e.EventStatusId,
        eventStatusName = e.EventStatus?.Name,
        e.Theme,
        e.OrganizerReference,
        visibilityTypeId = e.VisibilityTypeId,
        visibilityTypeName = e.VisibilityType?.Name,
        e.Slug,
        e.CreatedByUserId,
        e.CreatedByName,
        e.CreatedAt,
        e.UpdatedAt,
    };

    public static object MapDetail(EventEntity e) => new
    {
        e.Id,
        e.TenantId,
        tenantName = e.Tenant?.Name,
        e.Title,
        e.Tagline,
        e.Description,
        eventCategoryId = e.EventCategoryId,
        eventCategoryName = e.EventCategory?.Name,
        eventStatusId = e.EventStatusId,
        eventStatusName = e.EventStatus?.Name,
        e.Theme,
        e.OrganizerReference,
        venueTypeId = e.VenueTypeId,
        venueTypeName = e.VenueType?.Name,
        primaryGenreId = e.PrimaryGenreId,
        primaryGenreName = e.PrimaryGenre?.Name,
        secondaryGenreId = e.SecondaryGenreId,
        secondaryGenreName = e.SecondaryGenre?.Name,
        e.SoundSystem,
        ageRestrictionId = e.AgeRestrictionId,
        ageRestrictionName = e.AgeRestriction?.Name,
        cancellationPolicyId = e.CancellationPolicyId,
        cancellationPolicyName = e.CancellationPolicy?.Name,
        e.EntryPolicy,
        e.ProhibitedItems,
        e.TermsAndConditions,
        visibilityTypeId = e.VisibilityTypeId,
        visibilityTypeName = e.VisibilityType?.Name,
        e.InviteCode,
        e.RequiresApproval,
        e.Slug,
        e.MetaTitle,
        e.MetaDescription,
        e.CreatedByUserId,
        e.CreatedByName,
        e.CreatedAt,
        e.UpdatedAt,
        venue = e.Venue is null ? null : new
        {
            e.Venue.Id,
            e.Venue.VenueName,
            e.Venue.Address,
            e.Venue.City,
            districtId = e.Venue.DistrictId,
            districtName = e.Venue.District?.Name,
            e.Venue.Province,
            e.Venue.GoogleMapsUrl,
            e.Venue.Latitude,
            e.Venue.Longitude,
            e.Venue.LandmarkInstructions,
        },
        schedules = e.Schedules.OrderBy(s => s.DayNumber).Select(s => new
        {
            s.Id,
            s.DayNumber,
            s.EventDate,
            s.StartTime,
            s.EndTime,
            s.GatesOpenTime,
            s.LastEntryTime,
        }),
        media = e.Media.OrderBy(m => m.DisplayOrder).Select(m => new
        {
            m.Id,
            mediaType = m.MediaType.ToString(),
            m.StorageUrl,
            m.ThumbnailUrl,
            m.DisplayOrder,
            m.FileName,
        }),
        artists = e.Artists.OrderBy(a => a.DisplayOrder).Select(a => new
        {
            a.Id,
            a.ArtistId,
            artistName = a.Artist?.Name,
            stageName = a.StageNameOverride ?? a.Artist?.StageName,
            a.SetStart,
            a.SetEnd,
            a.DisplayOrder,
            primaryGenreId = a.PrimaryGenreId,
        }),
        ticketTypes = e.TicketTypes.OrderBy(t => t.DisplayOrder).Select(t => new
        {
            t.Id,
            t.Name,
            t.Description,
            t.Price,
            t.Quantity,
            t.QuantitySold,
            t.SaleStart,
            t.SaleEnd,
            t.MaxPerUser,
            t.IsActive,
            t.DisplayOrder,
        }),
        promoCodes = e.PromoCodes.Select(p => new
        {
            p.Id,
            p.Code,
            discountType = p.DiscountType.ToString(),
            p.DiscountValue,
            p.ExpiresAt,
            p.UsageLimit,
            p.UsageCount,
            p.IsActive,
        }),
        facilities = e.LookupSelections
            .Where(s => s.LookupTypeCode == LookupTypeCodes.Facility)
            .Select(s => new { s.LookupValueId, name = s.LookupValue.Name }),
        productionFeatures = e.LookupSelections
            .Where(s => s.LookupTypeCode == LookupTypeCodes.ProductionFeature)
            .Select(s => new { s.LookupValueId, name = s.LookupValue.Name }),
    };
}
