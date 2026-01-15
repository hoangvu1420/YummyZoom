# Outbox Idle Backoff: Fixing Latency With Wakeup Signals

## Context

We observed a local regression where TeamCart real-time state (Redis VM) was never created after `CreateTeamCart`.
The root cause was the **idle backoff** logic in `OutboxPublisherHostedService`: when the publisher enters a long `Task.Delay(...)`, **new outbox rows cannot wake it up**. If the service had backed off to minutes, freshly-enqueued outbox messages could sit unprocessed until the sleep completes.

This breaks “near real-time” projections (TeamCart RT) even though the outbox row is correctly persisted.

## Why the previous idle backoff is problematic

The pattern “double delay when no work” is fine *only if the wait can be interrupted*.

Without a wakeup signal, an idle backoff loop has an unavoidable worst-case latency:

- Worst-case publish latency ≈ `currentDelay` (can reach `MaxBackoff`)
- New traffic arriving right after the delay starts will not be picked up until it completes

This is distinct from **per-message retry backoff** (`NextAttemptOnUtc`), which is appropriate to be minutes long when a specific event handler is failing.

## Requirements (for YummyZoom)

- **Near real-time**: projections like TeamCart RT should appear in Redis quickly after a write (sub-second to a few seconds).
- **Low idle cost**: avoid constant DB polling at high frequency when the system is idle.
- **Multi-replica compatible**: works when the publisher runs in a separate worker and/or across multiple instances.
- **Graceful fallback**: if the wakeup mechanism fails, polling still drains eventually.

## Recommended approach (best overall): Postgres LISTEN/NOTIFY + fallback polling

### High level

1) Keep `ProcessOnceAsync()` as-is (batch select + publish).
2) Add an **outbox wake channel** in Postgres.
3) After committing new outbox rows, emit `NOTIFY outbox_wakeup`.
4) The outbox publisher waits on either:
   - a `NOTIFY` signal (wake immediately), or
   - a timer tick (fallback polling)

This preserves low idle load but restores immediate responsiveness when traffic arrives.

### Where to emit NOTIFY

Emit after the transaction that enqueued outbox rows successfully commits.

Possible implementation points:

- Inside the EF interceptor that converts domain events to outbox (not ideal for DB side effects).
- Prefer: after `SaveChangesAsync` completes, detect “outbox rows were added” and then `NOTIFY`.
  - This can be done via a dedicated service invoked by the unit-of-work wrapper, or a `SaveChangesInterceptor` that records a flag during `SavingChanges` and executes `NOTIFY` during `SavedChanges`.

### Publisher loop sketch

- Maintain a small polling interval (e.g. 250ms–1s) as fallback.
- Subscribe to Postgres notifications using `NpgsqlConnection` + `LISTEN`.
- Wait using `Task.WhenAny(delayTask, notificationTask)`.
- On notification: drain immediately (reset idle delay).

### Multi-replica behavior

- `NOTIFY` wakes *all* listeners, which is desirable.
- Each publisher still uses `FOR UPDATE SKIP LOCKED`, so only one instance processes a given outbox row.

## Alternative approach (minimal, still safe): cap idle backoff separately from retry backoff

If we want minimal changes without DB notifications:

- Keep per-message retry scheduling (`NextAttemptOnUtc`) with exponential backoff up to minutes.
- Keep publisher polling always, but allow only a **small** idle backoff cap.

Example policy:

- `PollInterval`: 250ms
- `MaxIdleBackoff`: 1–2 seconds (not minutes)
- `MaxRetryBackoff` / `MaxBackoff`: minutes (applies only to failed messages)

This eliminates “publisher asleep for minutes” while still cutting idle DB load substantially compared to fixed 250ms polling.

## In-process wakeups (not sufficient alone)

An in-memory `SemaphoreSlim`/`Channel` “wakeup” triggered when the web request enqueues outbox messages can work in a single-process dev scenario, but:

- It does not wake other replicas.
- It does not help if the publisher runs in a different host (worker).

It can still be used as a small optimization, but it should not be the primary mechanism.

## Suggested configuration shape

Keep existing:

- `OutboxPublisher:BatchSize`
- `OutboxPublisher:PollInterval` (minimum poll / fallback)
- `OutboxPublisher:MaxBackoff` (use for retry backoff, not idle)
- `OutboxPublisher:MaxAttempts` (optional, for DLQ policy)

Add:

- `OutboxPublisher:MaxIdleBackoff` (small cap, seconds)
- `OutboxPublisher:UsePostgresNotify` (bool)
- `OutboxPublisher:NotifyChannel` (string, e.g. `outbox_wakeup`)

## Implementation plan (incremental)

1) Re-introduce idle backoff with a small `MaxIdleBackoff` cap (seconds), keeping current polling-only behavior.
2) Add Postgres `LISTEN/NOTIFY` wakeup:
   - emit `NOTIFY` when outbox rows are inserted
   - publisher waits on notify-or-timeout
3) Optionally re-enable `MaxAttempts` gating + dead-letter policy once wakeup is reliable.

## Acceptance criteria

- After a long idle period, creating a TeamCart results in:
  - outbox row inserted
  - outbox publisher wakes immediately
  - `Outbox: processed ...` appears promptly
  - TeamCart RT VM is present in Redis within expected latency

