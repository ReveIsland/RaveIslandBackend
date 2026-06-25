using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Persistence.Entities;
using Stripe;

namespace RaveIsland.ApiService.Infrastructure.Billing;

public interface IStripeWebhookHandler
{
    Task HandleAsync(Event stripeEvent, CancellationToken cancellationToken = default);
}

public sealed class StripeWebhookHandler(
    AppDbContext db,
    StripeBillingSyncService billingSync,
    IEventPublishPaymentService eventPublishPaymentService,
    ILogger<StripeWebhookHandler> logger) : IStripeWebhookHandler
{
    public async Task HandleAsync(Event stripeEvent, CancellationToken cancellationToken = default)
    {
        if (await db.StripeWebhookEvents.AnyAsync(e => e.EventId == stripeEvent.Id, cancellationToken))
        {
            logger.LogInformation("Skipping already processed Stripe event {EventId}", stripeEvent.Id);
            return;
        }

        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
                await HandleCheckoutCompletedAsync(stripeEvent, cancellationToken);
                break;
            case "customer.subscription.created":
            case "customer.subscription.updated":
            case "customer.subscription.deleted":
                await HandleSubscriptionChangedAsync(stripeEvent, cancellationToken);
                break;
            case "invoice.payment_failed":
                await HandlePaymentFailedAsync(stripeEvent, cancellationToken);
                break;
        }

        db.StripeWebhookEvents.Add(new StripeWebhookEvent
        {
            EventId = stripeEvent.Id,
            ProcessedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleCheckoutCompletedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is not Stripe.Checkout.Session session)
        {
            return;
        }

        if (string.Equals(
                session.Metadata?.GetValueOrDefault(BillingCheckoutContracts.CheckoutPurposeMetadataKey),
                BillingCheckoutContracts.EventPublishPurpose,
                StringComparison.OrdinalIgnoreCase))
        {
            await eventPublishPaymentService.HandleCheckoutCompletedAsync(session, cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(session.CustomerId))
        {
            return;
        }

        var tenant = await FindTenantByCustomerIdAsync(session.CustomerId, cancellationToken);
        if (tenant is null)
        {
            return;
        }

        tenant.BillingSetupCompletedAt ??= DateTimeOffset.UtcNow;
        tenant.StripeSubscriptionId = session.SubscriptionId ?? tenant.StripeSubscriptionId;

        if (!string.IsNullOrWhiteSpace(session.SubscriptionId))
        {
            await billingSync.SyncSubscriptionAsync(tenant, session.SubscriptionId, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleSubscriptionChangedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is not Subscription subscription)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(subscription.CustomerId))
        {
            return;
        }

        var tenant = await FindTenantByCustomerIdAsync(subscription.CustomerId, cancellationToken);
        if (tenant is null)
        {
            return;
        }

        StripeBillingSyncService.ApplySubscriptionSnapshot(tenant, subscription);
        await billingSync.SyncSubscriptionAsync(tenant, subscription.Id, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task HandlePaymentFailedAsync(Event stripeEvent, CancellationToken cancellationToken)
    {
        if (stripeEvent.Data.Object is not Invoice invoice || string.IsNullOrWhiteSpace(invoice.CustomerId))
        {
            return;
        }

        var tenant = await FindTenantByCustomerIdAsync(invoice.CustomerId, cancellationToken);
        if (tenant is null)
        {
            return;
        }

        tenant.StripeSubscriptionStatus = "past_due";
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Tenant?> FindTenantByCustomerIdAsync(string customerId, CancellationToken cancellationToken)
    {
        return await db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.StripeCustomerId == customerId, cancellationToken);
    }
}
