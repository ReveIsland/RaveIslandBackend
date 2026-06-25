using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Infrastructure.Lookups;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Persistence.Entities;

namespace RaveIsland.ApiService.Features.Events.Publish;

public interface IEventPublishValidator
{
    Task<IReadOnlyList<string>> ValidateAsync(EventEntity eventEntity, CancellationToken cancellationToken = default);
}

public sealed class EventPublishValidator(AppDbContext db) : IEventPublishValidator
{
    public async Task<IReadOnlyList<string>> ValidateAsync(
        EventEntity eventEntity,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(eventEntity.Title))
        {
            errors.Add("Event name is required.");
        }

        if (string.IsNullOrWhiteSpace(eventEntity.Description))
        {
            errors.Add("Event description is required.");
        }

        if (eventEntity.EventCategoryId == Guid.Empty)
        {
            errors.Add("Event category is required.");
        }

        var hasSchedule = await db.EventSchedules.AnyAsync(s => s.EventId == eventEntity.Id, cancellationToken);
        if (!hasSchedule)
        {
            errors.Add("At least one event schedule is required.");
        }

        var hasVenue = await db.Venues.AnyAsync(v => v.EventId == eventEntity.Id, cancellationToken);
        if (!hasVenue)
        {
            errors.Add("Venue information is required.");
        }

        var hasCover = await db.EventMedia
            .IgnoreQueryFilters()
            .AnyAsync(
            m => m.EventId == eventEntity.Id && m.MediaType == EventMediaType.Cover,
            cancellationToken);
        if (!hasCover)
        {
            errors.Add("Cover image is required.");
        }

        return errors;
    }
}
