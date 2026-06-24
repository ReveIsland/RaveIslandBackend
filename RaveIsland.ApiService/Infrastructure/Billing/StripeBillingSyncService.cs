using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Persistence.Entities;
using Stripe;
using Stripe.Checkout;

namespace RaveIsland.ApiService.Infrastructure.Billing;

public interface IStripeBillingSyncService
{
    Task<bool> SyncFromCheckoutSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    Task RefreshTenantBillingCacheAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public sealed class StripeBillingSyncService(
    AppDbContext db,
    IOptions<StripeOptions> options,
    ILogger<StripeBillingSyncService> logger) : IStripeBillingSyncService
{
    public async Task<bool> SyncFromCheckoutSessionAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (!options.Value.IsConfigured || string.IsNullOrWhiteSpace(sessionId))
        {
            return false;
        }

        var sessionService = new SessionService();
        var session = await sessionService.GetAsync(
            sessionId,
            new SessionGetOptions { Expand = ["subscription"] },
            cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(session.CustomerId))
        {
            return false;
        }

        var tenant = await db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.StripeCustomerId == session.CustomerId, cancellationToken);

        if (tenant is null)
        {
            logger.LogWarning("No tenant found for Stripe customer {CustomerId}", session.CustomerId);
            return false;
        }

        tenant.BillingSetupCompletedAt ??= DateTimeOffset.UtcNow;
        tenant.StripeSubscriptionId = session.SubscriptionId ?? tenant.StripeSubscriptionId;

        if (session.Subscription is Subscription subscription)
        {
            ApplySubscriptionSnapshot(tenant, subscription);
        }
        else if (!string.IsNullOrWhiteSpace(session.SubscriptionId))
        {
            await SyncSubscriptionAsync(tenant, session.SubscriptionId, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task RefreshTenantBillingCacheAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (!options.Value.IsConfigured)
        {
            return;
        }

        var tenant = await db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant is null || string.IsNullOrWhiteSpace(tenant.StripeSubscriptionId))
        {
            return;
        }

        await SyncSubscriptionAsync(tenant, tenant.StripeSubscriptionId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    internal async Task SyncSubscriptionAsync(
        Tenant tenant,
        string subscriptionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var subscriptionService = new SubscriptionService();
            var subscription = await subscriptionService.GetAsync(
                subscriptionId,
                new SubscriptionGetOptions { Expand = ["items.data.price.product"] },
                cancellationToken: cancellationToken);

            ApplySubscriptionSnapshot(tenant, subscription);
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Failed to sync subscription {SubscriptionId} for tenant {TenantId}", subscriptionId, tenant.Id);
        }
    }

    internal static void ApplySubscriptionSnapshot(Tenant tenant, Subscription subscription)
    {
        tenant.StripeSubscriptionId = subscription.Id;
        tenant.StripeSubscriptionStatus = subscription.Status;
        tenant.StripePriceId = subscription.Items?.Data?.FirstOrDefault()?.Price?.Id;
        tenant.BillingSetupCompletedAt ??= DateTimeOffset.UtcNow;
    }
}
