namespace RaveIsland.ApiService.Infrastructure.Billing;

public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    public string SecretKey { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string FreePriceId { get; set; } = string.Empty;
    public string StarterPriceId { get; set; } = string.Empty;
    public string ProPriceId { get; set; } = string.Empty;
    public string EventsPublishedMeterId { get; set; } = string.Empty;
    public string EventsPublishedMeterEventName { get; set; } = "events_published";
    public int FreeTierPublishCredits { get; set; } = 1;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(SecretKey);
}
