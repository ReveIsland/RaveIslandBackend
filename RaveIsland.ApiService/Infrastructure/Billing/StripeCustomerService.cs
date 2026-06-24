using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Persistence.Entities;
using Stripe;

namespace RaveIsland.ApiService.Infrastructure.Billing;

public interface IStripeCustomerService
{
    Task<Tenant> EnsureCustomerAsync(
        Tenant tenant,
        string email,
        string name,
        CancellationToken cancellationToken = default);
}

public sealed class StripeCustomerService(
    AppDbContext db,
    IOptions<StripeOptions> options) : IStripeCustomerService
{
    public async Task<Tenant> EnsureCustomerAsync(
        Tenant tenant,
        string email,
        string name,
        CancellationToken cancellationToken = default)
    {
        if (!options.Value.IsConfigured)
        {
            throw new InvalidOperationException("Stripe is not configured.");
        }

        if (!string.IsNullOrWhiteSpace(tenant.StripeCustomerId))
        {
            return tenant;
        }

        var customerService = new CustomerService();
        var customer = await customerService.CreateAsync(new CustomerCreateOptions
        {
            Email = email,
            Name = name,
            Metadata = new Dictionary<string, string>
            {
                ["tenant_id"] = tenant.Id.ToString(),
                ["tenant_slug"] = tenant.Slug,
                ["billing_contact_email"] = email,
            },
        }, cancellationToken: cancellationToken);

        tenant.StripeCustomerId = customer.Id;
        await db.SaveChangesAsync(cancellationToken);
        return tenant;
    }
}
