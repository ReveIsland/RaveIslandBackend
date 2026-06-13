namespace RaveIsland.ApiService.Common;

public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string TenantAdminOnly = "TenantAdminOnly";
    public const string TenantMember = "TenantMember";
    public const string TenantAdminOrAdmin = "TenantAdminOrAdmin";
}
