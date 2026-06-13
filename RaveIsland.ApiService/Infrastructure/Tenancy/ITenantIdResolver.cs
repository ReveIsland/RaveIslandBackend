namespace RaveIsland.ApiService.Infrastructure.Tenancy;

public interface ITenantIdResolver
{
    Guid? GetTenantId();
}
