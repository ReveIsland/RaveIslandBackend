using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Email;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Persistence.Entities;
using RaveIsland.ApiService.Infrastructure.Security;
using RaveIsland.ApiService.Infrastructure.Tenancy;

namespace RaveIsland.ApiService.Features.Users.InviteUser;

public sealed class InviteUserEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/api/users/invite", Handle).RequireAuthorization(AuthorizationPolicies.TenantAdminOrAdmin);

    private static async Task<IResult> Handle(
        InviteUserRequest request,
        AppDbContext db,
        ITenantContext tenantContext,
        ITenantMembershipResolver tenantMembershipResolver,
        IEmailSender emailSender,
        IOptions<AppOptions> appOptions,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.FirstName) ||
            string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.Role))
        {
            return Results.BadRequest(new { error = "Email, first name, last name, and role are required." });
        }

        if (!AppRoles.TenantRoles.Contains(request.Role, StringComparer.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new { error = "Role must be tenant-admin or tenant-user." });
        }

        var tenantId = tenantContext.IsAdmin
            ? request.TenantId
            : tenantContext.TenantId ?? await tenantMembershipResolver.ResolveTenantIdAsync(cancellationToken);
        if (!tenantId.HasValue)
        {
            return Results.BadRequest(new { error = "Tenant context could not be resolved for this user." });
        }

        if (tenantContext.IsTenantAdmin && !tenantContext.IsAdmin &&
            string.Equals(request.Role, AppRoles.TenantAdmin, StringComparison.OrdinalIgnoreCase))
        {
            return Results.Forbid();
        }

        var email = request.Email.Trim().ToLowerInvariant();

        var tenant = await db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId.Value && t.IsActive, cancellationToken);

        if (tenant is null)
        {
            return Results.NotFound(new { error = "Tenant not found." });
        }

        var pendingInvite = await db.UserInvitations
            .IgnoreQueryFilters()
            .AnyAsync(i =>
                i.TenantId == tenantId.Value &&
                i.Email == email &&
                (i.Status == InvitationStatus.Pending || i.Status == InvitationStatus.InviteSent),
                cancellationToken);

        if (pendingInvite)
        {
            return Results.Conflict(new { error = "An active invitation already exists for this email." });
        }

        var rawToken = TokenHasher.GenerateToken();
        var invitation = new UserInvitation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId.Value,
            Email = email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            IntendedRole = request.Role,
            TokenHash = TokenHasher.Hash(rawToken),
            Status = InvitationStatus.Pending,
            InvitedByUserId = tenantContext.UserId ?? "system",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.UserInvitations.Add(invitation);
        await db.SaveChangesAsync(cancellationToken);

        var inviteUrl = $"{appOptions.Value.WebBaseUrl.TrimEnd('/')}/invite/accept?token={rawToken}";
        var htmlBody = $"""
            <p>Hello {invitation.FirstName},</p>
            <p>You have been invited to join <strong>{tenant.Name}</strong> on Rave Island as a <strong>{request.Role}</strong>.</p>
            <p><a href="{inviteUrl}">Accept your invitation</a></p>
            <p>This link expires on {invitation.ExpiresAt:u}.</p>
            """;

        try
        {
            await emailSender.SendAsync(
                email,
                $"You're invited to join {tenant.Name} on Rave Island",
                htmlBody,
                cancellationToken);

            invitation.Status = InvitationStatus.InviteSent;
            invitation.SentAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            return Results.Problem(
                detail: "Invitation was created but the email could not be sent. Retry sending from user management.",
                statusCode: StatusCodes.Status502BadGateway);
        }

        return Results.Ok(new
        {
            invitation.Id,
            invitation.Email,
            invitation.Status,
            invitation.ExpiresAt,
        });
    }

    public sealed record InviteUserRequest(
        string Email,
        string FirstName,
        string LastName,
        string Role,
        Guid? TenantId);
}
