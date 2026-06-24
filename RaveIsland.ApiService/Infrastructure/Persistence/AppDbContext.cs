using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Identity;
using RaveIsland.ApiService.Infrastructure.Lookups;
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
    public DbSet<LookupType> LookupTypes => Set<LookupType>();
    public DbSet<LookupValue> LookupValues => Set<LookupValue>();
    public DbSet<EventSchedule> EventSchedules => Set<EventSchedule>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<EventMedia> EventMedia => Set<EventMedia>();
    public DbSet<Artist> Artists => Set<Artist>();
    public DbSet<EventArtist> EventArtists => Set<EventArtist>();
    public DbSet<EventTicketType> EventTicketTypes => Set<EventTicketType>();
    public DbSet<EventPromoCode> EventPromoCodes => Set<EventPromoCode>();
    public DbSet<EventLookupSelection> EventLookupSelections => Set<EventLookupSelection>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<CheckInLog> CheckInLogs => Set<CheckInLog>();
    public DbSet<StripeWebhookEvent> StripeWebhookEvents => Set<StripeWebhookEvent>();

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
            entity.Property(e => e.StripeCustomerId).HasMaxLength(128);
            entity.Property(e => e.StripeSubscriptionId).HasMaxLength(128);
            entity.Property(e => e.StripePriceId).HasMaxLength(128);
            entity.Property(e => e.StripeSubscriptionStatus).HasMaxLength(64);
            entity.Property(e => e.PendingPromotionCode).HasMaxLength(64);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.StripeCustomerId);
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
            entity.Property(e => e.Tagline).HasMaxLength(512);
            entity.Property(e => e.Description).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.Theme).HasMaxLength(256);
            entity.Property(e => e.OrganizerReference).HasMaxLength(256);
            entity.Property(e => e.SoundSystem).HasMaxLength(256);
            entity.Property(e => e.EntryPolicy).HasMaxLength(4000);
            entity.Property(e => e.ProhibitedItems).HasMaxLength(4000);
            entity.Property(e => e.TermsAndConditions).HasMaxLength(8000);
            entity.Property(e => e.InviteCode).HasMaxLength(64);
            entity.Property(e => e.Slug).HasMaxLength(256);
            entity.Property(e => e.MetaTitle).HasMaxLength(256);
            entity.Property(e => e.MetaDescription).HasMaxLength(512);
            entity.Property(e => e.CreatedByUserId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.CreatedByName).HasMaxLength(256);
            entity.HasIndex(e => e.TenantId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Slug);
            entity.HasOne(e => e.Tenant).WithMany(t => t.Events).HasForeignKey(e => e.TenantId);
            entity.HasOne(e => e.EventCategory).WithMany().HasForeignKey(e => e.EventCategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.EventStatus).WithMany().HasForeignKey(e => e.EventStatusId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.VisibilityType).WithMany().HasForeignKey(e => e.VisibilityTypeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.VenueType).WithMany().HasForeignKey(e => e.VenueTypeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.PrimaryGenre).WithMany().HasForeignKey(e => e.PrimaryGenreId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.SecondaryGenre).WithMany().HasForeignKey(e => e.SecondaryGenreId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AgeRestriction).WithMany().HasForeignKey(e => e.AgeRestrictionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.CancellationPolicy).WithMany().HasForeignKey(e => e.CancellationPolicyId).OnDelete(DeleteBehavior.Restrict);
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

        modelBuilder.Entity<LookupType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.HasIndex(e => e.Code).IsUnique();

            var seedTime = new DateTimeOffset(2026, 6, 13, 0, 0, 0, TimeSpan.Zero);
            foreach (var typeDef in LookupSeedData.Types)
            {
                entity.HasData(new LookupType
                {
                    Id = typeDef.Id,
                    Code = typeDef.Code,
                    Name = typeDef.Name,
                    Description = typeDef.Description,
                    IsSystem = true,
                    CreatedAt = seedTime,
                });
            }
        });

        modelBuilder.Entity<LookupValue>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
            entity.Property(e => e.IconUrl).HasMaxLength(2048);
            entity.Property(e => e.MetadataJson).HasMaxLength(4000);
            entity.HasIndex(e => new { e.LookupTypeId, e.Code }).IsUnique();
            entity.HasOne(e => e.LookupType).WithMany(t => t.Values).HasForeignKey(e => e.LookupTypeId);
        });

        modelBuilder.Entity<EventSchedule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.EventId, e.DayNumber }).IsUnique();
            entity.HasOne(e => e.Event).WithMany(ev => ev.Schedules).HasForeignKey(e => e.EventId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Venue>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.VenueName).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Address).HasMaxLength(512).IsRequired();
            entity.Property(e => e.City).HasMaxLength(128).IsRequired();
            entity.Property(e => e.Province).HasMaxLength(128);
            entity.Property(e => e.GoogleMapsUrl).HasMaxLength(2048);
            entity.Property(e => e.LandmarkInstructions).HasMaxLength(2000);
            entity.HasIndex(e => e.EventId).IsUnique();
            entity.HasOne(e => e.Event).WithOne(ev => ev.Venue).HasForeignKey<Venue>(e => e.EventId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.District).WithMany().HasForeignKey(e => e.DistrictId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EventMedia>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StorageUrl).HasMaxLength(2048).IsRequired();
            entity.Property(e => e.ThumbnailUrl).HasMaxLength(2048);
            entity.Property(e => e.FileName).HasMaxLength(512).IsRequired();
            entity.HasIndex(e => e.EventId);
            entity.HasOne(e => e.Event).WithMany(ev => ev.Media).HasForeignKey(e => e.EventId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Artist>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
            entity.Property(e => e.StageName).HasMaxLength(256);
            entity.Property(e => e.Bio).HasMaxLength(4000);
            entity.Property(e => e.ProfileImageUrl).HasMaxLength(2048);
            entity.Property(e => e.SocialLinksJson).HasMaxLength(4000);
            entity.HasIndex(e => e.TenantId);
            entity.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId);
            entity.HasOne(e => e.ArtistType).WithMany().HasForeignKey(e => e.ArtistTypeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.PrimaryGenre).WithMany().HasForeignKey(e => e.PrimaryGenreId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(e => BypassTenantFilters || e.TenantId == CurrentTenantId);
        });

        modelBuilder.Entity<EventArtist>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StageNameOverride).HasMaxLength(256);
            entity.HasIndex(e => new { e.EventId, e.ArtistId }).IsUnique();
            entity.HasOne(e => e.Event).WithMany(ev => ev.Artists).HasForeignKey(e => e.EventId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Artist).WithMany(a => a.EventArtists).HasForeignKey(e => e.ArtistId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.PrimaryGenre).WithMany().HasForeignKey(e => e.PrimaryGenreId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EventTicketType>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.HasIndex(e => e.EventId);
            entity.HasOne(e => e.Event).WithMany(ev => ev.TicketTypes).HasForeignKey(e => e.EventId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.DefaultLookupValue).WithMany().HasForeignKey(e => e.DefaultLookupValueId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EventPromoCode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(64).IsRequired();
            entity.Property(e => e.DiscountValue).HasPrecision(18, 2);
            entity.Property(e => e.AppliesToTicketTypeIdsJson).HasMaxLength(4000);
            entity.HasIndex(e => new { e.EventId, e.Code }).IsUnique();
            entity.HasOne(e => e.Event).WithMany(ev => ev.PromoCodes).HasForeignKey(e => e.EventId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EventLookupSelection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LookupTypeCode).HasMaxLength(64).IsRequired();
            entity.HasIndex(e => new { e.EventId, e.LookupValueId }).IsUnique();
            entity.HasOne(e => e.Event).WithMany(ev => ev.LookupSelections).HasForeignKey(e => e.EventId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.LookupValue).WithMany().HasForeignKey(e => e.LookupValueId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.QrToken).HasMaxLength(512).IsRequired();
            entity.Property(e => e.HolderName).HasMaxLength(256);
            entity.Property(e => e.HolderEmail).HasMaxLength(256);
            entity.HasIndex(e => e.QrToken).IsUnique();
            entity.HasIndex(e => e.EventId);
            entity.HasOne(e => e.Event).WithMany().HasForeignKey(e => e.EventId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.EventTicketType).WithMany(t => t.Tickets).HasForeignKey(e => e.EventTicketTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CheckInLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ScannedByUserId).HasMaxLength(128).IsRequired();
            entity.Property(e => e.ScannedByName).HasMaxLength(256);
            entity.Property(e => e.GateId).HasMaxLength(64);
            entity.HasIndex(e => e.EventId);
            entity.HasIndex(e => e.TicketId);
            entity.HasOne(e => e.Ticket).WithMany(t => t.CheckInLogs).HasForeignKey(e => e.TicketId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Event).WithMany().HasForeignKey(e => e.EventId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StripeWebhookEvent>(entity =>
        {
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.EventId).HasMaxLength(128);
        });
    }
}
