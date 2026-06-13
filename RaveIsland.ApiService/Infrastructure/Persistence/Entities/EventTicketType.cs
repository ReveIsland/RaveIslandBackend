namespace RaveIsland.ApiService.Infrastructure.Persistence.Entities;

public class EventTicketType
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public int QuantitySold { get; set; }
    public DateTimeOffset? SaleStart { get; set; }
    public DateTimeOffset? SaleEnd { get; set; }
    public int? MaxPerUser { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? DefaultLookupValueId { get; set; }
    public int DisplayOrder { get; set; }

    public EventEntity Event { get; set; } = null!;
    public LookupValue? DefaultLookupValue { get; set; }
    public ICollection<Ticket> Tickets { get; set; } = [];
}
