namespace RaveIsland.ApiService.Infrastructure.Lookups;

public interface ILookupSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
