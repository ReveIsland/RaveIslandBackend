namespace RaveIsland.ApiService.Infrastructure.Persistence.Entities;

public class Venue
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public required string VenueName { get; set; }
    public required string Address { get; set; }
    public required string City { get; set; }
    public Guid DistrictId { get; set; }
    public string? Province { get; set; }
    public string? GoogleMapsUrl { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? LandmarkInstructions { get; set; }

    public EventEntity Event { get; set; } = null!;
    public LookupValue District { get; set; } = null!;
}
