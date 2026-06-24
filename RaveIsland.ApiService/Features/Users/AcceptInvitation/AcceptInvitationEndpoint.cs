using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Billing;
using RaveIsland.ApiService.Infrastructure.Identity;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Persistence.Entities;
using RaveIsland.ApiService.Infrastructure.Security;

namespace RaveIsland.ApiService.Features.Users.AcceptInvitation;

public sealed class AcceptInvitationEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/invitations/{token}", GetInvitation);
        app.MapPost("/api/invitations/{token}/accept", AcceptInvitation);
    }

    private static async Task<IResult> GetInvitation(
        string token,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var invitation = await FindValidInvitationAsync(db, token, cancellationToken);
        if (invitation is null)
        {
            return Results.NotFound(new { error = "Invitation not found or no longer valid." });
        }

        return Results.Ok(new
        {
            invitation.Email,
            invitation.FirstName,
            invitation.LastName,
            invitation.IntendedRole,
            tenantName = invitation.Tenant.Name,
            invitation.ExpiresAt,
            status = invitation.Status.ToString(),
        });
    }

    private static async Task<IResult> AcceptInvitation(
        string token,
        AcceptInvitationRequest request,
        AppDbContext db,
        IKeycloakAdminService keycloakAdmin,
        IBillingSetupService billingSetupService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password != request.ConfirmPassword)
        {
            return Results.BadRequest(new { error = "Password and confirmation must match." });
        }

        if (request.Password.Length < 8)
        {
            return Results.BadRequest(new { error = "Password must be at least 8 characters." });
        }

        var invitation = await FindValidInvitationAsync(db, token, cancellationToken, track: true);
        if (invitation is null)
        {
            return Results.NotFound(new { error = "Invitation not found or no longer valid." });
        }

        if (invitation.Status is InvitationStatus.Registered or InvitationStatus.Revoked)
        {
            return Results.Conflict(new { error = "This invitation has already been used or revoked." });
        }

        if (invitation.ExpiresAt < DateTimeOffset.UtcNow)
        {
            invitation.Status = InvitationStatus.Expired;
            await db.SaveChangesAsync(cancellationToken);
            return Results.BadRequest(new { error = "This invitation has expired." });
        }

        var keycloakUserId = await keycloakAdmin.CreateUserAsync(
            invitation.Email,
            invitation.FirstName,
            invitation.LastName,
            request.Password,
            invitation.TenantId,
            invitation.IntendedRole,
            cancellationToken);

        db.TenantMemberships.Add(new TenantMembership
        {
            Id = Guid.NewGuid(),
            TenantId = invitation.TenantId,
            KeycloakUserId = keycloakUserId,
            Role = invitation.IntendedRole,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        invitation.Status = InvitationStatus.Registered;
        invitation.KeycloakUserId = keycloakUserId;
        invitation.RegisteredAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        var billingResult = await billingSetupService.SetupBillingAfterRegistrationAsync(
            invitation,
            cancellationToken);

        return Results.Ok(new
        {
            message = billingResult.Message ?? "Registration complete. You can now sign in.",
            requiresBillingSetup = billingResult.RequiresBillingSetup,
            checkoutUrl = billingResult.CheckoutUrl,
        });
    }

    private static async Task<UserInvitation?> FindValidInvitationAsync(
        AppDbContext db,
        string token,
        CancellationToken cancellationToken,
        bool track = false)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var tokenHash = TokenHasher.Hash(token);
        var query = db.UserInvitations
            .IgnoreQueryFilters()
            .Include(i => i.Tenant)
            .Where(i => i.TokenHash == tokenHash);

        if (!track)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public sealed record AcceptInvitationRequest(string Password, string ConfirmPassword);
}
