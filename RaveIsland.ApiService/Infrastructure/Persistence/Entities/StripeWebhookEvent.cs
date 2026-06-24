namespace RaveIsland.ApiService.Infrastructure.Persistence.Entities;

public class StripeWebhookEvent
{
    public required string EventId { get; set; }
    public DateTimeOffset ProcessedAt { get; set; }
}
