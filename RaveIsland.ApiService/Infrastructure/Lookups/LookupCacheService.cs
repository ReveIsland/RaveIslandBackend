using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using RaveIsland.ApiService.Infrastructure.Persistence;

namespace RaveIsland.ApiService.Infrastructure.Lookups;

public sealed class LookupCacheService(AppDbContext db, IDistributedCache cache) : ILookupCacheService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<IReadOnlyList<LookupValueDto>> GetValuesAsync(
        string typeCode,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = includeInactive
            ? $"lookup:values:{typeCode}:all"
            : $"lookup:values:{typeCode}";

        var cached = await cache.GetStringAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return JsonSerializer.Deserialize<List<LookupValueDto>>(cached, JsonOptions) ?? [];
        }

        var values = await LoadValuesFromDbAsync(typeCode, includeInactive, cancellationToken);
        await cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(values, JsonOptions),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl },
            cancellationToken);

        return values;
    }

    public async Task<IReadOnlyList<LookupTypeDto>> GetTypesAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "lookup:types";
        var cached = await cache.GetStringAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return JsonSerializer.Deserialize<List<LookupTypeDto>>(cached, JsonOptions) ?? [];
        }

        var types = await db.LookupTypes
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new LookupTypeDto(t.Id, t.Code, t.Name, t.Description, t.IsSystem, t.CreatedAt))
            .ToListAsync(cancellationToken);

        await cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(types, JsonOptions),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl },
            cancellationToken);

        return types;
    }

    public async Task InvalidateTypeAsync(string typeCode, CancellationToken cancellationToken = default)
    {
        await cache.RemoveAsync($"lookup:values:{typeCode}", cancellationToken);
        await cache.RemoveAsync($"lookup:values:{typeCode}:all", cancellationToken);
        await cache.RemoveAsync("lookup:types", cancellationToken);
    }

    public Task InvalidateTypesAsync(CancellationToken cancellationToken = default) =>
        cache.RemoveAsync("lookup:types", cancellationToken);

    private async Task<List<LookupValueDto>> LoadValuesFromDbAsync(
        string typeCode,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var query = db.LookupValues
            .AsNoTracking()
            .Where(v => v.LookupType.Code == typeCode);

        if (!includeInactive)
        {
            query = query.Where(v => v.IsActive);
        }

        return await query
            .OrderBy(v => v.DisplayOrder)
            .ThenBy(v => v.Name)
            .Select(v => new LookupValueDto(
                v.Id,
                v.LookupTypeId,
                v.Code,
                v.Name,
                v.DisplayOrder,
                v.IsActive,
                v.IsSystem,
                v.IconUrl,
                v.MetadataJson))
            .ToListAsync(cancellationToken);
    }
}
