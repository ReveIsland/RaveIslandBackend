namespace RaveIsland.ApiService.Common;

public static class AppRoles
{
    public const string Admin = "admin";
    public const string TenantAdmin = "tenant-admin";
    public const string TenantUser = "tenant-user";

    public static readonly string[] TenantRoles = [TenantAdmin, TenantUser];
}
