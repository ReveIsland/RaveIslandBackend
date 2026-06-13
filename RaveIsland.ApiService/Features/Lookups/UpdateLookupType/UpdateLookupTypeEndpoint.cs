using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Lookups;
using RaveIsland.ApiService.Infrastructure.Persistence;

namespace RaveIsland.ApiService.Features.Lookups.UpdateLookupType;

public sealed class UpdateLookupTypeEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPatch("/api/lookups/types/{id:guid}", Handle).RequireAuthorization(AuthorizationPolicies.AdminOnly);

    private static async Task<IResult> Handle(
        Guid id,
        UpdateLookupTypeRequest request,
        AppDbContext db,
        ILookupCacheService lookupCache,
        CancellationToken cancellationToken)
    {
        var lookupType = await db.LookupTypes.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (lookupType is null)
        {
            return Results.NotFound(new { error = "Lookup type not found." });
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            lookupType.Name = request.Name.Trim();
        }

        if (request.Description is not null)
        {
            lookupType.Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim();
        }

        await db.SaveChangesAsync(cancellationToken);
        await lookupCache.InvalidateTypesAsync(cancellationToken);
        await lookupCache.InvalidateTypeAsync(lookupType.Code, cancellationToken);

        return Results.Ok(new
        {
            lookupType.Id,
            lookupType.Code,
            lookupType.Name,
            lookupType.Description,
            lookupType.IsSystem,
            lookupType.CreatedAt,
        });
    }

    public sealed record UpdateLookupTypeRequest(string? Name, string? Description);
}
