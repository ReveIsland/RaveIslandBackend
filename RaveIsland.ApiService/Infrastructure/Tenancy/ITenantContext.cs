namespace RaveIsland.ApiService.Infrastructure.Tenancy;

public interface ITenantContext
{
    string? UserId { get; }
    Guid? TenantId { get; }
    bool IsAdmin { get; }
    bool IsTenantAdmin { get; }
    bool IsTenantUser { get; }
    bool BypassTenantFilters { get; }
    IReadOnlyList<string> Roles { get; }
}
