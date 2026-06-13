using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Infrastructure.Persistence;

namespace RaveIsland.ApiService.Infrastructure.Lookups;

public static class EventDefaults
{
    public static async Task<Guid?> GetDraftStatusIdAsync(AppDbContext db, CancellationToken ct) =>
        await LookupHelper.ResolveValueIdAsync(db, LookupTypeCodes.EventStatus, LookupValueCodes.EventStatusDraft, ct);

    public static async Task<Guid?> GetPublicVisibilityIdAsync(AppDbContext db, CancellationToken ct) =>
        await LookupHelper.ResolveValueIdAsync(db, LookupTypeCodes.EventVisibility, "Public", ct);

    public static async Task<Guid?> GetPublishedStatusIdAsync(AppDbContext db, CancellationToken ct) =>
        await LookupHelper.ResolveValueIdAsync(db, LookupTypeCodes.EventStatus, LookupValueCodes.EventStatusPublished, ct);
}
