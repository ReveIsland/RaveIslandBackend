using RaveIsland.ApiService.Infrastructure.Persistence.Entities;

namespace RaveIsland.ApiService.Features.Events.Promos;

public interface IPromoValidationService
{
    PromoValidationResult Validate(EventPromoCode promo, decimal ticketPrice, Guid? ticketTypeId);
}

public sealed record PromoValidationResult(bool IsValid, string? Error, decimal DiscountAmount);

public sealed class PromoValidationService : IPromoValidationService
{
    public PromoValidationResult Validate(EventPromoCode promo, decimal ticketPrice, Guid? ticketTypeId)
    {
        if (!promo.IsActive)
        {
            return new PromoValidationResult(false, "Promo code is inactive.", 0);
        }

        if (promo.ExpiresAt.HasValue && promo.ExpiresAt.Value < DateTimeOffset.UtcNow)
        {
            return new PromoValidationResult(false, "Promo code has expired.", 0);
        }

        if (promo.UsageLimit.HasValue && promo.UsageCount >= promo.UsageLimit.Value)
        {
            return new PromoValidationResult(false, "Promo code usage limit reached.", 0);
        }

        var discount = promo.DiscountType switch
        {
            PromoDiscountType.Percent => Math.Round(ticketPrice * promo.DiscountValue / 100m, 2),
            PromoDiscountType.Fixed => promo.DiscountValue,
            _ => 0m,
        };

        discount = Math.Min(discount, ticketPrice);
        return new PromoValidationResult(true, null, discount);
    }
}
