using YummyZoom.SharedKernel;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Seeding;

public interface ISeeder
{
    string Name { get; }
    int Order { get; }

    Task<Result> SeedAsync(SeedingContext context, CancellationToken cancellationToken = default);
    Task<bool> CanSeedAsync(SeedingContext context, CancellationToken cancellationToken = default);
}

