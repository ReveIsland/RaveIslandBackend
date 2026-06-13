using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Lookups;
using RaveIsland.ApiService.Infrastructure.Persistence;

namespace RaveIsland.ApiService.Features.Lookups.DeleteLookupValue;

public sealed class DeleteLookupValueEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/api/lookups/values/{id:guid}", Handle).RequireAuthorization(AuthorizationPolicies.AdminOnly);

    private static async Task<IResult> Handle(
        Guid id,
        AppDbContext db,
        ILookupCacheService lookupCache,
        CancellationToken cancellationToken)
    {
        var lookupValue = await db.LookupValues
            .Include(v => v.LookupType)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        if (lookupValue is null)
        {
            return Results.NotFound(new { error = "Lookup value not found." });
        }

        if (lookupValue.IsSystem)
        {
            lookupValue.IsActive = false;
            await db.SaveChangesAsync(cancellationToken);
            await lookupCache.InvalidateTypeAsync(lookupValue.LookupType.Code, cancellationToken);
            return Results.Ok(new { deactivated = true, id });
        }

        db.LookupValues.Remove(lookupValue);
        await db.SaveChangesAsync(cancellationToken);
        await lookupCache.InvalidateTypeAsync(lookupValue.LookupType.Code, cancellationToken);

        return Results.NoContent();
    }
}
