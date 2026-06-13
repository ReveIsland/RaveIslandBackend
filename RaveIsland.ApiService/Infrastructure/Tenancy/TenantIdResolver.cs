namespace RaveIsland.ApiService.Infrastructure.Tenancy;

public sealed class TenantIdResolver(IHttpContextAccessor httpContextAccessor) : ITenantIdResolver
{
    public Guid? GetTenantId() =>
        TenantIdResolutionHelper.Resolve(httpContextAccessor);
}
