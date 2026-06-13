using RaveIsland.ApiService.Infrastructure.Persistence.Entities;

namespace RaveIsland.ApiService.Infrastructure.Lookups;

public interface ILookupCacheService
{
    Task<IReadOnlyList<LookupValueDto>> GetValuesAsync(string typeCode, bool includeInactive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupTypeDto>> GetTypesAsync(CancellationToken cancellationToken = default);
    Task InvalidateTypeAsync(string typeCode, CancellationToken cancellationToken = default);
    Task InvalidateTypesAsync(CancellationToken cancellationToken = default);
}

public sealed record LookupValueDto(
    Guid Id,
    Guid LookupTypeId,
    string Code,
    string Name,
    int DisplayOrder,
    bool IsActive,
    bool IsSystem,
    string? IconUrl,
    string? MetadataJson);

public sealed record LookupTypeDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsSystem,
    DateTimeOffset CreatedAt);
