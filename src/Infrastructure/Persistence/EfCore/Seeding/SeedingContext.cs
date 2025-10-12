using Microsoft.Extensions.Logging;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Seeding;

public class SeedingContext
{
    public required ApplicationDbContext DbContext { get; init; }
    public required SeedingConfiguration Configuration { get; init; }
    public required IServiceProvider ServiceProvider { get; init; }
    public required ILogger Logger { get; init; }

    public Dictionary<string, object> SharedData { get; } = new();
}

