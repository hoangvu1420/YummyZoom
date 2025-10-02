using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.TeamCartAggregate.Enums;

namespace YummyZoom.Infrastructure.BackgroundServices;

public sealed class TeamCartExpirationHostedService : BackgroundService
{
    private readonly ILogger<TeamCartExpirationHostedService> _logger;
    private readonly IOptionsMonitor<TeamCartExpirationOptions> _optionsMonitor;
    private readonly IServiceProvider _services;
    private readonly TimeProvider _timeProvider;

    public TeamCartExpirationHostedService(
        ILogger<TeamCartExpirationHostedService> logger,
        IOptionsMonitor<TeamCartExpirationOptions> optionsMonitor,
        IServiceProvider services,
        TimeProvider timeProvider)
    {
        _logger = logger;
        _optionsMonitor = optionsMonitor;
        _services = services;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _optionsMonitor.CurrentValue;
        if (!options.Enabled)
        {
            _logger.LogInformation("TeamCartExpirationHostedService is disabled via configuration.");
            return;
        }

        _logger.LogInformation("TeamCartExpirationHostedService started: cadence={Cadence}, batch={Batch}, grace={Grace}",
            options.Cadence, options.BatchSize, options.GraceWindow);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOneIterationAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TeamCartExpirationHostedService iteration failed");
            }

            try
            {
                await Task.Delay(_optionsMonitor.CurrentValue.Cadence, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunOneIterationAsync(CancellationToken ct)
    {
        var options = _optionsMonitor.CurrentValue;
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var cutoff = now - options.GraceWindow;
        var totalExpired = 0;

        using var scope = _services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ITeamCartRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        while (!ct.IsCancellationRequested)
        {
            var candidates = await repo.GetExpiringCartsAsync(cutoff, options.BatchSize, ct);
            if (candidates.Count == 0)
            {
                break;
            }

            foreach (var cart in candidates)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    if (cart.Status is TeamCartStatus.Converted or TeamCartStatus.Expired)
                    {
                        continue;
                    }

                    var result = cart.MarkAsExpired();
                    if (result.IsFailure)
                    {
                        // Guarded failure shouldn't happen, but log and continue
                        _logger.LogWarning("MarkAsExpired failed for CartId={CartId}: {Error}", cart.Id.Value, result.Error);
                        continue;
                    }

                    await uow.SaveChangesAsync(ct);
                    totalExpired++;
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Another node updated the row; safe to ignore
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to expire TeamCart {CartId}", cart.Id.Value);
                }
            }
        }

        if (totalExpired > 0)
        {
            _logger.LogInformation("Expired {Count} TeamCarts in this iteration (cutoff={Cutoff:o})", totalExpired, cutoff);
        }
    }
}


