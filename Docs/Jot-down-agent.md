## The Core Problem: Synchronous vs. Asynchronous Execution

*   **The Current Functional Test Pattern:** When your test calls `await SendAsync(command)`, the entire process happens sequentially and synchronously within that single `await`:
    1.  Command handler runs.
    2.  `SaveChanges` is called.
    3.  The interceptor fires.
    4.  MediatR **immediately and synchronously** invokes the `INotificationHandler`.
    5.  The handler's logic (e.g., updating another entity, logging to the DB) completes.
    6.  `SendAsync` returns.
    7.  Your test can immediately assert the results of the event handler because its work is already done.

*   **The New Outbox Pattern:** The flow changes significantly.
    1.  Command handler runs.
    2.  `SaveChanges` is called.
    3.  The interceptor fires and **writes a new row to the `OutboxMessage` table**.
    4.  The database transaction commits.
    5.  `SendAsync` returns.
    6.  **Crucially, the event handler has not run yet.** It is waiting in the outbox to be picked up by the background job.

Your existing tests will fail because when they try to assert the outcome of an event (e.g., `await FindAsync<AuditLog>(...)`), that `AuditLog` entity won't exist yet.

---

## Strategy 1

#### Manually Trigger the Outbox Processor

This is the cleanest, fastest, and most reliable approach for functional tests. Your test will explicitly tell the system, "Now, process the events that are waiting in the outbox."

**Step 1: Create a Test Helper to Run the Job**

First, extend your `Testing.cs` facade with a new method that can resolve and execute the `ProcessOutboxMessagesJob`.

```csharp
// In Testing.cs
using MediatR;
using Microsoft.Extensions.DependencyInjection;
// Assuming your job is in this namespace
using YummyZoom.Infrastructure.BackgroundJobs;

public static partial class Testing
{
    // ... existing methods

    public static async Task ProcessOutboxMessagesAsync()
    {
        // Create a new scope to resolve scoped services like the DbContext
        using var scope = _scopeFactory.CreateScope();

        // Resolve the DbContext and MediatR from the test's service provider
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        // Create an instance of the job with the necessary dependencies
        var outboxProcessor = new ProcessOutboxMessagesJob(dbContext, publisher);

        // Execute it
        await outboxProcessor.Execute(CancellationToken.None);
    }
}
```

**Step 2: Update Your Tests**

Now, in any test that needs to assert the result of a domain event, you simply add a call to this new helper after sending the command.

**Example: Before (Old Pattern)**

```csharp
[Test]
public async Task CreateRestaurant_AsOwner_ShouldCreateAuditLog()
{
    // Arrange
    var userId = await RunAsUserAsync("owner@example.com", "password", new[] { Roles.RestaurantOwner });
    var command = new CreateRestaurantCommand { Name = "New Audited Restaurant" };

    // Act
    var result = await SendAsync(command);

    // Assert
    result.ShouldBeSuccessful();
    var auditLog = await FindAsync<AuditLog>(r => r.EntityId == result.Value.ToString());
    auditLog.Should().NotBeNull(); // This works because the handler ran in-process
}
```

**Example: After (New Outbox Pattern)**

```csharp
[Test]
public async Task CreateRestaurant_AsOwner_ShouldCreateAuditLog()
{
    // Arrange
    var userId = await RunAsUserAsync("owner@example.com", "password", new[] { Roles.RestaurantOwner });
    var command = new CreateRestaurantCommand { Name = "New Audited Restaurant" };

    // Act
    var result = await SendAsync(command);
    // This command only creates the Restaurant and the OutboxMessage.

    // This is the new, crucial step.
    await ProcessOutboxMessagesAsync(); // Manually process the outbox.

    // Assert
    result.ShouldBeSuccessful();
    var auditLog = await FindAsync<AuditLog>(r => r.EntityId == result.Value.ToString());
    auditLog.Should().NotBeNull(); // This now passes!
}
```

**Pros of this strategy:**
*   **Deterministic and Fast:** No waiting or delays. The test runs as fast as possible.
*   **Reliable:** Eliminates the flakiness of timing issues. It will never fail because the job "hadn't run yet."
*   **Clear Intent:** The test explicitly shows the two stages of the operation: the initial command and the subsequent event processing.

---

## Strategy 2


### 1) Add a tiny, testable outbox processor interface

Expose the publisher as a service you can **invoke on demand** in tests.

```csharp
public interface IOutboxProcessor
{
    /// Processes up to batchSize messages once. Returns # processed.
    Task<int> ProcessOnceAsync(CancellationToken ct = default);

    /// Repeatedly processes until no work remains.
    Task<int> DrainAsync(TimeSpan? timeout = null, CancellationToken ct = default);
}
```

Your production `OutboxPublisherHostedService` can **compose** this processor (so prod keeps polling), while tests call it directly.

```csharp
public sealed class OutboxProcessor : IOutboxProcessor
{
    private readonly IServiceProvider _sp;
    private readonly OutboxPublisherOptions _options;

    public OutboxProcessor(IServiceProvider sp, IOptions<OutboxPublisherOptions> opts)
    {
        _sp = sp; _options = opts.Value;
    }

    public async Task<int> ProcessOnceAsync(CancellationToken ct = default)
    {
        using var scope = _sp.CreateScope();
        // reuse your existing ‘claim rows → mediator.Publish → mark processed’ logic here
        // return number of processed messages
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
}
```

> Prod: the `BackgroundService` just loops `ProcessOnceAsync` with delays.
> Tests: call `DrainAsync()` explicitly—no sleeps, no flakes.

### 2) Make it accessible in tests

Register both services normally:

```csharp
builder.Services.AddScoped<IOutboxProcessor, OutboxProcessor>();
builder.Services.AddHostedService<OutboxPublisherHostedService>(); // prod polling
```

In tests you can **either** leave the hosted service running (fine) **or** replace it with a no-op to avoid background noise:

```csharp
public sealed class NoopHostedService : IHostedService
{
    public Task StartAsync(CancellationToken _) => Task.CompletedTask;
    public Task StopAsync(CancellationToken _) => Task.CompletedTask;
}
```

Then in a test’s Arrange step:

```csharp
// Optional: silence the background looper for determinism
Testing.ReplaceService<IHostedService>(new NoopHostedService());
```

(Your `Testing.ReplaceService<T>()` API already exists, so this is plug-and-play.)

### 3) Add a one-liner helper to your test facade

In `Testing.cs`:

```csharp
public static async Task DrainOutboxAsync(TimeSpan? timeout = null, CancellationToken ct = default)
{
    var processor = GetService<IOutboxProcessor>();
    await processor.DrainAsync(timeout ?? TimeSpan.FromSeconds(5), ct);
}
```

### 4) Update tests: Act → Drain → Assert

Your previous tests likely did:

```csharp
var result = await SendAsync(cmd);
// ASSERT handler side-effects now
```

Change to:

```csharp
var result = await SendAsync(cmd);
await DrainOutboxAsync();        // <- publish after commit
// ASSERT handler side-effects now
```

That’s it. You keep the same assertions (projected rows created, emails queued, logs, etc.), just add `DrainOutboxAsync()` before asserting.
