using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Infrastructure.Identity;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Persistence.Entities;

namespace RaveIsland.ApiService.Infrastructure.Tenancy;

public interface ITenantMembershipResolver
{
    Task<Guid?> ResolveTenantIdAsync(CancellationToken cancellationToken = default);
}

public sealed class TenantMembershipResolver(
    IHttpContextAccessor httpContextAccessor,
    AppDbContext db,
    IKeycloakAdminService keycloakAdmin,
    ILogger<TenantMembershipResolver> logger) : ITenantMembershipResolver
{
    public const string TenantIdItemKey = "RaveIsland.TenantId";
    public const string TenantIdResolvedKey = "RaveIsland.TenantIdResolved";

    public async Task<Guid?> ResolveTenantIdAsync(CancellationToken cancellationToken = default)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return null;
        }

        if (httpContext.Items.TryGetValue(TenantIdItemKey, out var cached) && cached is Guid cachedTenantId)
        {
            return cachedTenantId;
        }

        if (httpContext.Items.ContainsKey(TenantIdResolvedKey))
        {
            return null;
        }

        var user = httpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            httpContext.Items[TenantIdResolvedKey] = true;
            return null;
        }

        var tenantId = await ResolveForUserAsync(user, cancellationToken);

        if (tenantId.HasValue)
        {
            httpContext.Items[TenantIdItemKey] = tenantId.Value;
            AddTenantClaim(user, tenantId.Value);
        }

        httpContext.Items[TenantIdResolvedKey] = true;
        return tenantId;
    }

    internal async Task<Guid?> ResolveForUserAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var tenantClaim = user.FindFirst("tenant_id")?.Value;
        if (Guid.TryParse(tenantClaim, out var tenantId))
        {
            return tenantId;
        }

        var email = ResolveEmail(user);
        var userId = await ResolveKeycloakUserIdAsync(user, email, cancellationToken);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            var membershipTenantId = await db.TenantMemberships
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(m => m.KeycloakUserId == userId && m.IsActive)
                .Select(m => (Guid?)m.TenantId)
                .FirstOrDefaultAsync(cancellationToken);

            if (membershipTenantId.HasValue)
            {
                await SyncTenantAttributeAsync(userId, membershipTenantId.Value, cancellationToken);
                return membershipTenantId;
            }
        }

        UserInvitation? invitation = null;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            invitation = await db.UserInvitations
                .IgnoreQueryFilters()
                .Where(i => i.Status == InvitationStatus.Registered && i.KeycloakUserId == userId)
                .OrderByDescending(i => i.RegisteredAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (invitation is null && !string.IsNullOrWhiteSpace(email))
        {
            invitation = await db.UserInvitations
                .IgnoreQueryFilters()
                .Where(i => i.Status == InvitationStatus.Registered && i.Email == email)
                .OrderByDescending(i => i.RegisteredAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (invitation is null)
        {
            logger.LogWarning(
                "No registered invitation found for user {UserId} ({Email})",
                userId ?? "unknown",
                email ?? "unknown");
            return null;
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            userId = invitation.KeycloakUserId;
        }

        if (string.IsNullOrWhiteSpace(userId) && !string.IsNullOrWhiteSpace(invitation.Email))
        {
            userId = await keycloakAdmin.GetUserIdByEmailAsync(invitation.Email, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning(
                "Could not resolve Keycloak user id for invitation {InvitationId} ({Email})",
                invitation.Id,
                invitation.Email);
            return null;
        }

        await EnsureMembershipAsync(invitation.Id, invitation.TenantId, invitation.IntendedRole, userId, cancellationToken);
        await SyncTenantAttributeAsync(userId, invitation.TenantId, cancellationToken);
        return invitation.TenantId;
    }

    private static string? ResolveEmail(ClaimsPrincipal user)
    {
        return KeycloakClaims.GetEmail(user)?.Trim().ToLowerInvariant()
            ?? user.FindFirst("preferred_username")?.Value?.Trim().ToLowerInvariant();
    }

    private async Task<string?> ResolveKeycloakUserIdAsync(
        ClaimsPrincipal user,
        string? email,
        CancellationToken cancellationToken)
    {
        var userId = KeycloakClaims.GetUserId(user);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            return userId;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return await keycloakAdmin.GetUserIdByEmailAsync(email, cancellationToken);
    }

    private async Task SyncTenantAttributeAsync(
        string keycloakUserId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        try
        {
            await keycloakAdmin.SetTenantAttributeAsync(keycloakUserId, tenantId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to sync tenant_id to Keycloak for user {UserId}", keycloakUserId);
        }
    }

    private static void AddTenantClaim(ClaimsPrincipal user, Guid tenantId)
    {
        if (user.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        if (!identity.HasClaim("tenant_id", tenantId.ToString()))
        {
            identity.AddClaim(new Claim("tenant_id", tenantId.ToString()));
        }
    }

    private async Task EnsureMembershipAsync(
        Guid invitationId,
        Guid tenantId,
        string role,
        string userId,
        CancellationToken cancellationToken)
    {
        var hasMembership = await db.TenantMemberships
            .IgnoreQueryFilters()
            .AnyAsync(m => m.KeycloakUserId == userId && m.TenantId == tenantId, cancellationToken);

        if (!hasMembership)
        {
            db.TenantMemberships.Add(new TenantMembership
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                KeycloakUserId = userId,
                Role = role,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            });
        }

        var invitation = await db.UserInvitations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(i => i.Id == invitationId, cancellationToken);

        if (invitation is not null &&
            !string.Equals(invitation.KeycloakUserId, userId, StringComparison.Ordinal))
        {
            invitation.KeycloakUserId = userId;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
