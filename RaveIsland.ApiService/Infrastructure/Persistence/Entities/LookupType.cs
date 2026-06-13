namespace RaveIsland.ApiService.Infrastructure.Persistence.Entities;

public class LookupType
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<LookupValue> Values { get; set; } = [];
}
