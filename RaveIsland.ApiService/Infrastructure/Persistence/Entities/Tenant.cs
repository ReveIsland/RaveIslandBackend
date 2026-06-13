namespace RaveIsland.ApiService.Infrastructure.Persistence.Entities;

public class Tenant
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<TenantMembership> Memberships { get; set; } = [];
    public ICollection<EventEntity> Events { get; set; } = [];
    public ICollection<UserInvitation> Invitations { get; set; } = [];
}
