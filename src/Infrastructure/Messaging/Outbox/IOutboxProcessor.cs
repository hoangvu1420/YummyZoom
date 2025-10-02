namespace YummyZoom.Infrastructure.Messaging.Outbox;

public interface IOutboxProcessor
{
    /// <summary>
    /// Processes up to a configured batch size of outbox messages once.
    /// Returns the number of messages processed in this invocation.
    /// </summary>
    Task<int> ProcessOnceAsync(CancellationToken ct = default);

    /// <summary>
    /// Repeatedly processes outbox messages until no work remains or the optional timeout elapses.
    /// Returns the total number of messages processed.
    /// </summary>
    Task<int> DrainAsync(TimeSpan? timeout = null, CancellationToken ct = default);
}


