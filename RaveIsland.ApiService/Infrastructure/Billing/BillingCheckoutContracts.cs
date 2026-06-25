namespace RaveIsland.ApiService.Infrastructure.Billing;

public static class BillingCheckoutContracts
{
    public const string CheckoutPurposeMetadataKey = "checkout_purpose";
    public const string EventPublishPurpose = "event_publish";
    public const string EventIdMetadataKey = "event_id";
    public const string EventNameMetadataKey = "event_name";
    public const string EventSlugMetadataKey = "event_slug";
    public const string TenantIdMetadataKey = "tenant_id";
    public const string TenantNameMetadataKey = "tenant_name";
    public const string TenantSlugMetadataKey = "tenant_slug";

    private const int StripeMetadataValueMaxLength = 500;

    public static Dictionary<string, string> BuildEventPublishMetadata(
        Guid eventId,
        string eventTitle,
        string? eventSlug,
        Guid tenantId,
        string tenantName,
        string tenantSlug)
    {
        var metadata = new Dictionary<string, string>
        {
            [CheckoutPurposeMetadataKey] = EventPublishPurpose,
            [EventIdMetadataKey] = eventId.ToString(),
            [EventNameMetadataKey] = TruncateMetadataValue(eventTitle),
            [TenantIdMetadataKey] = tenantId.ToString(),
            [TenantNameMetadataKey] = TruncateMetadataValue(tenantName),
            [TenantSlugMetadataKey] = TruncateMetadataValue(tenantSlug),
        };

        var slug = TruncateMetadataValue(eventSlug);
        if (!string.IsNullOrWhiteSpace(slug))
        {
            metadata[EventSlugMetadataKey] = slug;
        }

        return metadata;
    }

    public static string TruncateMetadataValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= StripeMetadataValueMaxLength
            ? trimmed
            : trimmed[..StripeMetadataValueMaxLength];
    }
}
