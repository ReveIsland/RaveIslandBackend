using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Lookups;

namespace RaveIsland.ApiService.Features.Lookups.ListLookupTypes;

public sealed class ListLookupTypesEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/lookups/types", Handle).RequireAuthorization(AuthorizationPolicies.AdminOnly);

    private static async Task<IResult> Handle(
        ILookupCacheService lookupCache,
        CancellationToken cancellationToken)
    {
        var types = await lookupCache.GetTypesAsync(cancellationToken);
        return Results.Ok(types);
    }
}
