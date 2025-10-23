using System.Diagnostics;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YummyZoom.Application.Common.Interfaces;

namespace YummyZoom.Infrastructure.Persistence.ReadModels.Coupons;

/// <summary>
/// Background service that periodically refreshes the active_coupons_view materialized view.
/// </summary>
public sealed class ActiveCouponViewMaintenanceHostedService : BackgroundService
{
    private readonly ActiveCouponViewMaintenanceOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ActiveCouponViewMaintenanceHostedService> _logger;

    public ActiveCouponViewMaintenanceHostedService(
        IOptions<ActiveCouponViewMaintenanceOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<ActiveCouponViewMaintenanceHostedService> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("ActiveCouponViewMaintenance: disabled via configuration");
            return;
        }

        if (_options.InitialDelay > TimeSpan.Zero)
        {
            _logger.LogInformation("ActiveCouponViewMaintenance: waiting {InitialDelay} before starting", _options.InitialDelay);
            try 
            { 
                await Task.Delay(_options.InitialDelay, stoppingToken); 
            }
            catch (OperationCanceledException) 
            { 
                return; 
            }
        }

        // Initial refresh on startup
        await RefreshOnceAsync(stoppingToken);

        // Periodic refresh
        while (!stoppingToken.IsCancellationRequested)
        {
            try 
            { 
                await Task.Delay(_options.RefreshInterval, stoppingToken); 
            }
            catch (OperationCanceledException) 
            { 
                break; 
            }

            await RefreshOnceAsync(stoppingToken);
        }
    }

    private async Task RefreshOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbConnectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();

            var stopwatch = Stopwatch.StartNew();

            using var connection = dbConnectionFactory.CreateConnection();
            
            // Refresh the materialized view
            await connection.ExecuteAsync("REFRESH MATERIALIZED VIEW CONCURRENTLY active_coupons_view;");
            
            stopwatch.Stop();

            _logger.LogInformation("ActiveCouponViewMaintenance: refreshed materialized view in {ElapsedMs}ms", 
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ActiveCouponViewMaintenance: failed to refresh materialized view");
        }
    }
}
