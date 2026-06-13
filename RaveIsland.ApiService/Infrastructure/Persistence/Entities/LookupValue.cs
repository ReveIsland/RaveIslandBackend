namespace RaveIsland.ApiService.Infrastructure.Persistence.Entities;

public class LookupValue
{
    public Guid Id { get; set; }
    public Guid LookupTypeId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSystem { get; set; }
    public string? IconUrl { get; set; }
    public string? MetadataJson { get; set; }

    public LookupType LookupType { get; set; } = null!;
}
