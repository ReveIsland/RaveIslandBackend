using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace RaveIsland.ApiService.Infrastructure.Persistence;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=raveisland;Username=postgres;Password=postgres");

        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        var provider = services.BuildServiceProvider();

        return new AppDbContext(
            optionsBuilder.Options,
            provider.GetRequiredService<IHttpContextAccessor>(),
            provider.GetRequiredService<IServiceScopeFactory>());
    }
}
