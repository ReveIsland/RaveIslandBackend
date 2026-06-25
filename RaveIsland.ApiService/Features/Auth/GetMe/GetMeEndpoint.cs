using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Billing;
using RaveIsland.ApiService.Infrastructure.Identity;
using RaveIsland.ApiService.Infrastructure.Persistence;

using RaveIsland.ApiService.Infrastructure.Tenancy;

namespace RaveIsland.ApiService.Features.Auth.GetMe;

public sealed class GetMeEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/me", Handle).RequireAuthorization();

    private static async Task<IResult> Handle(
        ClaimsPrincipal user,
        HttpContext httpContext,
        AppDbContext db,
        IConfiguration configuration,
        IHostEnvironment environment,
        ITenantIdResolver tenantIdResolver,
        ITenantMembershipResolver tenantMembershipResolver,
        IStripeEntitlementService billingService,
        CancellationToken cancellationToken)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return Results.Unauthorized();
        }

        var profile = await KeycloakClaims.ResolveUserProfileAsync(
            user,
            httpContext,
            configuration,
            environment,
            cancellationToken);

        var tenantId = tenantIdResolver.GetTenantId()
            ?? await tenantMembershipResolver.ResolveTenantIdAsync(cancellationToken);
        string? tenantName = null;

        if (tenantId.HasValue)
        {
            tenantName = await db.Tenants
                .AsNoTracking()
                .Where(t => t.Id == tenantId.Value)
                .Select(t => t.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        object? organizationSubscription = null;
        var roles = KeycloakClaims.GetRoles(user);
        var isOrgMember = roles.Contains(AppRoles.TenantAdmin, StringComparer.OrdinalIgnoreCase)
            || roles.Contains(AppRoles.TenantUser, StringComparer.OrdinalIgnoreCase);

        if (tenantId.HasValue && isOrgMember)
        {
            var billing = await billingService.GetCachedBillingStatusAsync(tenantId.Value, cancellationToken);
            var planName = billing.PlanName
                ?? (billing.IsSubscribed ? "Subscribed" : billing.BillingSetupCompleted ? "Active" : null);

            organizationSubscription = new
            {
                planName,
                status = billing.SubscriptionStatus,
                isSubscribed = billing.IsSubscribed,
            };
        }

        return Results.Ok(new
        {
            name = profile.Name,
            email = profile.Email,
            roles = profile.Roles,
            tenantId,
            tenantName,
            organizationSubscription,
        });
    }
}
