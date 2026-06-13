namespace RaveIsland.ApiService.Infrastructure.Lookups;

public static class LookupTypeCodes
{
    public const string EventCategory = "EventCategory";
    public const string MusicGenre = "MusicGenre";
    public const string VenueType = "VenueType";
    public const string Facility = "Facility";
    public const string ProductionFeature = "ProductionFeature";
    public const string TicketType = "TicketType";
    public const string AgeRestriction = "AgeRestriction";
    public const string EventVisibility = "EventVisibility";
    public const string EventStatus = "EventStatus";
    public const string PaymentMethod = "PaymentMethod";
    public const string District = "District";
    public const string ArtistType = "ArtistType";
    public const string SocialMediaProvider = "SocialMediaProvider";
    public const string CancellationPolicy = "CancellationPolicy";
}

public static class LookupTypeIds
{
    public static readonly Guid EventCategory = Guid.Parse("11111111-1111-4111-8111-111111110001");
    public static readonly Guid MusicGenre = Guid.Parse("11111111-1111-4111-8111-111111110002");
    public static readonly Guid VenueType = Guid.Parse("11111111-1111-4111-8111-111111110003");
    public static readonly Guid Facility = Guid.Parse("11111111-1111-4111-8111-111111110004");
    public static readonly Guid ProductionFeature = Guid.Parse("11111111-1111-4111-8111-111111110005");
    public static readonly Guid TicketType = Guid.Parse("11111111-1111-4111-8111-111111110006");
    public static readonly Guid AgeRestriction = Guid.Parse("11111111-1111-4111-8111-111111110007");
    public static readonly Guid EventVisibility = Guid.Parse("11111111-1111-4111-8111-111111110008");
    public static readonly Guid EventStatus = Guid.Parse("11111111-1111-4111-8111-111111110009");
    public static readonly Guid PaymentMethod = Guid.Parse("11111111-1111-4111-8111-11111111000a");
    public static readonly Guid District = Guid.Parse("11111111-1111-4111-8111-11111111000b");
    public static readonly Guid ArtistType = Guid.Parse("11111111-1111-4111-8111-11111111000c");
    public static readonly Guid SocialMediaProvider = Guid.Parse("11111111-1111-4111-8111-11111111000d");
    public static readonly Guid CancellationPolicy = Guid.Parse("11111111-1111-4111-8111-11111111000e");
}

public static class LookupValueCodes
{
    public const string EventStatusDraft = "Draft";
    public const string EventStatusPublished = "Published";
}
