using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace RaveIsland.ApiService.Infrastructure.Billing;

public interface IStripeCheckoutService
{
    Task<string> CreateSubscriptionCheckoutUrlAsync(
        string customerId,
        string priceId,
        string successUrl,
        string cancelUrl,
        string? promotionCode = null,
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
}
