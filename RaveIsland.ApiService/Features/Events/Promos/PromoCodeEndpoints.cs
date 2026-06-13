using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Features.Events.Promos;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Persistence.Entities;
using RaveIsland.ApiService.Infrastructure.Tenancy;

namespace RaveIsland.ApiService.Features.Events.Promos;

public sealed class ManagePromoCodesEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/api/events/{eventId:guid}/promo-codes", Handle).RequireAuthorization(AuthorizationPolicies.TenantMember);

    private static async Task<IResult> Handle(
        Guid eventId,
        UpsertPromoCodesRequest request,
        AppDbContext db,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var eventEntity = await EventQueryHelper.FindEventAsync(db, tenantContext, eventId, cancellationToken);
        if (eventEntity is null) return Results.NotFound(new { error = "Event not found." });
        var access = EventQueryHelper.CheckAccess(tenantContext, eventEntity);
        if (access is not null) return access;

        var existingIds = request.PromoCodes.Where(p => p.Id.HasValue).Select(p => p.Id!.Value).ToList();
        var toRemove = await db.EventPromoCodes
            .Where(p => p.EventId == eventId && !existingIds.Contains(p.Id))
            .ToListAsync(cancellationToken);
        db.EventPromoCodes.RemoveRange(toRemove);

        foreach (var item in request.PromoCodes)
        {
            if (string.IsNullOrWhiteSpace(item.Code))
            {
                return Results.BadRequest(new { error = "Promo code is required." });
            }

            EventPromoCode promo;
            if (item.Id.HasValue)
            {
                promo = await db.EventPromoCodes.FirstAsync(p => p.Id == item.Id.Value && p.EventId == eventId, cancellationToken);
            }
            else
            {
                promo = new EventPromoCode { Id = Guid.NewGuid(), EventId = eventId, Code = string.Empty };
                db.EventPromoCodes.Add(promo);
            }

            promo.Code = item.Code.Trim().ToUpperInvariant();
            promo.DiscountType = item.DiscountType;
            promo.DiscountValue = item.DiscountValue;
            promo.ExpiresAt = item.ExpiresAt;
            promo.UsageLimit = item.UsageLimit;
            promo.IsActive = item.IsActive;
            promo.AppliesToTicketTypeIdsJson = item.AppliesToTicketTypeIdsJson;
        }

        eventEntity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { count = request.PromoCodes.Count });
    }

    public sealed record PromoCodeItem(
        Guid? Id,
        string Code,
        PromoDiscountType DiscountType,
        decimal DiscountValue,
        DateTimeOffset? ExpiresAt,
        int? UsageLimit,
        bool IsActive,
        string? AppliesToTicketTypeIdsJson);

    public sealed record UpsertPromoCodesRequest(IReadOnlyList<PromoCodeItem> PromoCodes);
}

public sealed class ValidatePromoCodeEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/events/{eventId:guid}/promo-codes/validate", Handle).RequireAuthorization(AuthorizationPolicies.TenantMember);

    private static async Task<IResult> Handle(
        Guid eventId,
        ValidatePromoRequest request,
        AppDbContext db,
        IPromoValidationService promoValidation,
        CancellationToken cancellationToken)
    {
        var promo = await db.EventPromoCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.EventId == eventId && p.Code == request.Code.Trim().ToUpperInvariant(), cancellationToken);

        if (promo is null)
        {
            return Results.NotFound(new { error = "Promo code not found." });
        }

        var result = promoValidation.Validate(promo, request.TicketPrice, request.TicketTypeId);
        return Results.Ok(new { result.IsValid, result.Error, result.DiscountAmount });
    }

    public sealed record ValidatePromoRequest(string Code, decimal TicketPrice, Guid? TicketTypeId);
}
