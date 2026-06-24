using System.Text;
using Microsoft.Extensions.Options;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Billing;
using Stripe;

namespace RaveIsland.ApiService.Features.Webhooks;

public sealed class StripeWebhookEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/webhooks/stripe", Handle).AllowAnonymous().DisableAntiforgery();

    private static async Task<IResult> Handle(
        HttpRequest request,
        IStripeWebhookHandler handler,
        IOptions<StripeOptions> options,
        ILogger<StripeWebhookEndpoint> logger,
        CancellationToken cancellationToken)
    {
        request.Body.Position = 0;
        using var memoryStream = new MemoryStream();
        await request.Body.CopyToAsync(memoryStream, cancellationToken);
        var json = Encoding.UTF8.GetString(memoryStream.ToArray());

        var signature = request.Headers["Stripe-Signature"].ToString();
        if (string.IsNullOrWhiteSpace(signature))
        {
            return Results.BadRequest(new { error = "Missing Stripe-Signature header." });
        }

        var webhookSecret = options.Value.WebhookSecret;
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            return Results.Problem(
                "Stripe webhook secret is not configured.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                json,
                signature,
                webhookSecret,
                throwOnApiVersionMismatch: false);
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Stripe webhook signature verification failed.");
            return Results.BadRequest(new
            {
                error = ex.Message,
                hint = "Use the whsec_ value printed by 'stripe listen' in Stripe:WebhookSecret, then restart the API.",
            });
        }

        try
        {
            await handler.HandleAsync(stripeEvent, cancellationToken);
            return Results.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Stripe webhook handler failed for event {EventId}", stripeEvent.Id);
            return Results.Ok();
        }
    }
}
