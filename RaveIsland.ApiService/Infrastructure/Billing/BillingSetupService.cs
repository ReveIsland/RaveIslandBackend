using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Email;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Persistence.Entities;

namespace RaveIsland.ApiService.Infrastructure.Billing;

public sealed class BillingSetupResult
{
    public bool RequiresBillingSetup { get; init; }
    public string? CheckoutUrl { get; init; }
    public string? Message { get; init; }
}

public interface IBillingSetupService
{
    Task<BillingSetupResult> SetupBillingAfterRegistrationAsync(
        UserInvitation invitation,
        CancellationToken cancellationToken = default);
}

public sealed class BillingSetupService(
    AppDbContext db,
    IStripeCustomerService customerService,
    IStripeCheckoutService checkoutService,
    IOptions<StripeOptions> stripeOptions,
    IOptions<AppOptions> appOptions) : IBillingSetupService
{
    public async Task<BillingSetupResult> SetupBillingAfterRegistrationAsync(
        UserInvitation invitation,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(invitation.IntendedRole, AppRoles.TenantAdmin, StringComparison.OrdinalIgnoreCase))
        {
            return new BillingSetupResult
            {
                RequiresBillingSetup = false,
                Message = "Registration complete. You can now sign in.",
            };
        }

        if (!stripeOptions.Value.IsConfigured)
        {
            return new BillingSetupResult
            {
                RequiresBillingSetup = false,
                Message = "Registration complete. You can now sign in.",
            };
        }

        var tenant = await db.Tenants
            .IgnoreQueryFilters()
            .FirstAsync(t => t.Id == invitation.TenantId, cancellationToken);

        if (tenant.BillingSetupCompletedAt.HasValue)
        {
            return new BillingSetupResult
            {
                RequiresBillingSetup = false,
                Message = "Registration complete. You can now sign in.",
            };
        }

        var contactName = $"{invitation.FirstName} {invitation.LastName}".Trim();
        tenant = await customerService.EnsureCustomerAsync(tenant, invitation.Email, contactName, cancellationToken);

        if (string.IsNullOrWhiteSpace(tenant.StripeCustomerId))
        {
            throw new InvalidOperationException("Failed to create Stripe customer.");
        }

        var freePriceId = stripeOptions.Value.FreePriceId;
        if (string.IsNullOrWhiteSpace(freePriceId))
        {
            throw new InvalidOperationException("Stripe FreePriceId is not configured.");
        }

        var webBaseUrl = appOptions.Value.WebBaseUrl.TrimEnd('/');
        var checkoutUrl = await checkoutService.CreateSubscriptionCheckoutUrlAsync(
            tenant.StripeCustomerId,
            freePriceId,
            $"{webBaseUrl}/billing/welcome?session_id={{CHECKOUT_SESSION_ID}}",
            $"{webBaseUrl}/billing/setup",
            tenant.PendingPromotionCode,
            cancellationToken);

        return new BillingSetupResult
        {
            RequiresBillingSetup = true,
            CheckoutUrl = checkoutUrl,
            Message = "Registration complete. Continue to billing setup.",
        };
    }
}
