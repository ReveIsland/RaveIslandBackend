using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Lookups;

namespace RaveIsland.ApiService.Features.Lookups.GetLookupValues;

public sealed class GetLookupValuesEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/lookups/{typeCode}", Handle);

    private static async Task<IResult> Handle(
        string typeCode,
        ILookupCacheService lookupCache,
        CancellationToken cancellationToken)
    {
        var values = await lookupCache.GetValuesAsync(typeCode, includeInactive: false, cancellationToken);
        return Results.Ok(values);
    }
}
