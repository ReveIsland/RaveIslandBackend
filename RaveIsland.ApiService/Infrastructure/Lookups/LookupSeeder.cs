using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Persistence.Entities;

namespace RaveIsland.ApiService.Infrastructure.Lookups;

public sealed class LookupSeeder(AppDbContext db) : ILookupSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var typeDef in LookupSeedData.Types)
        {
            var existingType = await db.LookupTypes
                .FirstOrDefaultAsync(t => t.Code == typeDef.Code, cancellationToken);

            if (existingType is null)
            {
                existingType = new LookupType
                {
                    Id = typeDef.Id,
                    Code = typeDef.Code,
                    Name = typeDef.Name,
                    Description = typeDef.Description,
                    IsSystem = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                };
                db.LookupTypes.Add(existingType);
                await db.SaveChangesAsync(cancellationToken);
            }

            foreach (var valueDef in typeDef.Values)
            {
                var existingValue = await db.LookupValues
                    .FirstOrDefaultAsync(
                        v => v.LookupTypeId == existingType.Id && v.Code == valueDef.Code,
                        cancellationToken);

                if (existingValue is null)
                {
                    db.LookupValues.Add(new LookupValue
                    {
                        Id = CreateDeterministicGuid(existingType.Id, valueDef.Code),
                        LookupTypeId = existingType.Id,
                        Code = valueDef.Code,
                        Name = valueDef.Name,
                        DisplayOrder = valueDef.DisplayOrder,
                        IsActive = true,
                        IsSystem = true,
                    });
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public static Guid CreateDeterministicGuid(Guid typeId, string valueCode)
    {
        var input = $"{typeId:N}:{valueCode}";
        var hash = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }
}
