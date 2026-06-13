namespace RaveIsland.ApiService.Infrastructure.Persistence.Entities;

public class UserInvitation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string IntendedRole { get; set; }
    public required string TokenHash { get; set; }
    public InvitationStatus Status { get; set; }
    public required string InvitedByUserId { get; set; }
    public string? KeycloakUserId { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? RegisteredAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
