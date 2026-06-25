using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace RaveIsland.ApiService.Infrastructure.Billing;

public sealed record CheckoutSessionResult(string Url, string SessionId);

public interface IStripeCheckoutService
{
    Task<string> CreateSubscriptionCheckoutUrlAsync(
        string customerId,
        string priceId,
        string successUrl,
        string cancelUrl,
        string? promotionCode = null,
        CancellationToken cancellationToken = default);

    Task<CheckoutSessionResult> CreateEventPublishCheckoutAsync(
        string customerId,
        Guid eventId,
        string eventTitle,
        string? eventSlug,
        Guid tenantId,
        string tenantName,
        string tenantSlug,
        int amountCents,
        string currency,
        string? priceId,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default);
}

public sealed class StripeCheckoutService(IOptions<StripeOptions> options) : IStripeCheckoutService
{
    public async Task<string> CreateSubscriptionCheckoutUrlAsync(
        string customerId,
        string priceId,
        string successUrl,
        string cancelUrl,
        string? promotionCode = null,
        CancellationToken cancellationToken = default)
    {
        if (!options.Value.IsConfigured)
        {
            throw new InvalidOperationException("Stripe is not configured.");
        }

        var sessionOptions = new SessionCreateOptions
        {
            Mode = "subscription",
            Customer = customerId,
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            AllowPromotionCodes = string.IsNullOrWhiteSpace(promotionCode),
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Price = priceId,
                    Quantity = 1,
                },
            ],
            PaymentMethodCollection = "if_required",
        };

        if (!string.IsNullOrWhiteSpace(promotionCode))
        {
            var promoService = new PromotionCodeService();
            var promoCodes = await promoService.ListAsync(new PromotionCodeListOptions
            {
                Code = promotionCode,
                Active = true,
                Limit = 1,
            }, cancellationToken: cancellationToken);

            var promo = promoCodes.Data.FirstOrDefault();
            if (promo is not null)
            {
                sessionOptions.Discounts =
                [
                    new SessionDiscountOptions { PromotionCode = promo.Id },
                ];
                sessionOptions.AllowPromotionCodes = false;
            }
        }

        var sessionService = new SessionService();
        var session = await sessionService.CreateAsync(sessionOptions, cancellationToken: cancellationToken);
        return session.Url ?? throw new InvalidOperationException("Stripe did not return a checkout URL.");
    }

    public async Task<CheckoutSessionResult> CreateEventPublishCheckoutAsync(
        string customerId,
        Guid eventId,
        string eventTitle,
        string? eventSlug,
        Guid tenantId,
        string tenantName,
        string tenantSlug,
        int amountCents,
        string currency,
        string? priceId,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default)
    {
        if (!options.Value.IsConfigured)
        {
            throw new InvalidOperationException("Stripe is not configured.");
        }

        var metadata = BillingCheckoutContracts.BuildEventPublishMetadata(
            eventId,
            eventTitle,
            eventSlug,
            tenantId,
            tenantName,
            tenantSlug);

        var truncatedTitle = BillingCheckoutContracts.TruncateMetadataValue(eventTitle);
        var paymentDescription = string.IsNullOrWhiteSpace(truncatedTitle)
            ? "Payment for the event"
            : $"Payment for the event - {truncatedTitle}";

        var lineItem = new SessionLineItemOptions { Quantity = 1 };
        if (!string.IsNullOrWhiteSpace(priceId))
        {
            lineItem.Price = priceId;
        }
        else
        {
            var productName = string.IsNullOrWhiteSpace(truncatedTitle)
                ? "Event publish fee"
                : $"Publish: {truncatedTitle[..Math.Min(truncatedTitle.Length, 120)]}";

            lineItem.PriceData = new SessionLineItemPriceDataOptions
            {
                Currency = currency,
                UnitAmount = amountCents,
                ProductData = new SessionLineItemPriceDataProductDataOptions
                {
                    Name = productName,
                    Description = paymentDescription,
                },
            };
        }

        var sessionOptions = new SessionCreateOptions
        {
            Mode = "payment",
            Customer = customerId,
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            LineItems = [lineItem],
            Metadata = metadata,
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                Description = paymentDescription,
                Metadata = metadata,
            },
        };

        var sessionService = new SessionService();
        var session = await sessionService.CreateAsync(sessionOptions, cancellationToken: cancellationToken);
        return new CheckoutSessionResult(
            session.Url ?? throw new InvalidOperationException("Stripe did not return a checkout URL."),
            session.Id);
    }
}
