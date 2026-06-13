using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Features.Events.CheckIn;
using RaveIsland.ApiService.Infrastructure.Identity;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Persistence.Entities;
using RaveIsland.ApiService.Infrastructure.Tenancy;

namespace RaveIsland.ApiService.Features.Events.CheckIn;

public sealed class CheckInTicketEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/events/{eventId:guid}/check-in", Handle).RequireAuthorization(AuthorizationPolicies.TenantMember);

    private static async Task<IResult> Handle(
        Guid eventId,
        CheckInRequest request,
        AppDbContext db,
        ITenantContext tenantContext,
        IQrTokenService qrTokenService,
        System.Security.Claims.ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var eventEntity = await EventQueryHelper.FindEventAsync(db, tenantContext, eventId, cancellationToken, track: false);
        if (eventEntity is null) return Results.NotFound(new { error = "Event not found." });

        var parsed = qrTokenService.ParseToken(request.QrToken);
        if (parsed is null || parsed.Value.EventId != eventId)
        {
            return Results.BadRequest(new { error = "Invalid QR code." });
        }

        var ticket = await db.Tickets
            .FirstOrDefaultAsync(t => t.Id == parsed.Value.TicketId && t.EventId == eventId, cancellationToken);

        if (ticket is null)
        {
            return Results.NotFound(new { error = "Ticket not found." });
        }

        if (ticket.IsCheckedIn)
        {
            return Results.Conflict(new { error = "Ticket already checked in.", ticket.CheckedInAt });
        }

        var userId = KeycloakClaims.GetUserId(user) ?? "unknown";
        var now = DateTimeOffset.UtcNow;

        ticket.IsCheckedIn = true;
        ticket.CheckedInAt = now;

        db.CheckInLogs.Add(new CheckInLog
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            EventId = eventId,
            ScannedByUserId = userId,
            ScannedByName = KeycloakClaims.GetDisplayName(user),
            GateId = request.GateId,
            ScannedAt = now,
        });

        await db.SaveChangesAsync(cancellationToken);

        return Results.Ok(new
        {
            ticket.Id,
            ticket.HolderName,
            ticket.HolderEmail,
            checkedInAt = now,
        });
    }

    public sealed record CheckInRequest(string QrToken, string? GateId);
}

public sealed class IssueTicketEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/events/{eventId:guid}/tickets/issue", Handle).RequireAuthorization(AuthorizationPolicies.TenantMember);

    private static async Task<IResult> Handle(
        Guid eventId,
        IssueTicketRequest request,
        AppDbContext db,
        ITenantContext tenantContext,
        IQrTokenService qrTokenService,
        CancellationToken cancellationToken)
    {
        var eventEntity = await EventQueryHelper.FindEventAsync(db, tenantContext, eventId, cancellationToken);
        if (eventEntity is null) return Results.NotFound(new { error = "Event not found." });
        var access = EventQueryHelper.CheckAccess(tenantContext, eventEntity);
        if (access is not null) return access;

        var ticketType = await db.EventTicketTypes
            .FirstOrDefaultAsync(t => t.Id == request.EventTicketTypeId && t.EventId == eventId, cancellationToken);

        if (ticketType is null)
        {
            return Results.NotFound(new { error = "Ticket type not found." });
        }

        if (ticketType.QuantitySold >= ticketType.Quantity)
        {
            return Results.Conflict(new { error = "Ticket type sold out." });
        }

        var ticket = new Ticket
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            EventTicketTypeId = ticketType.Id,
            HolderName = request.HolderName,
            HolderEmail = request.HolderEmail,
            CreatedAt = DateTimeOffset.UtcNow,
            QrToken = string.Empty,
        };

        ticket.QrToken = qrTokenService.GenerateToken(ticket.Id, eventId);
        ticketType.QuantitySold++;

        db.Tickets.Add(ticket);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/events/{eventId}/tickets/{ticket.Id}", new
        {
            ticket.Id,
            ticket.QrToken,
            ticket.HolderName,
            ticket.HolderEmail,
        });
    }

    public sealed record IssueTicketRequest(
        Guid EventTicketTypeId,
        string? HolderName,
        string? HolderEmail);
}
