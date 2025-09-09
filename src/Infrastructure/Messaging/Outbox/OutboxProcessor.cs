using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using YummyZoom.Domain.Common.Models;
using YummyZoom.Infrastructure.Persistence.EfCore;
using YummyZoom.Infrastructure.Serialization;
using YummyZoom.Infrastructure.Serialization.JsonOptions;

namespace YummyZoom.Infrastructure.Messaging.Outbox;

public sealed class OutboxProcessor : IOutboxProcessor
{
	private readonly IServiceProvider _serviceProvider;
	private readonly OutboxPublisherOptions _options;
	private readonly ILogger<OutboxProcessor> _logger;
	private static readonly JsonSerializerOptions JsonOptions = OutboxJson.Options;

	public OutboxProcessor(
		IServiceProvider serviceProvider,
		IOptions<OutboxPublisherOptions> options,
		ILogger<OutboxProcessor> logger)
	{
		_serviceProvider = serviceProvider;
		_options = options.Value;
		_logger = logger;
	}

	public async Task<int> ProcessOnceAsync(CancellationToken ct = default)
	{
		using var scope = _serviceProvider.CreateScope();
		var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
		var now = DateTime.UtcNow;

		var strategy = dbContext.Database.CreateExecutionStrategy();
		return await strategy.ExecuteAsync(async () =>
		{
			var toPublish = await dbContext.OutboxMessages
				.FromSqlRaw(@"
					SELECT * FROM ""OutboxMessages""
					WHERE ""ProcessedOnUtc"" IS NULL
					  AND (""NextAttemptOnUtc"" IS NULL OR ""NextAttemptOnUtc"" <= NOW())
					ORDER BY ""OccurredOnUtc""
					FOR UPDATE SKIP LOCKED
					LIMIT {0}", _options.BatchSize)
				.ToListAsync(ct);

			if (toPublish.Count == 0)
			{
				return 0;
			}

			var processedCount = 0;

			foreach (var msg in toPublish)
			{
                try
                {
                    var type = Type.GetType(msg.Type, throwOnError: true)!;
                    var @event = (IDomainEvent?)JsonSerializer.Deserialize(msg.Content, type, JsonOptions);
                    if (@event is null)
                        throw new InvalidOperationException($"Deserialized event is null for {msg.Id} ({msg.Type}).");

                    // Publish the event
                    await mediator.Publish(@event, ct);

                    // Update the outbox message as processed
                    using var postScope = _serviceProvider.CreateScope();
                    var db2 = postScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    db2.Attach(msg);
                    msg.ProcessedOnUtc = DateTime.UtcNow;
                    msg.Error = null;
                    await db2.SaveChangesAsync(ct);

                    processedCount++;
                    _logger.LogInformation("Outbox: published event {Type} OutboxId={OutboxId}", msg.Type, msg.Id);
                }
                catch (Exception ex)
                {
                    using var errScope = _serviceProvider.CreateScope();
                    var db2 = errScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    db2.Attach(msg);

                    msg.Attempt += 1;
                    var backoff = ComputeBackoff(msg.Attempt, _options.MaxBackoff);
                    msg.NextAttemptOnUtc = now + backoff;
                    msg.Error = ex.ToString();

                    await db2.SaveChangesAsync(ct);

                    _logger.LogError(ex, "Outbox: failed to publish event {Type} OutboxId={OutboxId}", msg.Type, msg.Id);
				}
			}

			return processedCount;
		});
	}

	public async Task<int> DrainAsync(TimeSpan? timeout = null, CancellationToken ct = default)
	{
		var until = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
		var total = 0;
		while (DateTime.UtcNow < until)
		{
			var n = await ProcessOnceAsync(ct);
			total += n;
			if (n == 0) break;
		}
		return total;
	}

	private static TimeSpan ComputeBackoff(int attempt, TimeSpan cap)
	{
		var baseMs = Math.Pow(2, Math.Min(attempt, 10)) * 100;
		var jitter = Random.Shared.Next(0, 100);
		var ms = Math.Min(baseMs + jitter, cap.TotalMilliseconds);
		return TimeSpan.FromMilliseconds(ms);
	}
}


