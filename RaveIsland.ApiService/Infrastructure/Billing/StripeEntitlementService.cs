using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Persistence.Entities;
using Stripe;
using Stripe.Billing;
using Stripe.Entitlements;

namespace RaveIsland.ApiService.Infrastructure.Billing;

public sealed class BillingStatus
{
    public bool IsConfigured { get; init; }
    public bool BillingSetupCompleted { get; init; }
    public bool IsSubscribed { get; init; }
    public string? SubscriptionStatus { get; init; }
    public string? PriceId { get; init; }
    public string? PlanName { get; init; }
    public bool CanPublish { get; init; }
    public decimal? AvailablePublishCredits { get; init; }
    public bool HasUnlimitedPublishes { get; init; }
    public IReadOnlyList<string> ActiveFeatures { get; init; } = [];
}

public interface IStripeEntitlementService
{
    Task<BillingStatus> GetBillingStatusAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<BillingStatus> GetCachedBillingStatusAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ValidateCanPublishAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<bool> IsFreeTierTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    Task<bool> HasFeatureAsync(
        Guid tenantId,
        string featureLookupKey,
        CancellationToken cancellationToken = default);
}

public sealed class StripeEntitlementService(
    AppDbContext db,
    IOptions<StripeOptions> options,
    IStripeBillingSyncService billingSync) : IStripeEntitlementService
{
    private static readonly HashSet<string> ActiveSubscriptionStatuses =
    [
        "active",
        "trialing",
    ];

    public Task<BillingStatus> GetBillingStatusAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        BuildBillingStatusAsync(tenantId, refreshCache: true, cancellationToken);

    public Task<BillingStatus> GetCachedBillingStatusAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        BuildBillingStatusAsync(tenantId, refreshCache: false, cancellationToken);

    private async Task<BillingStatus> BuildBillingStatusAsync(
        Guid tenantId,
        bool refreshCache,
        CancellationToken cancellationToken)
    {
        if (refreshCache)
        {
            await billingSync.RefreshTenantBillingCacheAsync(tenantId, cancellationToken);
        }

        var tenant = await db.Tenants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant is null)
        {
            return new BillingStatus { IsConfigured = options.Value.IsConfigured };
        }

        if (!options.Value.IsConfigured)
        {
            return new BillingStatus
            {
                IsConfigured = false,
                BillingSetupCompleted = tenant.BillingSetupCompletedAt.HasValue,
                IsSubscribed = tenant.BillingSetupCompletedAt.HasValue,
                CanPublish = true,
            };
        }

        var hasUnlimited = await HasUnlimitedPublishesAsync(tenant, cancellationToken);
        var credits = await GetAvailableCreditsAsync(tenant.StripeCustomerId, cancellationToken);
        var features = await GetActiveFeaturesAsync(tenant.StripeCustomerId, cancellationToken);
        var canPublish = CanPublish(tenant, hasUnlimited, credits, options.Value.FreePriceId);
        var isSubscribed = tenant.BillingSetupCompletedAt.HasValue &&
                           ActiveSubscriptionStatuses.Contains(tenant.StripeSubscriptionStatus ?? string.Empty);
        var planName = await ResolvePlanNameAsync(tenant.StripePriceId, cancellationToken)
            ?? ResolvePlanNameFromOptions(tenant.StripePriceId);

        return new BillingStatus
        {
            IsConfigured = true,
            BillingSetupCompleted = tenant.BillingSetupCompletedAt.HasValue,
            IsSubscribed = isSubscribed,
            SubscriptionStatus = tenant.StripeSubscriptionStatus,
            PriceId = tenant.StripePriceId,
            PlanName = planName,
            CanPublish = canPublish,
            AvailablePublishCredits = credits,
            HasUnlimitedPublishes = hasUnlimited,
            ActiveFeatures = features,
        };
    }

    public async Task<IReadOnlyList<string>> ValidateCanPublishAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (!options.Value.IsConfigured)
        {
            return [];
        }

        var tenant = await db.Tenants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant is null)
        {
            return ["Tenant not found."];
        }

        if (string.IsNullOrWhiteSpace(tenant.StripeCustomerId))
        {
            return ["Billing setup is required before publishing events."];
        }

        if (!tenant.BillingSetupCompletedAt.HasValue)
        {
            return ["Complete billing setup before publishing events."];
        }

        if (!string.IsNullOrWhiteSpace(tenant.StripeSubscriptionStatus) &&
            !ActiveSubscriptionStatuses.Contains(tenant.StripeSubscriptionStatus))
        {
            return ["Your subscription is not active. Update billing to publish events."];
        }

        if (IsFreeTierPriceId(tenant.StripePriceId, options.Value.FreePriceId))
        {
            return [];
        }

        var hasUnlimited = await HasUnlimitedPublishesAsync(tenant, cancellationToken);
        if (hasUnlimited)
        {
            return [];
        }

        var credits = await GetAvailableCreditsAsync(tenant.StripeCustomerId, cancellationToken);
        if (credits is null or <= 0)
        {
            return ["Publish limit reached. Upgrade your plan in billing settings."];
        }

        return [];
    }

    public async Task<bool> IsFreeTierTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var tenant = await db.Tenants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        return tenant is not null && IsFreeTierPriceId(tenant.StripePriceId, options.Value.FreePriceId);
    }

    public async Task<bool> HasFeatureAsync(
        Guid tenantId,
        string featureLookupKey,
        CancellationToken cancellationToken = default)
    {
        if (!options.Value.IsConfigured)
        {
            return true;
        }

        var tenant = await db.Tenants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant?.StripeCustomerId is null)
        {
            return false;
        }

        var features = await GetActiveFeaturesAsync(tenant.StripeCustomerId, cancellationToken);
        return features.Contains(featureLookupKey, StringComparer.OrdinalIgnoreCase);
    }

    private static bool CanPublish(Tenant tenant, bool hasUnlimited, decimal? credits, string freePriceId)
    {
        if (!tenant.BillingSetupCompletedAt.HasValue || string.IsNullOrWhiteSpace(tenant.StripeCustomerId))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(tenant.StripeSubscriptionStatus) &&
            !ActiveSubscriptionStatuses.Contains(tenant.StripeSubscriptionStatus))
        {
            return false;
        }

        if (IsFreeTierPriceId(tenant.StripePriceId, freePriceId))
        {
            return true;
        }

        return hasUnlimited || credits is > 0;
    }

    private static bool IsFreeTierPriceId(string? priceId, string freePriceId) =>
        !string.IsNullOrWhiteSpace(priceId) &&
        string.Equals(priceId, freePriceId, StringComparison.Ordinal);

    private async Task<bool> HasUnlimitedPublishesAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tenant.StripePriceId))
        {
            return false;
        }

        var priceService = new PriceService();
        var price = await priceService.GetAsync(tenant.StripePriceId, cancellationToken: cancellationToken);
        if (price.Product is null)
        {
            return false;
        }

        var productService = new ProductService();
        var product = await productService.GetAsync(price.ProductId, cancellationToken: cancellationToken);
        return product.Metadata.TryGetValue("unlimited_publishes", out var value) &&
               string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<decimal?> GetAvailableCreditsAsync(
        string? customerId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return null;
        }

        try
        {
            var summaryService = new CreditBalanceSummaryService();
            var summary = await summaryService.GetAsync(new CreditBalanceSummaryGetOptions
            {
                Customer = customerId,
                Filter = new CreditBalanceSummaryFilterOptions
                {
                    Type = "applicability_scope",
                    ApplicabilityScope = new CreditBalanceSummaryFilterApplicabilityScopeOptions
                    {
                        PriceType = "metered",
                    },
                },
            }, cancellationToken: cancellationToken);

            var monetary = summary.Balances?
                .FirstOrDefault(b => string.Equals(b.AvailableBalance?.Type, "monetary", StringComparison.OrdinalIgnoreCase));

            if (monetary?.AvailableBalance?.Monetary?.Value is null)
            {
                return 0;
            }

            return monetary.AvailableBalance.Monetary.Value / 100m;
        }
        catch (StripeException)
        {
            return 0;
        }
    }

    private async Task<IReadOnlyList<string>> GetActiveFeaturesAsync(
        string? customerId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return [];
        }

        try
        {
            var entitlementService = new ActiveEntitlementService();
            var entitlements = await entitlementService.ListAsync(new ActiveEntitlementListOptions
            {
                Customer = customerId,
                Limit = 100,
            }, cancellationToken: cancellationToken);

            return entitlements.Data
                .Select(e => e.LookupKey)
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Cast<string>()
                .ToList();
        }
        catch (StripeException)
        {
            return [];
        }
    }

    private async Task<string?> ResolvePlanNameAsync(string? priceId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(priceId))
        {
            return null;
        }

        try
        {
            var priceService = new PriceService();
            var price = await priceService.GetAsync(
                priceId,
                new PriceGetOptions { Expand = ["product"] },
                cancellationToken: cancellationToken);

            if (!string.IsNullOrWhiteSpace(price.Product?.Name))
            {
                return price.Product.Name;
            }

            if (!string.IsNullOrWhiteSpace(price.ProductId))
            {
                var productService = new ProductService();
                var product = await productService.GetAsync(price.ProductId, cancellationToken: cancellationToken);
                return product.Name;
            }

            return null;
        }
        catch (StripeException)
        {
            return null;
        }
    }

    private string? ResolvePlanNameFromOptions(string? priceId)
    {
        if (string.IsNullOrWhiteSpace(priceId))
        {
            return null;
        }

        if (string.Equals(priceId, options.Value.FreePriceId, StringComparison.Ordinal))
        {
            return "Free";
        }

        if (string.Equals(priceId, options.Value.StarterPriceId, StringComparison.Ordinal))
        {
            return "Starter";
        }

        if (string.Equals(priceId, options.Value.ProPriceId, StringComparison.Ordinal))
        {
            return "Pro";
        }

        return null;
    }
}
