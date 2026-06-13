using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Lookups;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Persistence.Entities;

namespace RaveIsland.ApiService.Features.Lookups.CreateLookupValue;

public sealed class CreateLookupValueEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/lookups/{typeCode}/values", Handle).RequireAuthorization(AuthorizationPolicies.AdminOnly);

    private static async Task<IResult> Handle(
        string typeCode,
        CreateLookupValueRequest request,
        AppDbContext db,
        ILookupCacheService lookupCache,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { error = "Code and Name are required." });
        }

        var lookupType = await db.LookupTypes.FirstOrDefaultAsync(t => t.Code == typeCode, cancellationToken);
        if (lookupType is null)
        {
            return Results.NotFound(new { error = "Lookup type not found." });
        }

        var code = request.Code.Trim();
        if (await db.LookupValues.AnyAsync(v => v.LookupTypeId == lookupType.Id && v.Code == code, cancellationToken))
        {
            return Results.Conflict(new { error = "A value with this code already exists for this type." });
        }

        var maxOrder = await db.LookupValues
            .Where(v => v.LookupTypeId == lookupType.Id)
            .MaxAsync(v => (int?)v.DisplayOrder, cancellationToken) ?? 0;

        var lookupValue = new LookupValue
        {
            Id = Guid.NewGuid(),
            LookupTypeId = lookupType.Id,
            Code = code,
            Name = request.Name.Trim(),
            DisplayOrder = request.DisplayOrder ?? maxOrder + 1,
            IsActive = true,
            IsSystem = false,
            IconUrl = string.IsNullOrWhiteSpace(request.IconUrl) ? null : request.IconUrl.Trim(),
            MetadataJson = string.IsNullOrWhiteSpace(request.MetadataJson) ? null : request.MetadataJson.Trim(),
        };

        db.LookupValues.Add(lookupValue);
        await db.SaveChangesAsync(cancellationToken);
        await lookupCache.InvalidateTypeAsync(typeCode, cancellationToken);

        return Results.Created($"/api/lookups/values/{lookupValue.Id}", MapValue(lookupValue));
    }

    internal static object MapValue(LookupValue v) => new
    {
        v.Id,
        v.LookupTypeId,
        v.Code,
        v.Name,
        v.DisplayOrder,
        v.IsActive,
        v.IsSystem,
        v.IconUrl,
        v.MetadataJson,
    };

    public sealed record CreateLookupValueRequest(
        string Code,
        string Name,
        int? DisplayOrder,
        string? IconUrl,
        string? MetadataJson);
}
