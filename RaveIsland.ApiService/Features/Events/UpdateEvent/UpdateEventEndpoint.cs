using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Lookups;
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
            if (string.IsNullOrWhiteSpace(request.Description))
            {
                return Results.BadRequest(new { error = "Description cannot be empty." });
            }

            eventEntity.Description = request.Description.Trim();
        }

        if (request.Tagline is not null)
        {
            eventEntity.Tagline = string.IsNullOrWhiteSpace(request.Tagline) ? null : request.Tagline.Trim();
        }

        if (request.Theme is not null)
        {
            eventEntity.Theme = string.IsNullOrWhiteSpace(request.Theme) ? null : request.Theme.Trim();
        }

        if (request.OrganizerReference is not null)
        {
            eventEntity.OrganizerReference = string.IsNullOrWhiteSpace(request.OrganizerReference)
                ? null
                : request.OrganizerReference.Trim();
        }

        if (request.EventCategoryId.HasValue)
        {
            if (!await LookupHelper.IsValidValueAsync(db, request.EventCategoryId.Value, LookupTypeCodes.EventCategory, cancellationToken))
            {
                return Results.BadRequest(new { error = "Invalid event category." });
            }

            eventEntity.EventCategoryId = request.EventCategoryId.Value;
        }

        if (request.EventStatusId.HasValue)
        {
            if (!await LookupHelper.IsValidValueAsync(db, request.EventStatusId.Value, LookupTypeCodes.EventStatus, cancellationToken))
            {
                return Results.BadRequest(new { error = "Invalid event status." });
            }

            eventEntity.EventStatusId = request.EventStatusId.Value;
        }

        if (request.VenueTypeId.HasValue)
        {
            if (!await LookupHelper.IsValidValueAsync(db, request.VenueTypeId.Value, LookupTypeCodes.VenueType, cancellationToken))
            {
                return Results.BadRequest(new { error = "Invalid venue type." });
            }

            eventEntity.VenueTypeId = request.VenueTypeId;
        }

        if (request.PrimaryGenreId.HasValue)
        {
            if (!await LookupHelper.IsValidValueAsync(db, request.PrimaryGenreId.Value, LookupTypeCodes.MusicGenre, cancellationToken))
            {
                return Results.BadRequest(new { error = "Invalid primary genre." });
            }

            eventEntity.PrimaryGenreId = request.PrimaryGenreId;
        }

        if (request.SecondaryGenreId.HasValue)
        {
            if (!await LookupHelper.IsValidValueAsync(db, request.SecondaryGenreId.Value, LookupTypeCodes.MusicGenre, cancellationToken))
            {
                return Results.BadRequest(new { error = "Invalid secondary genre." });
            }

            eventEntity.SecondaryGenreId = request.SecondaryGenreId;
        }

        if (request.SoundSystem is not null)
        {
            eventEntity.SoundSystem = string.IsNullOrWhiteSpace(request.SoundSystem) ? null : request.SoundSystem.Trim();
        }

        if (request.AgeRestrictionId.HasValue)
        {
            if (!await LookupHelper.IsValidValueAsync(db, request.AgeRestrictionId.Value, LookupTypeCodes.AgeRestriction, cancellationToken))
            {
                return Results.BadRequest(new { error = "Invalid age restriction." });
            }

            eventEntity.AgeRestrictionId = request.AgeRestrictionId;
        }

        if (request.CancellationPolicyId.HasValue)
        {
            if (!await LookupHelper.IsValidValueAsync(db, request.CancellationPolicyId.Value, LookupTypeCodes.CancellationPolicy, cancellationToken))
            {
                return Results.BadRequest(new { error = "Invalid cancellation policy." });
            }

            eventEntity.CancellationPolicyId = request.CancellationPolicyId;
        }

        if (request.EntryPolicy is not null)
        {
            eventEntity.EntryPolicy = string.IsNullOrWhiteSpace(request.EntryPolicy) ? null : request.EntryPolicy.Trim();
        }

        if (request.ProhibitedItems is not null)
        {
            eventEntity.ProhibitedItems = string.IsNullOrWhiteSpace(request.ProhibitedItems) ? null : request.ProhibitedItems.Trim();
        }

        if (request.TermsAndConditions is not null)
        {
            eventEntity.TermsAndConditions = string.IsNullOrWhiteSpace(request.TermsAndConditions)
                ? null
                : request.TermsAndConditions.Trim();
        }

        if (request.VisibilityTypeId.HasValue)
        {
            if (!await LookupHelper.IsValidValueAsync(db, request.VisibilityTypeId.Value, LookupTypeCodes.EventVisibility, cancellationToken))
            {
                return Results.BadRequest(new { error = "Invalid visibility type." });
            }

            eventEntity.VisibilityTypeId = request.VisibilityTypeId.Value;
        }

        if (request.InviteCode is not null)
        {
            eventEntity.InviteCode = string.IsNullOrWhiteSpace(request.InviteCode) ? null : request.InviteCode.Trim();
        }

        if (request.RequiresApproval.HasValue)
        {
            eventEntity.RequiresApproval = request.RequiresApproval.Value;
        }

        if (request.Slug is not null)
        {
            eventEntity.Slug = string.IsNullOrWhiteSpace(request.Slug) ? null : request.Slug.Trim().ToLowerInvariant();
        }

        if (request.MetaTitle is not null)
        {
            eventEntity.MetaTitle = string.IsNullOrWhiteSpace(request.MetaTitle) ? null : request.MetaTitle.Trim();
        }

        if (request.MetaDescription is not null)
        {
            eventEntity.MetaDescription = string.IsNullOrWhiteSpace(request.MetaDescription) ? null : request.MetaDescription.Trim();
        }

        eventEntity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(new { eventEntity.Id });
    }

    public sealed record UpdateEventRequest(
        string? Title,
        string? Description,
        string? Tagline,
        string? Theme,
        string? OrganizerReference,
        Guid? EventCategoryId,
        Guid? EventStatusId,
        Guid? VenueTypeId,
        Guid? PrimaryGenreId,
        Guid? SecondaryGenreId,
        string? SoundSystem,
        Guid? AgeRestrictionId,
        Guid? CancellationPolicyId,
        string? EntryPolicy,
        string? ProhibitedItems,
        string? TermsAndConditions,
        Guid? VisibilityTypeId,
        string? InviteCode,
        bool? RequiresApproval,
        string? Slug,
        string? MetaTitle,
        string? MetaDescription);
}
