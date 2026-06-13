using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Persistence.Entities;
using RaveIsland.ApiService.Infrastructure.Tenancy;

namespace RaveIsland.ApiService.Features.Events.Schedules;

public sealed class ListSchedulesEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/events/{eventId:guid}/schedules", Handle).RequireAuthorization(AuthorizationPolicies.TenantMember);

    private static async Task<IResult> Handle(
        Guid eventId,
        AppDbContext db,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var eventEntity = await EventQueryHelper.FindEventAsync(db, tenantContext, eventId, cancellationToken, track: false);
        if (eventEntity is null) return Results.NotFound(new { error = "Event not found." });

        var schedules = await db.EventSchedules
            .AsNoTracking()
            .Where(s => s.EventId == eventId)
            .OrderBy(s => s.DayNumber)
            .ToListAsync(cancellationToken);

        return Results.Ok(schedules);
    }
}

public sealed class UpsertScheduleEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/api/events/{eventId:guid}/schedules", Handle).RequireAuthorization(AuthorizationPolicies.TenantMember);

    private static async Task<IResult> Handle(
        Guid eventId,
        UpsertSchedulesRequest request,
        AppDbContext db,
        ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var eventEntity = await EventQueryHelper.FindEventAsync(db, tenantContext, eventId, cancellationToken);
        if (eventEntity is null) return Results.NotFound(new { error = "Event not found." });
        var access = EventQueryHelper.CheckAccess(tenantContext, eventEntity);
        if (access is not null) return access;

        foreach (var item in request.Schedules)
        {
            if (item.EndTime <= item.StartTime)
            {
                return Results.BadRequest(new { error = "End time must be later than start time." });
            }
        }

        var existing = await db.EventSchedules.Where(s => s.EventId == eventId).ToListAsync(cancellationToken);
        db.EventSchedules.RemoveRange(existing);

        foreach (var item in request.Schedules.OrderBy(s => s.DayNumber))
        {
            db.EventSchedules.Add(new EventSchedule
            {
                Id = Guid.NewGuid(),
                EventId = eventId,
                DayNumber = item.DayNumber,
                EventDate = item.EventDate,
                StartTime = item.StartTime,
                EndTime = item.EndTime,
                GatesOpenTime = item.GatesOpenTime,
                LastEntryTime = item.LastEntryTime,
            });
        }

        eventEntity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { count = request.Schedules.Count });
    }

    public sealed record ScheduleItem(
        int DayNumber,
        DateOnly EventDate,
        TimeOnly StartTime,
        TimeOnly EndTime,
        TimeOnly? GatesOpenTime,
        TimeOnly? LastEntryTime);

    public sealed record UpsertSchedulesRequest(IReadOnlyList<ScheduleItem> Schedules);
}
