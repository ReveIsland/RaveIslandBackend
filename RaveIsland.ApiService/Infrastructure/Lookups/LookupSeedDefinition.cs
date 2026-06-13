namespace RaveIsland.ApiService.Infrastructure.Lookups;

public sealed record LookupSeedValueDefinition(string Code, string Name, int DisplayOrder);

public sealed record LookupSeedTypeDefinition(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    IReadOnlyList<LookupSeedValueDefinition> Values);
