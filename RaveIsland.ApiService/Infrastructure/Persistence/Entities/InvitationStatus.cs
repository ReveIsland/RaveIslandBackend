namespace RaveIsland.ApiService.Infrastructure.Persistence.Entities;

public enum InvitationStatus
{
    Pending = 0,
    InviteSent = 1,
    Registered = 2,
    Expired = 3,
    Revoked = 4,
}
