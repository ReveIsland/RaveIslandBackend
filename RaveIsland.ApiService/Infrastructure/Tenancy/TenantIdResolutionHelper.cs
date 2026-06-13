using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Infrastructure.Persistence;

namespace RaveIsland.ApiService.Infrastructure.Tenancy;

internal static class TenantIdResolutionHelper
{
    private const string TenantIdItemKey = "RaveIsland.TenantId";
    private const string TenantIdResolvedKey = "RaveIsland.TenantIdResolved";

    public static Guid? Resolve(IHttpContextAccessor httpContextAccessor, IServiceScopeFactory scopeFactory)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return null;
        }

        if (httpContext.Items.TryGetValue(TenantIdItemKey, out var cached) && cached is Guid tenantId)
        {
            return tenantId;
        }

        if (httpContext.Items.ContainsKey(TenantIdResolvedKey))
        {
            return null;
        }

        var user = httpContext.User;
        var tenantClaim = user?.FindFirst("tenant_id")?.Value;
        if (Guid.TryParse(tenantClaim, out tenantId))
        {
            httpContext.Items[TenantIdItemKey] = tenantId;
            httpContext.Items[TenantIdResolvedKey] = true;
            return tenantId;
        }

        var userId = user?.FindFirst("sub")?.Value;
        Guid? resolvedTenantId = null;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            resolvedTenantId = db.TenantMemberships
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(m => m.KeycloakUserId == userId && m.IsActive)
                .Select(m => (Guid?)m.TenantId)
                .FirstOrDefault();
        }

        if (resolvedTenantId.HasValue)
        {
            httpContext.Items[TenantIdItemKey] = resolvedTenantId.Value;
        }

        httpContext.Items[TenantIdResolvedKey] = true;
        return resolvedTenantId;
    }
}
