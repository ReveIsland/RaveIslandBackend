using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Infrastructure.Persistence;

namespace RaveIsland.ApiService.Infrastructure.Lookups;

public static class LookupHelper
{
    public static async Task<Guid?> ResolveValueIdAsync(
        AppDbContext db,
        string typeCode,
        string valueCode,
        CancellationToken cancellationToken = default)
    {
        return await db.LookupValues
            .AsNoTracking()
            .Where(v => v.LookupType.Code == typeCode && v.Code == valueCode && v.IsActive)
            .Select(v => (Guid?)v.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public static async Task<bool> IsValidValueAsync(
        AppDbContext db,
        Guid valueId,
        string typeCode,
        CancellationToken cancellationToken = default)
    {
        return await db.LookupValues
            .AsNoTracking()
            .AnyAsync(v => v.Id == valueId && v.LookupType.Code == typeCode && v.IsActive, cancellationToken);
    }
}
