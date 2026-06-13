namespace RaveIsland.ApiService.Infrastructure.Tenancy;

public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ITenantMembershipResolver resolver)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            await resolver.ResolveTenantIdAsync(context.RequestAborted);
        }

        await next(context);
    }
}
