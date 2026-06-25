using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RaveIsland.ApiService.Infrastructure.Email;
using RaveIsland.ApiService.Infrastructure.Lookups;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Persistence.Entities;

namespace RaveIsland.ApiService.Infrastructure.Billing;

public sealed class PublishPaymentRequirement
{
    public bool Required { get; init; }
    public int AmountCents { get; init; }
    public string Currency { get; init; } = "usd";
    public bool AlreadyPaid { get; init; }
}

public interface IEventPublishPaymentService
{
    Task<PublishPaymentRequirement> GetRequirementAsync(
        Guid tenantId,
        Guid eventId,
        CancellationToken cancellationToken = default);

    Task<string?> CreateCheckoutSessionAsync(
        EventEntity eventEntity,
        Tenant tenant,
        CancellationToken cancellationToken = default);

    Task<bool> ConfirmAndPublishAsync(
        Guid eventId,
        string checkoutSessionId,
        CancellationToken cancellationToken = default);

    Task HandleCheckoutCompletedAsync(
        Stripe.Checkout.Session session,
        CancellationToken cancellationToken = default);
}

public sealed class EventPublishPaymentService(
    AppDbContext db,
    IStripeEntitlementService entitlementService,
    IStripeCheckoutService checkoutService,
    IEventPublishService publishService,
    IOptions<StripeOptions> stripeOptions,
    IOptions<AppOptions> appOptions,
    ILogger<EventPublishPaymentService> logger) : IEventPublishPaymentService
{
    public async Task<PublishPaymentRequirement> GetRequirementAsync(
        Guid tenantId,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        if (!stripeOptions.Value.IsConfigured)
        {
            return new PublishPaymentRequirement { Required = false };
        }

        if (!await entitlementService.IsFreeTierTenantAsync(tenantId, cancellationToken))
        {
            return new PublishPaymentRequirement { Required = false };
        }

        var alreadyPaid = await HasPaidPaymentAsync(eventId, cancellationToken);
        return new PublishPaymentRequirement
        {
            Required = !alreadyPaid,
            AlreadyPaid = alreadyPaid,
            AmountCents = stripeOptions.Value.FreeTierPublishFeeAmountCents,
            Currency = stripeOptions.Value.FreeTierPublishFeeCurrency,
        };
    }

    public async Task<string?> CreateCheckoutSessionAsync(
        EventEntity eventEntity,
        Tenant tenant,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenant.StripeCustomerId))
        {
            throw new InvalidOperationException("Stripe customer is not configured for this tenant.");
        }

        if (await HasPaidPaymentAsync(eventEntity.Id, cancellationToken))
        {
            return null;
        }

        var options = stripeOptions.Value;
        var webBaseUrl = appOptions.Value.WebBaseUrl.TrimEnd('/');
        var successUrl =
            $"{webBaseUrl}/events/{eventEntity.Id}/edit?tab=publish&publishPayment=success&session_id={{CHECKOUT_SESSION_ID}}";
        var cancelUrl = $"{webBaseUrl}/events/{eventEntity.Id}/edit?tab=publish&publishPayment=cancelled";

        var checkout = await checkoutService.CreateEventPublishCheckoutAsync(
            tenant.StripeCustomerId,
            eventEntity.Id,
            eventEntity.Title,
            eventEntity.Slug,
            eventEntity.TenantId,
            tenant.Name,
            tenant.Slug,
            options.FreeTierPublishFeeAmountCents,
            options.FreeTierPublishFeeCurrency,
            string.IsNullOrWhiteSpace(options.FreeTierPublishFeePriceId)
                ? null
                : options.FreeTierPublishFeePriceId,
            successUrl,
            cancelUrl,
            cancellationToken);

        db.EventPublishPayments.Add(new EventPublishPayment
        {
            Id = Guid.NewGuid(),
            EventId = eventEntity.Id,
            TenantId = eventEntity.TenantId,
            StripeCheckoutSessionId = checkout.SessionId,
            Status = EventPublishPaymentStatuses.Pending,
            AmountCents = options.FreeTierPublishFeeAmountCents,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await db.SaveChangesAsync(cancellationToken);
        return checkout.Url;
    }

    public async Task<bool> ConfirmAndPublishAsync(
        Guid eventId,
        string checkoutSessionId,
        CancellationToken cancellationToken = default)
    {
        var session = await new Stripe.Checkout.SessionService().GetAsync(
            checkoutSessionId,
            cancellationToken: cancellationToken);

        if (!string.Equals(session.Metadata?.GetValueOrDefault(BillingCheckoutContracts.CheckoutPurposeMetadataKey),
                BillingCheckoutContracts.EventPublishPurpose, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.Equals(session.Metadata?.GetValueOrDefault(BillingCheckoutContracts.EventIdMetadataKey),
                eventId.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        await CompletePaymentAndPublishAsync(session, cancellationToken);
        return true;
    }

    public Task HandleCheckoutCompletedAsync(
        Stripe.Checkout.Session session,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(session.Metadata?.GetValueOrDefault(BillingCheckoutContracts.CheckoutPurposeMetadataKey),
                BillingCheckoutContracts.EventPublishPurpose, StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        return CompletePaymentAndPublishAsync(session, cancellationToken);
    }

    private async Task CompletePaymentAndPublishAsync(
        Stripe.Checkout.Session session,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(session.Metadata?.GetValueOrDefault(BillingCheckoutContracts.EventIdMetadataKey), out var eventId))
        {
            logger.LogWarning("Event publish checkout session {SessionId} is missing event_id metadata.", session.Id);
            return;
        }

        var payment = await db.EventPublishPayments
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.EventId == eventId && p.StripeCheckoutSessionId == session.Id, cancellationToken);

        payment ??= await db.EventPublishPayments
            .IgnoreQueryFilters()
            .Where(p => p.EventId == eventId && p.Status == EventPublishPaymentStatuses.Pending)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (payment is not null)
        {
            payment.StripeCheckoutSessionId = session.Id;
            payment.Status = EventPublishPaymentStatuses.Paid;
            payment.PaidAt = DateTimeOffset.UtcNow;
        }
        else
        {
            db.EventPublishPayments.Add(new EventPublishPayment
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                TenantId = Guid.TryParse(session.Metadata?.GetValueOrDefault(BillingCheckoutContracts.TenantIdMetadataKey), out var tenantId)
                    ? tenantId
                    : Guid.Empty,
                StripeCheckoutSessionId = session.Id,
                Status = EventPublishPaymentStatuses.Paid,
                AmountCents = stripeOptions.Value.FreeTierPublishFeeAmountCents,
                CreatedAt = DateTimeOffset.UtcNow,
                PaidAt = DateTimeOffset.UtcNow,
            });
        }

        var eventEntity = await db.Events
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        if (eventEntity is null)
        {
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var publishedStatusId = await EventDefaults.GetPublishedStatusIdAsync(db, cancellationToken);
        if (publishedStatusId.HasValue && eventEntity.EventStatusId == publishedStatusId.Value)
        {
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        await publishService.PublishAsync(eventEntity, reportMeter: false, cancellationToken);
    }

    private async Task<bool> HasPaidPaymentAsync(Guid eventId, CancellationToken cancellationToken) =>
        await db.EventPublishPayments
            .IgnoreQueryFilters()
            .AnyAsync(
                p => p.EventId == eventId && p.Status == EventPublishPaymentStatuses.Paid,
                cancellationToken);
}
