using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Infrastructure.Lookups;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Persistence.Entities;

namespace RaveIsland.ApiService.Infrastructure.Billing;

public interface IEventPublishService
{
    Task<PublishEventResult> PublishAsync(EventEntity eventEntity, bool reportMeter, CancellationToken cancellationToken = default);
}

public sealed class PublishEventResult
{
    public required Guid Id { get; init; }
    public string? Slug { get; init; }
    public required string PublicUrl { get; init; }
}

public sealed class EventPublishService(AppDbContext db, IStripeMeterService meterService) : IEventPublishService
{
    public async Task<PublishEventResult> PublishAsync(
        EventEntity eventEntity,
        bool reportMeter,
        CancellationToken cancellationToken = default)
    {
        var publishedStatusId = await EventDefaults.GetPublishedStatusIdAsync(db, cancellationToken)
            ?? throw new InvalidOperationException("Published status lookup value is not seeded.");

        var wasAlreadyPublished = eventEntity.EventStatusId == publishedStatusId;

        eventEntity.EventStatusId = publishedStatusId;
        if (string.IsNullOrWhiteSpace(eventEntity.Slug))
        {
            eventEntity.Slug = GenerateSlug(eventEntity.Title, eventEntity.Id);
        }

        eventEntity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        if (!wasAlreadyPublished && reportMeter)
        {
            var tenant = await db.Tenants
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == eventEntity.TenantId, cancellationToken);

            if (!string.IsNullOrWhiteSpace(tenant?.StripeCustomerId))
            {
                await meterService.ReportEventPublishedAsync(
                    tenant.StripeCustomerId,
                    eventEntity.Id,
                    cancellationToken);
            }
        }

        return new PublishEventResult
        {
            Id = eventEntity.Id,
            Slug = eventEntity.Slug,
            PublicUrl = $"/events/{eventEntity.Slug ?? eventEntity.Id.ToString()}",
        };
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
