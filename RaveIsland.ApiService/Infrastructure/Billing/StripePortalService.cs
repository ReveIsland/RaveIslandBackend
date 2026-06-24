using Microsoft.Extensions.Options;
using Stripe;
using Stripe.BillingPortal;

namespace RaveIsland.ApiService.Infrastructure.Billing;

public interface IStripePortalService
{
    Task<string> CreatePortalUrlAsync(
        string customerId,
        string returnUrl,
        CancellationToken cancellationToken = default);
}

public sealed class StripePortalService(IOptions<StripeOptions> options) : IStripePortalService
{
    public async Task<string> CreatePortalUrlAsync(
        string customerId,
        string returnUrl,
        CancellationToken cancellationToken = default)
    {
        if (!options.Value.IsConfigured)
        {
            throw new InvalidOperationException("Stripe is not configured.");
        }

        var sessionService = new SessionService();
        var session = await sessionService.CreateAsync(new SessionCreateOptions
        {
            Customer = customerId,
            ReturnUrl = returnUrl,
        }, cancellationToken: cancellationToken);

        return session.Url ?? throw new InvalidOperationException("Stripe did not return a portal URL.");
    }
}
