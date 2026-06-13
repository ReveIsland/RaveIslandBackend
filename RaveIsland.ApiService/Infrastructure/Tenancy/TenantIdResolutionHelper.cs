namespace RaveIsland.ApiService.Infrastructure.Tenancy;

internal static class TenantIdResolutionHelper
{
    public static Guid? Resolve(IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return null;
        }

        if (httpContext.Items.TryGetValue(TenantMembershipResolver.TenantIdItemKey, out var cached) &&
            cached is Guid tenantId)
        {
            return tenantId;
        }

        return null;
    }
}
