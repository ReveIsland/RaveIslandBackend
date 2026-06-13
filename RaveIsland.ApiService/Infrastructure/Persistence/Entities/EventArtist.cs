namespace RaveIsland.ApiService.Infrastructure.Persistence.Entities;

public class EventArtist
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid ArtistId { get; set; }
    public string? StageNameOverride { get; set; }
    public Guid? PrimaryGenreId { get; set; }
    public TimeOnly? SetStart { get; set; }
    public TimeOnly? SetEnd { get; set; }
    public int DisplayOrder { get; set; }

    public EventEntity Event { get; set; } = null!;
    public Artist Artist { get; set; } = null!;
    public LookupValue? PrimaryGenre { get; set; }
}
