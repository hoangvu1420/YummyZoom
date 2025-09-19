using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YummyZoom.Application.Common.Interfaces.IServices;

namespace YummyZoom.Infrastructure.Persistence.ReadModels.Admin;

public sealed class AdminMetricsMaintenanceHostedService : BackgroundService
{
    private readonly AdminMetricsMaintenanceOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AdminMetricsMaintenanceHostedService> _logger;

    public AdminMetricsMaintenanceHostedService(
        IOptions<AdminMetricsMaintenanceOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<AdminMetricsMaintenanceHostedService> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("AdminMetricsMaintenance: disabled via configuration");
            return;
        }

        if (_options.InitialDelay > TimeSpan.Zero)
        {
            try
            {
                await Task.Delay(_options.InitialDelay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await RecomputeOnceAsync(stoppingToken);

            try
            {
                await Task.Delay(_options.Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RecomputeOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var maintainer = scope.ServiceProvider.GetRequiredService<IAdminMetricsMaintainer>();
            await maintainer.RecomputeAllAsync(_options.DailySeriesWindowDays, ct);
            _logger.LogInformation("AdminMetricsMaintenance: recompute pass completed");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "AdminMetricsMaintenance: recompute pass failed");
        }
    }
}
