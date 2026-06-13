using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Lookups;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Persistence.Entities;

namespace RaveIsland.ApiService.Features.Lookups.CreateLookupType;

public sealed class CreateLookupTypeEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/lookups/types", Handle).RequireAuthorization(AuthorizationPolicies.AdminOnly);

    private static async Task<IResult> Handle(
        CreateLookupTypeRequest request,
        AppDbContext db,
        ILookupCacheService lookupCache,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { error = "Code and Name are required." });
        }

        var code = request.Code.Trim();
        if (await db.LookupTypes.AnyAsync(t => t.Code == code, cancellationToken))
        {
            return Results.Conflict(new { error = "A lookup type with this code already exists." });
        }

        var lookupType = new LookupType
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = request.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            IsSystem = false,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.LookupTypes.Add(lookupType);
        await db.SaveChangesAsync(cancellationToken);
        await lookupCache.InvalidateTypesAsync(cancellationToken);

        return Results.Created($"/api/lookups/types/{lookupType.Id}", new
        {
            lookupType.Id,
            lookupType.Code,
            lookupType.Name,
            lookupType.Description,
            lookupType.IsSystem,
            lookupType.CreatedAt,
        });
    }

    public sealed record CreateLookupTypeRequest(string Code, string Name, string? Description);
}
