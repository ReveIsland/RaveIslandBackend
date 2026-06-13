using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Tenancy;

namespace RaveIsland.ApiService.Features.Tenants.ListTenants;

public sealed class ListTenantsEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/tenants", Handle).RequireAuthorization();

    private static async Task<IResult> Handle(
        AppDbContext db,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var query = db.Tenants.AsNoTracking();

        if (!tenantContext.IsAdmin)
        {
            if (!tenantContext.TenantId.HasValue)
            {
                return Results.Forbid();
            }

            query = query.Where(t => t.Id == tenantContext.TenantId.Value);
        }

        var tenants = await query
            .OrderBy(t => t.Name)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Slug,
                t.IsActive,
                t.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(tenants);
    }
}
