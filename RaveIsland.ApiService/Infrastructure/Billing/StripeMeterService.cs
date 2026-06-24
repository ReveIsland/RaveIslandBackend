using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Billing;

namespace RaveIsland.ApiService.Infrastructure.Billing;

public interface IStripeMeterService
{
    Task ReportEventPublishedAsync(
        string customerId,
        Guid eventId,
        CancellationToken cancellationToken = default);

    Task GrantFreeTierCreditsAsync(
        string customerId,
        CancellationToken cancellationToken = default);
}

public sealed class StripeMeterService(IOptions<StripeOptions> options) : IStripeMeterService
{
    public async Task ReportEventPublishedAsync(
        string customerId,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        if (!options.Value.IsConfigured)
        {
            return;
        }

        var meterOptions = options.Value;
        var meterEventService = new MeterEventService();
        await meterEventService.CreateAsync(new MeterEventCreateOptions
        {
            EventName = meterOptions.EventsPublishedMeterEventName,
            Payload = new Dictionary<string, string>
            {
                ["stripe_customer_id"] = customerId,
                ["value"] = "1",
            },
            Identifier = $"publish-{eventId:N}",
            Timestamp = DateTime.UtcNow,
        }, cancellationToken: cancellationToken);
    }

    public async Task GrantFreeTierCreditsAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        if (!options.Value.IsConfigured || options.Value.FreeTierPublishCredits <= 0)
        {
            return;
        }

        var creditGrantService = new CreditGrantService();
        await creditGrantService.CreateAsync(new CreditGrantCreateOptions
        {
            Customer = customerId,
            Amount = new CreditGrantAmountOptions
            {
                Type = "monetary",
                Monetary = new CreditGrantAmountMonetaryOptions
                {
                    Value = options.Value.FreeTierPublishCredits * 100,
                    Currency = "usd",
                },
            },
            ApplicabilityConfig = new CreditGrantApplicabilityConfigOptions
            {
                Scope = new CreditGrantApplicabilityConfigScopeOptions
                {
                    PriceType = "metered",
                },
            },
            Category = "promotional",
            Name = "Free tier publish credits",
        }, cancellationToken: cancellationToken);
    }
}
