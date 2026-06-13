namespace RaveIsland.ApiService.Infrastructure.Persistence.Entities;

public class Artist
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Name { get; set; }
    public string? StageName { get; set; }
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
    public Guid? ArtistTypeId { get; set; }
    public Guid? PrimaryGenreId { get; set; }
    public string? SocialLinksJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public LookupValue? ArtistType { get; set; }
    public LookupValue? PrimaryGenre { get; set; }
    public ICollection<EventArtist> EventArtists { get; set; } = [];
}
