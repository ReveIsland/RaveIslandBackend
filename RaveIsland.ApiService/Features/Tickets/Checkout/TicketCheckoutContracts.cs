namespace RaveIsland.ApiService.Features.Tickets.Checkout;

/// <summary>
/// Phase 6 placeholder for mobile/web ticket purchase via Stripe Checkout (mode=payment).
/// See docs/STRIPE_SETUP.md consumer coupons and plan Phase 6.
/// </summary>
public static class TicketCheckoutContracts
{
    public const string CheckoutPurposeMetadataKey = "checkout_purpose";
    public const string TicketCheckoutPurpose = "ticket_purchase";
    public const string EventIdMetadataKey = "event_id";
    public const string TicketTypeIdMetadataKey = "ticket_type_id";
}

public sealed record CreateTicketCheckoutRequest(
    Guid TicketTypeId,
    int Quantity,
    string? HolderName,
    string? HolderEmail,
    string? PromotionCode);

public sealed record TicketCheckoutResponse(string CheckoutUrl);

// Future endpoint: POST /api/public/events/{slugOrId}/checkout
// Future webhook branch: checkout.session.completed where metadata checkout_purpose=ticket_purchase
