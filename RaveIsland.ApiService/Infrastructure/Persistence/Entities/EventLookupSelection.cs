namespace RaveIsland.ApiService.Infrastructure.Persistence.Entities;

public class EventLookupSelection
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid LookupValueId { get; set; }
    public required string LookupTypeCode { get; set; }

    public EventEntity Event { get; set; } = null!;
    public LookupValue LookupValue { get; set; } = null!;
}
