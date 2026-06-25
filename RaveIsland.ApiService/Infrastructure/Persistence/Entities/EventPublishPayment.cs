namespace RaveIsland.ApiService.Infrastructure.Persistence.Entities;

public static class EventPublishPaymentStatuses
{
    public const string Pending = "Pending";
    public const string Paid = "Paid";
}

public class EventPublishPayment
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid TenantId { get; set; }
    public required string StripeCheckoutSessionId { get; set; }
    public required string Status { get; set; }
    public int AmountCents { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }

    public EventEntity Event { get; set; } = null!;
}
