using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Features.Lookups.CreateLookupValue;
using RaveIsland.ApiService.Infrastructure.Lookups;
using RaveIsland.ApiService.Infrastructure.Persistence;

namespace RaveIsland.ApiService.Features.Lookups.UpdateLookupValue;

public sealed class UpdateLookupValueEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPatch("/api/lookups/values/{id:guid}", Handle).RequireAuthorization(AuthorizationPolicies.AdminOnly);

    private static async Task<IResult> Handle(
        Guid id,
        UpdateLookupValueRequest request,
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

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            lookupValue.Name = request.Name.Trim();
        }

        if (request.DisplayOrder.HasValue)
        {
            lookupValue.DisplayOrder = request.DisplayOrder.Value;
        }

        if (request.IsActive.HasValue)
        {
            lookupValue.IsActive = request.IsActive.Value;
        }

        if (request.IconUrl is not null)
        {
            lookupValue.IconUrl = string.IsNullOrWhiteSpace(request.IconUrl) ? null : request.IconUrl.Trim();
        }

        if (request.MetadataJson is not null)
        {
            lookupValue.MetadataJson = string.IsNullOrWhiteSpace(request.MetadataJson)
                ? null
                : request.MetadataJson.Trim();
        }

        await db.SaveChangesAsync(cancellationToken);
        await lookupCache.InvalidateTypeAsync(lookupValue.LookupType.Code, cancellationToken);

        return Results.Ok(CreateLookupValueEndpoint.MapValue(lookupValue));
    }

    public sealed record UpdateLookupValueRequest(
        string? Name,
        int? DisplayOrder,
        bool? IsActive,
        string? IconUrl,
        string? MetadataJson);
}
