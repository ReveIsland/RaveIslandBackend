namespace RaveIsland.ApiService.Infrastructure.Persistence.Entities;

public class EventEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Title { get; set; }
    public string? Tagline { get; set; }
    public required string Description { get; set; }
    public Guid EventCategoryId { get; set; }
    public string? Theme { get; set; }
    public Guid EventStatusId { get; set; }
    public string? OrganizerReference { get; set; }
    public Guid? VenueTypeId { get; set; }
    public Guid? PrimaryGenreId { get; set; }
    public Guid? SecondaryGenreId { get; set; }
    public string? SoundSystem { get; set; }
    public Guid? AgeRestrictionId { get; set; }
    public Guid? CancellationPolicyId { get; set; }
    public string? EntryPolicy { get; set; }
    public string? ProhibitedItems { get; set; }
    public string? TermsAndConditions { get; set; }
    public Guid VisibilityTypeId { get; set; }
    public string? InviteCode { get; set; }
    public bool RequiresApproval { get; set; }
    public string? Slug { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public required string CreatedByUserId { get; set; }
    public string? CreatedByName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public LookupValue EventCategory { get; set; } = null!;
    public LookupValue EventStatus { get; set; } = null!;
    public LookupValue VisibilityType { get; set; } = null!;
    public LookupValue? VenueType { get; set; }
    public LookupValue? PrimaryGenre { get; set; }
    public LookupValue? SecondaryGenre { get; set; }
    public LookupValue? AgeRestriction { get; set; }
    public LookupValue? CancellationPolicy { get; set; }
    public Venue? Venue { get; set; }
    public ICollection<EventSchedule> Schedules { get; set; } = [];
    public ICollection<EventMedia> Media { get; set; } = [];
    public ICollection<EventArtist> Artists { get; set; } = [];
    public ICollection<EventTicketType> TicketTypes { get; set; } = [];
    public ICollection<EventPromoCode> PromoCodes { get; set; } = [];
    public ICollection<EventLookupSelection> LookupSelections { get; set; } = [];
}
