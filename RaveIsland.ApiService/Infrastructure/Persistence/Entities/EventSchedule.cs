namespace RaveIsland.ApiService.Infrastructure.Persistence.Entities;

public class EventSchedule
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public int DayNumber { get; set; }
    public DateOnly EventDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public TimeOnly? GatesOpenTime { get; set; }
    public TimeOnly? LastEntryTime { get; set; }

    public EventEntity Event { get; set; } = null!;
}
