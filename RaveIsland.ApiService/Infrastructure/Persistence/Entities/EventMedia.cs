namespace RaveIsland.ApiService.Infrastructure.Persistence.Entities;

public class EventMedia
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public EventMediaType MediaType { get; set; }
    public required string StorageUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int DisplayOrder { get; set; }
    public required string FileName { get; set; }

    public EventEntity Event { get; set; } = null!;
}
