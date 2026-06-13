namespace RaveIsland.ApiService.Infrastructure.Tenancy;

public sealed class TenantIdResolver(IHttpContextAccessor httpContextAccessor, IServiceScopeFactory scopeFactory)
    : ITenantIdResolver
{
    public Guid? GetTenantId() =>
        TenantIdResolutionHelper.Resolve(httpContextAccessor, scopeFactory);
}
