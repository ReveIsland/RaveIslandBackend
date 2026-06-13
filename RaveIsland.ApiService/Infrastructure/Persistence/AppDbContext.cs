using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Identity;
using RaveIsland.ApiService.Infrastructure.Persistence.Entities;
using RaveIsland.ApiService.Infrastructure.Tenancy;

namespace RaveIsland.ApiService.Infrastructure.Persistence;

public class AppDbContext(
    DbContextOptions<AppDbContext> options,
    IHttpContextAccessor httpContextAccessor) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();
    public DbSet<EventEntity> Events => Set<EventEntity>();
    public DbSet<UserInvitation> UserInvitations => Set<UserInvitation>();

    private bool BypassTenantFilters
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return true;
            }

            return KeycloakClaims.GetRoles(user).Contains(AppRoles.Admin, StringComparer.OrdinalIgnoreCase);
        }
    }

    private Guid? CurrentTenantId =>
        TenantIdResolutionHelper.Resolve(httpContextAccessor);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Slug).HasMaxLength(128).IsRequired();
            entity.HasIndex(e => e.Slug).IsUnique();
        });

        modelBuilder.Entity<TenantMembership>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.KeycloakUserId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(64).IsRequired();
            entity.HasIndex(e => new { e.TenantId, e.KeycloakUserId }).IsUnique();
            entity.HasOne(e => e.Tenant).WithMany(t => t.Memberships).HasForeignKey(e => e.TenantId);
            entity.HasQueryFilter(e => BypassTenantFilters || e.TenantId == CurrentTenantId);
        });

        modelBuilder.Entity<EventEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(4000);
            entity.Property(e => e.CreatedByUserId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.CreatedByName).HasMaxLength(256);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasOne(e => e.Tenant).WithMany(t => t.Events).HasForeignKey(e => e.TenantId);
            entity.HasQueryFilter(e => BypassTenantFilters || e.TenantId == CurrentTenantId);
        });

        modelBuilder.Entity<UserInvitation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(128).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(128).IsRequired();
            entity.Property(e => e.IntendedRole).HasMaxLength(64).IsRequired();
            entity.Property(e => e.TokenHash).HasMaxLength(128).IsRequired();
            entity.Property(e => e.InvitedByUserId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.KeycloakUserId).HasMaxLength(128);
            entity.HasIndex(e => e.TokenHash);
            entity.HasIndex(e => new { e.TenantId, e.Email });
            entity.HasOne(e => e.Tenant).WithMany(t => t.Invitations).HasForeignKey(e => e.TenantId);
            entity.HasQueryFilter(e => BypassTenantFilters || e.TenantId == CurrentTenantId);
        });
    }
}
