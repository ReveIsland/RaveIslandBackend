using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Billing;
using RaveIsland.ApiService.Infrastructure.Email;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Tenancy;
using Stripe;

namespace RaveIsland.ApiService.Features.Billing;

public sealed class GetBillingConfigEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/billing/config", Handle).AllowAnonymous();

    private static IResult Handle(IOptions<StripeOptions> options) =>
        Results.Ok(new
        {
            publishableKey = options.Value.PublishableKey,
            isConfigured = options.Value.IsConfigured,
        });
}

public sealed class GetBillingStatusEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/billing/status", Handle).RequireAuthorization(AuthorizationPolicies.TenantAdminOrAdmin);

    private static async Task<IResult> Handle(
        AppDbContext db,
        ITenantContext tenantContext,
        IStripeEntitlementService entitlementService,
        CancellationToken cancellationToken)
    {
        var tenantId = await ResolveTenantIdAsync(db, tenantContext, cancellationToken);
        if (tenantId is null)
        {
            return Results.BadRequest(new { error = "Tenant context is required." });
        }

        var status = await entitlementService.GetBillingStatusAsync(tenantId.Value, cancellationToken);
        return Results.Ok(status);
    }

    internal static async Task<Guid?> ResolveTenantIdAsync(
        AppDbContext db,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        if (tenantContext.TenantId.HasValue)
        {
            return tenantContext.TenantId;
        }

        return null;
    }
}

public sealed class GetBillingPlansEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/billing/plans", Handle).AllowAnonymous();

    private static async Task<IResult> Handle(
        IOptions<StripeOptions> options,
        CancellationToken cancellationToken)
    {
        if (!options.Value.IsConfigured)
        {
            return Results.Ok(Array.Empty<object>());
        }

        var priceService = new PriceService();
        var prices = await priceService.ListAsync(new PriceListOptions
        {
            Active = true,
            Type = "recurring",
            Expand = ["data.product"],
            Limit = 100,
        }, cancellationToken: cancellationToken);

        var plans = prices.Data
            .Where(p =>
            {
                var segment = p.Product?.Metadata?.GetValueOrDefault("segment");
                return segment is null or "organizer";
            })
            .OrderBy(p => p.UnitAmount ?? 0)
            .Select(p => new
            {
                priceId = p.Id,
                productId = p.ProductId,
                name = p.Product?.Name,
                description = p.Product?.Description,
                unitAmount = p.UnitAmount,
                currency = p.Currency,
                interval = p.Recurring?.Interval,
            });

        return Results.Ok(plans);
    }
}

public sealed class CreateCheckoutSessionEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/billing/checkout-session", Handle)
            .RequireAuthorization(AuthorizationPolicies.TenantAdminOrAdmin);

    private static async Task<IResult> Handle(
        CreateCheckoutSessionRequest request,
        AppDbContext db,
        ITenantContext tenantContext,
        IStripeCheckoutService checkoutService,
        IOptions<StripeOptions> stripeOptions,
        IOptions<AppOptions> appOptions,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PriceId))
        {
            return Results.BadRequest(new { error = "PriceId is required." });
        }

        var tenantId = await GetBillingStatusEndpoint.ResolveTenantIdAsync(db, tenantContext, cancellationToken);
        if (tenantId is null)
        {
            return Results.BadRequest(new { error = "Tenant context is required." });
        }

        var tenant = await db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant?.StripeCustomerId is null)
        {
            return Results.BadRequest(new { error = "Stripe customer is not set up for this tenant." });
        }

        var webBaseUrl = appOptions.Value.WebBaseUrl.TrimEnd('/');
        var promotionCode = request.PromotionCode ?? tenant.PendingPromotionCode;
        var checkoutUrl = await checkoutService.CreateSubscriptionCheckoutUrlAsync(
            tenant.StripeCustomerId,
            request.PriceId,
            $"{webBaseUrl}/settings/billing?checkout=success",
            $"{webBaseUrl}/settings/billing?checkout=cancel",
            promotionCode,
            cancellationToken);

        return Results.Ok(new { checkoutUrl });
    }

    public sealed record CreateCheckoutSessionRequest(string PriceId, string? PromotionCode);
}

public sealed class CreatePortalSessionEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/billing/portal-session", Handle)
            .RequireAuthorization(AuthorizationPolicies.TenantAdminOrAdmin);

    private static async Task<IResult> Handle(
        AppDbContext db,
        ITenantContext tenantContext,
        IStripePortalService portalService,
        IOptions<AppOptions> appOptions,
        CancellationToken cancellationToken)
    {
        var tenantId = await GetBillingStatusEndpoint.ResolveTenantIdAsync(db, tenantContext, cancellationToken);
        if (tenantId is null)
        {
            return Results.BadRequest(new { error = "Tenant context is required." });
        }

        var tenant = await db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant?.StripeCustomerId is null)
        {
            return Results.BadRequest(new { error = "Stripe customer is not set up for this tenant." });
        }

        var webBaseUrl = appOptions.Value.WebBaseUrl.TrimEnd('/');
        var portalUrl = await portalService.CreatePortalUrlAsync(
            tenant.StripeCustomerId,
            $"{webBaseUrl}/settings/billing",
            cancellationToken);

        return Results.Ok(new { portalUrl });
    }
}

public sealed class ConfirmCheckoutEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/billing/confirm-checkout", Handle).AllowAnonymous();

    private static async Task<IResult> Handle(
        ConfirmCheckoutRequest request,
        IStripeBillingSyncService billingSync,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId))
        {
            return Results.BadRequest(new { error = "SessionId is required." });
        }

        var synced = await billingSync.SyncFromCheckoutSessionAsync(request.SessionId, cancellationToken);
        if (!synced)
        {
            return Results.BadRequest(new { error = "Could not confirm checkout session." });
        }

        return Results.Ok(new { message = "Billing setup confirmed." });
    }

    public sealed record ConfirmCheckoutRequest(string SessionId);
}
