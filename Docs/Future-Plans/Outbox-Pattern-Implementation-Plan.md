## Outbox Pattern – Project-Aligned Analysis and Implementation Plan

### Scope and goals

- Goal: Persist domain events transactionally with aggregate writes and publish them asynchronously after commit. Keep existing domain events and MediatR notification handlers. Ensure exactly-once persistence; at-least-once delivery.
- Non-goals: Change domain model, switch away from CQRS, or alter handler contracts.

### What we have today (current state)

- Architecture: Clean Architecture with DDD. Commands use repositories + aggregates + IUnitOfWork. Queries use Dapper via IDbConnectionFactory.
- Domain events: Aggregates raise events via AddDomainEvent(...) per Domain_Layer_Guidelines.md.
- Dispatch: src/Infrastructure/Data/Interceptors/DispatchDomainEventsInterceptor.cs publishes domain events via MediatR inside EF SaveChanges interceptor.
- DI wiring: Infrastructure.DependencyInjection registers AuditableEntityInterceptor, SoftDeleteInterceptor, and DispatchDomainEventsInterceptor. No outbox classes yet.

Implication: Handlers run in-transaction today. We need to switch to enqueue-in-transaction and publish-out-of-transaction from a background worker.

### Alignment with project patterns

- Domain layer: unchanged; keep aggregates/IDs/events pure and side-effect-free. Events remain IDomainEvent.
- Application layer: unchanged command/query patterns. Event handlers stay INotificationHandler<TEvent>; recommend optional idempotency base if handlers perform side-effects.
- Infrastructure layer: adds outbox entity/config, a SaveChangesInterceptor to enqueue, and a hosted background publisher. Uses PostgreSQL (UseNpgsql).
- Testing: Functional tests adapt to async publishing by triggering outbox processing deterministically.

### Design summary (adapted to this repo)

- Add OutboxMessage entity in src/Infrastructure/Data/Outbox/ with EF configuration in src/Infrastructure/Data/Configurations/ and a DbSet on ApplicationDbContext.
- Replace DispatchDomainEventsInterceptor with ConvertDomainEventsToOutboxInterceptor that serializes domain events to outbox rows and clears pending events during SavingChanges/SavingChangesAsync.
- Add OutboxPublisherHostedService to claim and publish pending messages using MediatR, with exponential backoff and retry metadata.
- Wire options and hosted service in Infrastructure.DependencyInjection. Keep existing interceptors; remove registration of the current dispatch interceptor.
- Optional: add inbox/idempotency helper for handlers that touch external systems or write denormalized projections. THIS WILL BE IMPLEMENTED IN LATER SPRINT.

### Implementation plan (step-by-step)

1) Infrastructure: Outbox entity + EF config + DbContext
- Add src/Infrastructure/Data/Outbox/OutboxMessage.cs with:
  - Id (Guid), OccurredOnUtc, Type (assembly qualified name), Content (jsonb), CorrelationId, CausationId, AggregateId, AggregateType, Attempt, NextAttemptOnUtc, ProcessedOnUtc, Error.
  - Static factory FromDomainEvent(object @event, DateTime nowUtc, ...) using System.Text.Json.
- Add src/Infrastructure/Data/Configurations/OutboxMessageConfiguration.cs:
  - Table OutboxMessages, indexes on ProcessedOnUtc, NextAttemptOnUtc, OccurredOnUtc, Content mapped as jsonb.
- Update ApplicationDbContext:
  - Add public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
  - Apply configuration in OnModelCreating.
- Create EF migration and apply to database.

2) Infrastructure: Interceptor to enqueue (not publish)
- Add src/Infrastructure/Data/Interceptors/ConvertDomainEventsToOutboxInterceptor.cs:
  - On SavingChanges/SavingChangesAsync, collect IHasDomainEvent entities with pending events.
  - Map each domain event to an OutboxMessage (include aggregate id string if available), add to context, then clear domain events.
- Dependency Injection cutover in Infrastructure.DependencyInjection:
  - Remove/disable registration of DispatchDomainEventsInterceptor.
  - Register ConvertDomainEventsToOutboxInterceptor along with existing interceptors.

3) Infrastructure: Background publisher
- Add src/Infrastructure/Outbox/OutboxPublisherHostedService.cs with OutboxPublisherOptions.
- Logic:
  - Loop with poll interval; within each batch create scope, begin transaction (ReadCommitted).
  - Claim rows due with FOR UPDATE SKIP LOCKED ordered by OccurredOnUtc and limit by BatchSize.
  - For each message: deserialize event type by AssemblyQualifiedName, commit lock transaction to release locks, publish via MediatR, mark processed. On failure, increment Attempt, set NextAttemptOnUtc with exponential backoff + jitter; park after MaxAttempts.
- Options defaults: BatchSize=50, PollInterval=200-250ms, MaxBackoff=5m, MaxAttempts=10.
- DI: register options and AddHostedService<OutboxPublisherHostedService>() in API host (or a worker project later if we split responsibilities).

4) Serialization and value objects
- If events include strongly-typed IDs or VOs, ensure matching JSON converters are configured in both the interceptor and worker (shared JsonSerializerOptions).
- Persist Type as assembly-qualified name to allow deserialization across versions.

5) Idempotency (recommended for side-effecting handlers)
- Minimal option: ensure natural/unique keys (e.g., upserts or unique constraints) on handler write targets.
- Robust option: introduce an InboxMessages table with composite key {EventId, Handler} and a base handler IdempotentNotificationHandler<TEvent> that records processing in the same transaction as side effects. Requires events to carry a stable EventId.

6) Testing strategy
- Unit tests: unchanged for aggregates raising events.
- Functional tests: after SendAsync(command), trigger outbox processing deterministically.
  - Add a test helper (e.g., Testing.ProcessOutboxMessagesAsync()) that resolves ApplicationDbContext and IPublisher and runs a one-off processor method or directly calls the hosted service’s internal batch method.
  - Update tests that assert event-driven outcomes to call the helper before assertions.
- Concurrency tests: run 2+ parallel processors to verify no double-processing with SKIP LOCKED.

7) Observability and operations
- Emit logs and basic metrics: processed count, lag (now − OccurredOnUtc), attempts, failures, queue depth.
- Add a retention job: periodical deletion of processed rows older than N days; leave failed rows longer.

8) Rollout plan
- Phase 1 (behind a flag): implement outbox pieces, register enqueue interceptor, keep current dispatcher disabled in non-prod; run migrations and verify publishing in staging.
- Phase 2: enable hosted service in production; monitor lag/failures; tune batch size and poll interval.
- Phase 3: optional inbox/idempotency migration for sensitive handlers; add cleanup job and metrics dashboards.

### Concrete edits checklist

- Add files:
  - src/Infrastructure/Data/Outbox/OutboxMessage.cs
  - src/Infrastructure/Data/Configurations/OutboxMessageConfiguration.cs
  - src/Infrastructure/Data/Interceptors/ConvertDomainEventsToOutboxInterceptor.cs
  - src/Infrastructure/Outbox/OutboxPublisherHostedService.cs
- Modify:
  - src/Infrastructure/Data/ApplicationDbContext.cs: add DbSet<OutboxMessage> and apply configuration.
  - src/Infrastructure/DependencyInjection.cs: replace DispatchDomainEventsInterceptor registration with ConvertDomainEventsToOutboxInterceptor; register options + hosted service.
- Create EF migration: AddOutboxMessages (table, indexes) and apply.
- Tests: add Testing.ProcessOutboxMessagesAsync(); update functional tests that assert event effects.

### Risks and mitigations

- Handler duplicates: use inbox table or natural unique keys for side effects.
- Serialization drift: keep event contracts additive; use converters for IDs/VOs.
- Throughput/locks: use FOR UPDATE SKIP LOCKED, small batches; scale horizontally with multiple workers.
- Test flakiness: avoid Task.Delay; use deterministic outbox trigger helper. TESTS WILL BE ENHANCED IN LATER SPRINT.

### Acceptance criteria

- Domain events are persisted to OutboxMessages within the same transaction as aggregate changes.
- Event handlers execute asynchronously after commit via hosted worker.
- Retries with backoff; poisoned messages are parked after MaxAttempts with diagnostic error text.
- No breaking changes to domain/application contracts; CQRS conventions remain intact.
