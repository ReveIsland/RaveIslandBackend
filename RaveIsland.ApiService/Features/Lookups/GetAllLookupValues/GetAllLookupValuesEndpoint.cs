using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Lookups;

namespace RaveIsland.ApiService.Features.Lookups.GetAllLookupValues;

public sealed class GetAllLookupValuesEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/lookups/{typeCode}/all", Handle).RequireAuthorization(AuthorizationPolicies.AdminOnly);

    private static async Task<IResult> Handle(
        string typeCode,
        ILookupCacheService lookupCache,
        CancellationToken cancellationToken)
    {
        var values = await lookupCache.GetValuesAsync(typeCode, includeInactive: true, cancellationToken);
        return Results.Ok(values);
    }
}
