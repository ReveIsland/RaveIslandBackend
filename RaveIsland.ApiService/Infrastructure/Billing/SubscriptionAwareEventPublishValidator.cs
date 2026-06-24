using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Features.Events.Publish;
using RaveIsland.ApiService.Infrastructure.Identity;
using RaveIsland.ApiService.Infrastructure.Persistence.Entities;

namespace RaveIsland.ApiService.Infrastructure.Billing;

public sealed class SubscriptionAwareEventPublishValidator(
    EventPublishValidator inner,
    IStripeEntitlementService entitlementService,
    IHttpContextAccessor httpContextAccessor) : IEventPublishValidator
{
    public async Task<IReadOnlyList<string>> ValidateAsync(
        EventEntity eventEntity,
        CancellationToken cancellationToken = default)
    {
        var errors = (await inner.ValidateAsync(eventEntity, cancellationToken)).ToList();

        if (IsPlatformAdmin())
        {
            return errors;
        }

        var billingErrors = await entitlementService.ValidateCanPublishAsync(
            eventEntity.TenantId,
            cancellationToken);
        errors.AddRange(billingErrors);
        return errors;
    }

    private bool IsPlatformAdmin()
    {
        var user = httpContextAccessor.HttpContext?.User;
        return user?.Identity?.IsAuthenticated == true &&
               KeycloakClaims.GetRoles(user).Contains(AppRoles.Admin, StringComparer.OrdinalIgnoreCase);
    }
}
