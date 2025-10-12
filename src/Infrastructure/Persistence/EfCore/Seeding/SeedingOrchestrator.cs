using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Seeding;

public class SeedingOrchestrator
{
    private readonly IEnumerable<ISeeder> _seeders;
    private readonly ApplicationDbContext _dbContext;
    private readonly SeedingConfiguration _config;
    private readonly ILogger<SeedingOrchestrator> _logger;
    private readonly IServiceProvider _sp;

    public SeedingOrchestrator(
        IEnumerable<ISeeder> seeders,
        ApplicationDbContext dbContext,
        IOptions<SeedingConfiguration> config,
        ILogger<SeedingOrchestrator> logger,
        IServiceProvider sp)
    {
        _seeders = seeders.OrderBy(s => s.Order).ToArray();
        _dbContext = dbContext;
        _config = config.Value;
        _logger = logger;
        _sp = sp;
    }

    public async Task ExecuteSeedingAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting database seeding with profile {Profile}", _config.Profile);

        var context = new SeedingContext
        {
            DbContext = _dbContext,
            Configuration = _config,
            ServiceProvider = _sp,
            Logger = _logger
        };

        foreach (var seeder in _seeders)
        {
            if (IsDisabled(seeder.Name))
            {
                _logger.LogInformation("Seeder {Seeder} is disabled by configuration. Skipping.", seeder.Name);
                continue;
            }

            try
            {
                if (!await seeder.CanSeedAsync(context, cancellationToken))
                {
                    _logger.LogInformation("Seeder {Seeder} reported no work. Skipping.", seeder.Name);
                    continue;
                }

                // each seeder in its own transaction
                var strategy = _dbContext.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                    var result = await seeder.SeedAsync(context, cancellationToken);
                    if (result.IsFailure)
                    {
                        _logger.LogWarning("Seeder {Seeder} failed: {Error}", seeder.Name, result.Error);
                        await tx.RollbackAsync(cancellationToken);
                        return;
                    }

                    await tx.CommitAsync(cancellationToken);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Seeder {Seeder} threw an exception. Continuing.", seeder.Name);
            }
        }

        _logger.LogInformation("Database seeding finished.");
    }

    private bool IsDisabled(string seederName)
    {
        if (_config.EnabledSeeders == null || _config.EnabledSeeders.Count == 0)
            return false; // default enabled if not specified

        return _config.EnabledSeeders.TryGetValue(seederName, out var enabled) && !enabled;
    }
}

