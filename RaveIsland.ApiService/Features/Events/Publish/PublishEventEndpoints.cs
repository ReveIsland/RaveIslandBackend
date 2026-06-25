using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Billing;
using RaveIsland.ApiService.Infrastructure.Lookups;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Tenancy;

namespace RaveIsland.ApiService.Features.Events.Publish;

public sealed class GetPublishReadinessEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/events/{eventId:guid}/publish-readiness", Handle)
            .RequireAuthorization(AuthorizationPolicies.TenantMember);

    private static async Task<IResult> Handle(
        Guid eventId,
        AppDbContext db,
        ITenantContext tenantContext,
        IEventPublishValidator validator,
        IEventPublishPaymentService publishPaymentService,
        CancellationToken cancellationToken)
    {
        var eventEntity = await EventQueryHelper.FindEventAsync(db, tenantContext, eventId, cancellationToken, track: false);
        if (eventEntity is null) return Results.NotFound(new { error = "Event not found." });

        var errors = await validator.ValidateAsync(eventEntity, cancellationToken);
        var payment = tenantContext.IsAdmin
            ? new PublishPaymentRequirement { Required = false }
            : await publishPaymentService.GetRequirementAsync(eventEntity.TenantId, eventId, cancellationToken);

        return Results.Ok(new
        {
            isReady = errors.Count == 0,
            errors,
            publishPayment = new
            {
                required = payment.Required,
                alreadyPaid = payment.AlreadyPaid,
                amountCents = payment.AmountCents,
                currency = payment.Currency,
            },
        });
    }
}

public sealed class PublishEventEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/events/{eventId:guid}/publish", Handle).RequireAuthorization(AuthorizationPolicies.TenantMember);

    private static async Task<IResult> Handle(
        Guid eventId,
        AppDbContext db,
        ITenantContext tenantContext,
        IEventPublishValidator validator,
        IEventPublishService publishService,
        IEventPublishPaymentService publishPaymentService,
        CancellationToken cancellationToken)
    {
        var eventEntity = await EventQueryHelper.FindEventAsync(db, tenantContext, eventId, cancellationToken);
        if (eventEntity is null) return Results.NotFound(new { error = "Event not found." });
        var access = EventQueryHelper.CheckAccess(tenantContext, eventEntity);
        if (access is not null) return access;

        var publishedStatusId = await EventDefaults.GetPublishedStatusIdAsync(db, cancellationToken);
        if (!publishedStatusId.HasValue)
        {
            return Results.Problem("Published status lookup value is not seeded.");
        }

        if (eventEntity.EventStatusId == publishedStatusId.Value)
        {
            return Results.Ok(new
            {
                eventEntity.Id,
                eventEntity.Slug,
                publicUrl = $"/events/{eventEntity.Slug ?? eventEntity.Id.ToString()}",
                published = true,
            });
        }

        var errors = await validator.ValidateAsync(eventEntity, cancellationToken);
        if (errors.Count > 0)
        {
            return Results.BadRequest(new { error = "Event is not ready to publish.", errors });
        }

        var paymentRequirement = tenantContext.IsAdmin
            ? new PublishPaymentRequirement { Required = false }
            : await publishPaymentService.GetRequirementAsync(
                eventEntity.TenantId,
                eventId,
                cancellationToken);

        if (paymentRequirement.Required)
        {
            var tenant = await db.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == eventEntity.TenantId, cancellationToken);

            if (tenant is null)
            {
                return Results.Problem("Tenant not found.");
            }

            var checkoutUrl = await publishPaymentService.CreateCheckoutSessionAsync(
                eventEntity,
                tenant,
                cancellationToken);

            return Results.Ok(new
            {
                requiresPublishPayment = true,
                checkoutUrl,
                amountCents = paymentRequirement.AmountCents,
                currency = paymentRequirement.Currency,
            });
        }

        var result = await publishService.PublishAsync(
            eventEntity,
            reportMeter: !tenantContext.IsAdmin,
            cancellationToken);

        return Results.Ok(new
        {
            result.Id,
            result.Slug,
            publicUrl = result.PublicUrl,
            published = true,
        });
    }
}

public sealed class ConfirmPublishPaymentEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/events/{eventId:guid}/confirm-publish-payment", Handle)
            .RequireAuthorization(AuthorizationPolicies.TenantMember);

    private static async Task<IResult> Handle(
        Guid eventId,
        string session_id,
        AppDbContext db,
        ITenantContext tenantContext,
        IEventPublishPaymentService publishPaymentService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(session_id))
        {
            return Results.BadRequest(new { error = "session_id is required." });
        }

        var eventEntity = await EventQueryHelper.FindEventAsync(db, tenantContext, eventId, cancellationToken, track: false);
        if (eventEntity is null) return Results.NotFound(new { error = "Event not found." });

        var confirmed = await publishPaymentService.ConfirmAndPublishAsync(eventId, session_id, cancellationToken);
        if (!confirmed)
        {
            return Results.BadRequest(new { error = "Payment could not be confirmed." });
        }

        var published = await db.Events
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        return Results.Ok(new
        {
            id = eventId,
            slug = published?.Slug,
            publicUrl = $"/events/{published?.Slug ?? eventId.ToString()}",
            published = true,
        });
    }
}
