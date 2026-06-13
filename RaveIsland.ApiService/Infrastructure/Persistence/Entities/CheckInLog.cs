namespace RaveIsland.ApiService.Infrastructure.Persistence.Entities;

public class CheckInLog
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public Guid EventId { get; set; }
    public required string ScannedByUserId { get; set; }
    public string? ScannedByName { get; set; }
    public string? GateId { get; set; }
    public DateTimeOffset ScannedAt { get; set; }

    public Ticket Ticket { get; set; } = null!;
    public EventEntity Event { get; set; } = null!;
}
