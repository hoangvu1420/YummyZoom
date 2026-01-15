using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace YummyZoom.Infrastructure.Messaging.Outbox;

public sealed class OutboxPublisherOptions
{
    public const string SectionName = "OutboxPublisher";

    public int BatchSize { get; set; } = 50;
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMilliseconds(250);
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
        while (!stoppingToken.IsCancellationRequested)
        {
            var n = await _processor.ProcessOnceAsync(stoppingToken);
            if (n == 0)
                await Task.Delay(_options.PollInterval, stoppingToken);
        }
    }
}
