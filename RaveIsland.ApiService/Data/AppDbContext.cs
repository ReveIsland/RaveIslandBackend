using Microsoft.EntityFrameworkCore;

namespace RaveIsland.ApiService.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Item> Items => Set<Item>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(256).IsRequired();
            entity.Property(e => e.CreatedBy).HasMaxLength(256);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}
