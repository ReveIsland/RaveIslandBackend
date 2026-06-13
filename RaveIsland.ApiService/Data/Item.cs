namespace RaveIsland.ApiService.Data;

public class Item
{
    public Guid Id { get; set; }

    public required string Title { get; set; }

    public string? CreatedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
