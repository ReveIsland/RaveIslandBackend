namespace RaveIsland.ApiService.Infrastructure.Persistence.Entities;

public class Ticket
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid EventTicketTypeId { get; set; }
    public required string QrToken { get; set; }
    public string? HolderName { get; set; }
    public string? HolderEmail { get; set; }
    public bool IsCheckedIn { get; set; }
    public DateTimeOffset? CheckedInAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public EventEntity Event { get; set; } = null!;
    public EventTicketType EventTicketType { get; set; } = null!;
    public ICollection<CheckInLog> CheckInLogs { get; set; } = [];
}
