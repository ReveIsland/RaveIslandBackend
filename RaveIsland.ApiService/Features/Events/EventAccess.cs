using RaveIsland.ApiService.Infrastructure.Tenancy;

namespace RaveIsland.ApiService.Features.Events;

internal static class EventAccess
{
    public static bool CanModify(ITenantContext tenantContext, string createdByUserId)
    {
        if (tenantContext.IsAdmin || tenantContext.IsTenantAdmin)
        {
            return true;
        }

        return tenantContext.IsTenantUser &&
               string.Equals(tenantContext.UserId, createdByUserId, StringComparison.Ordinal);
    }
}
