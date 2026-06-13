namespace RaveIsland.ApiService.Infrastructure.Persistence.Entities;

public class EventPromoCode
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public required string Code { get; set; }
    public PromoDiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public int? UsageLimit { get; set; }
    public int UsageCount { get; set; }
    public string? AppliesToTicketTypeIdsJson { get; set; }
    public bool IsActive { get; set; } = true;

    public EventEntity Event { get; set; } = null!;
}
