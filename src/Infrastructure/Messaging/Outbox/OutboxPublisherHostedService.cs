using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace YummyZoom.Infrastructure.Messaging.Outbox;

public sealed class OutboxPublisherOptions
{
    public const string SectionName = "OutboxPublisher";

    public int BatchSize { get; set; } = 50;
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMilliseconds(250);
    // Cap for "no work found" polling backoff. Keep this small (seconds) to avoid multi-minute
    // latency where new outbox rows arrive while the publisher is sleeping.
    public TimeSpan MaxIdleBackoff { get; set; } = TimeSpan.FromSeconds(2);
    public TimeSpan MaxBackoff { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxAttempts { get; set; } = 10;
}

public sealed class OutboxPublisherHostedService : BackgroundService
{
    private readonly OutboxPublisherOptions _options;
    private readonly IOutboxProcessor _processor;

    public OutboxPublisherHostedService(
        IOptions<OutboxPublisherOptions> options,
        IOutboxProcessor processor)
    {
        _options = options.Value;
        _processor = processor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var minDelay = _options.PollInterval;
        var maxIdleDelay = _options.MaxIdleBackoff < minDelay ? minDelay : _options.MaxIdleBackoff;
        var currentDelay = minDelay;

        while (!stoppingToken.IsCancellationRequested)
        {
            var n = await _processor.ProcessOnceAsync(stoppingToken);
            if (n > 0)
            {
                currentDelay = minDelay;
                continue;
            }

            await Task.Delay(currentDelay, stoppingToken);
            currentDelay = TimeSpan.FromMilliseconds(
                Math.Min(currentDelay.TotalMilliseconds * 2, maxIdleDelay.TotalMilliseconds));
        }
    }
}
