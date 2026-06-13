using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Features.Events;
using RaveIsland.ApiService.Infrastructure.Lookups;
using RaveIsland.ApiService.Infrastructure.Persistence;

namespace RaveIsland.ApiService.Features.Events.Public;

public sealed class GetPublicEventEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/public/events/{slugOrId}", Handle);

    private static async Task<IResult> Handle(
        string slugOrId,
        AppDbContext db,
        string? inviteCode,
        CancellationToken cancellationToken)
    {
        var publishedStatusId = await EventDefaults.GetPublishedStatusIdAsync(db, cancellationToken);
        if (!publishedStatusId.HasValue)
        {
            return Results.NotFound();
        }

        var query = db.Events
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(e => e.EventStatusId == publishedStatusId.Value);

        var eventEntity = Guid.TryParse(slugOrId, out var eventId)
            ? await query.WithDetails().FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken)
            : await query.WithDetails().FirstOrDefaultAsync(e => e.Slug == slugOrId, cancellationToken);

        if (eventEntity is null)
        {
            return Results.NotFound(new { error = "Event not found." });
        }

        var visibilityCode = await db.LookupValues
            .AsNoTracking()
            .Where(v => v.Id == eventEntity.VisibilityTypeId)
            .Select(v => v.Code)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.Equals(visibilityCode, "Private", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(visibilityCode, "InviteOnly", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(inviteCode) ||
                !string.Equals(inviteCode, eventEntity.InviteCode, StringComparison.OrdinalIgnoreCase))
            {
                return Results.NotFound(new { error = "Event not found." });
            }
        }

        if (string.Equals(visibilityCode, "Hidden", StringComparison.OrdinalIgnoreCase))
        {
            return Results.NotFound(new { error = "Event not found." });
        }

        return Results.Ok(EventMapper.MapDetail(eventEntity));
    }
}

public sealed class ValidateInviteCodeEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/public/events/{eventId:guid}/validate-invite", Handle);

    private static async Task<IResult> Handle(
        Guid eventId,
        ValidateInviteRequest request,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var eventEntity = await db.Events
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);

        if (eventEntity is null)
        {
            return Results.NotFound(new { error = "Event not found." });
        }

        var isValid = !string.IsNullOrWhiteSpace(eventEntity.InviteCode) &&
                      string.Equals(request.InviteCode, eventEntity.InviteCode, StringComparison.OrdinalIgnoreCase);

        return Results.Ok(new { isValid });
    }

    public sealed record ValidateInviteRequest(string InviteCode);
}
