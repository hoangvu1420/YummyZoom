# Scale-to-Zero Web + Background Processing Separation (Plan)

## Goal

Move all “always-running” background loops out of the `Web` host so:

- `web` (HTTP-only) can **scale to 0 reliably** on Azure Container Apps.
- Outbox publishing has **adaptive polling backoff** (idle = quiet, load = fast).
- Background processing runs in a dedicated **Worker host** (or ACA Jobs where appropriate).
- Longer-term: orchestration code lives in **Application / BackgroundProcessing**, Infrastructure stays as adapters.

## Current state (verified in repo)

### Hosting / DI

- `src/Web/Program.cs` calls `builder.AddInfrastructureServices()` which currently registers **hosted services** via Infrastructure DI.
- `src/Infrastructure/DependencyInjection.cs` includes:
  - `AddBackgroundServices()` → registers:
    - `OutboxPublisherHostedService`
    - `TeamCartExpirationHostedService`
  - `AddReadModelServices()` → registers:
    - `FullMenuViewMaintenanceHostedService`
    - `SearchIndexMaintenanceHostedService`
    - `ReviewSummaryMaintenanceHostedService`
    - `ActiveCouponViewMaintenanceHostedService`
    - `AdminMetricsMaintenanceHostedService`
  - `AddCachingIfConfigured()` can register `CacheInvalidationSubscriber` (Redis pub/sub) as a hosted service.

### Outbox

- `OutboxPublisherHostedService` is a continuous loop:
  - Calls `_processor.ProcessOnceAsync()`
  - Sleeps only when no work: fixed `PollInterval` (250ms)
  - `src/Infrastructure/Messaging/Outbox/OutboxPublisherHostedService.cs`
- `OutboxProcessor` has retry backoff *per message* (exponential w/ jitter) but:
  - `OutboxPublisherOptions.MaxAttempts` is configured yet **not enforced** during selection/processing.
  - `src/Infrastructure/Messaging/Outbox/OutboxProcessor.cs`

### Azure Container Apps (current)

- Only `web` is defined as an AppHost project resource:
  - `src/AppHost/Program.cs` adds `Projects.Web` only.
- The deployed Container App template for `web` pins **`minReplicas: 1`**:
  - `src/AppHost/infra/web.tmpl.yaml`
- `azd` configuration is AppHost-driven with a single service:
  - `src/AppHost/azure.yaml` (points at `AppHost.csproj`)
  - Infra is already generated to disk under `src/AppHost/infra/` (source of truth per `src/AppHost/next-steps.md`).

## Proposed target state

### Runtime topology

- `web` container app:
  - HTTP only.
  - `minReplicas: 0` (scale-to-zero).
  - No “polling” / “maintenance loops”.
- `worker` container app (or jobs):
  - Runs outbox draining and any periodic maintenance.
  - Can be:
    - **Always-on but tiny** (lowest latency), or
    - **Scale-to-zero / job-driven** (lowest cost, accepts latency).

### Layering

- Infrastructure:
  - EF Core, Redis, external clients, repositories, outbox *storage*.
- Application / BackgroundProcessing:
  - Orchestration (outbox batching / retry policy decisions, maintenance workflows).
- Worker host:
  - Composition root: registers hosted services + runners.

## Implementation plan (curated to current codebase)

### Phase 0 — Low-risk wins (can be done first)

1) **Adaptive idle backoff in outbox poller**
   - Change `OutboxPublisherHostedService` so idle delay grows (e.g. doubles up to `MaxBackoff`) and resets to minimum when work is found.
   - Keep `PollInterval` as the minimum.
   - Expected result: fewer DB wakeups and lower idle CPU/telemetry while preserving fast processing under load.

2) **Enforce `MaxAttempts`**
   - Update the selection query in `OutboxProcessor` to exclude messages once `Attempt >= MaxAttempts`.
   - Decide what “give up” means:
     - Option A (minimal schema): stop selecting (leave row unprocessed) + rely on operational alerting.
     - Option B (preferred): add a “dead-letter”/“failed permanently” marker column and expose it via admin tooling.

3) **Bind outbox options from configuration (optional but recommended)**
   - Today options are hard-coded in DI.
   - Move to `builder.Services.Configure<OutboxPublisherOptions>(builder.Configuration.GetSection("OutboxPublisher"))`
   - Add defaults in `src/Web/appsettings*.json` (and later in worker settings).

Notes:
- Phase 0 improves cost/behavior even before splitting hosts, but it does **not** by itself guarantee scale-to-zero (that requires host separation + template change).

### Phase 1 — Split DI so Web stops registering hosted services

Refactor `src/Infrastructure/DependencyInjection.cs` into explicit “common” vs “background” registration.

Suggested shape (names flexible):

- `AddInfrastructureCommonServices()`:
  - persistence, authN/authZ, repositories, external services, caching, etc.
  - must contain everything both `web` and `worker` need.
- `AddInfrastructureBackgroundServices()`:
  - `AddHostedService<OutboxPublisherHostedService>()`
  - `AddHostedService<TeamCartExpirationHostedService>()`
  - read-model maintenance hosted services

Then update `src/Web/Program.cs` to call only:

- `builder.AddInfrastructureCommonServices()` (not the background one)

Decision point: `CacheInvalidationSubscriber`
- It’s currently registered via caching and is “background”, but it is also *part of web-node correctness* (keeps local caches consistent).
- Recommendation:
  - Keep it in `web` if local in-memory caching is used and correctness depends on invalidation.
  - If you want a strict “web is HTTP-only” rule, we need to revisit cache strategy (likely TTL-only + distributed cache, or move invalidation responsibilities).

### Phase 2 — Add a Worker executable + wire it into AppHost

1) Create a new project `src/Worker` (executable worker service):
   - `YummyZoom.Worker` with a `Program.cs` that uses `Host.CreateApplicationBuilder(args)`
   - registers:
     - `AddServiceDefaults()`
     - `AddKeyVaultIfConfigured()` (if needed)
     - `AddApplicationServices()`
     - `AddInfrastructureCommonServices()`
     - `AddInfrastructureBackgroundServices()` (or `AddBackgroundProcessing()` if we split further)

2) Add the Worker to the Aspire graph:
   - Update `src/AppHost/Program.cs`:
     - `var worker = builder.AddProject<Projects.Worker>("worker");`
     - reference the same Postgres DB and Redis resources as `web`.
   - Keep ingress disabled for worker (no external endpoints).

3) Local dev validation:
   - `dotnet run --project src/AppHost/AppHost.csproj`
   - Confirm:
     - web serves HTTP
     - worker drains outbox / runs maintenance (based on enabled flags)

### Phase 3 — Azure infra updates (scale-to-zero + worker deployment)

1) **Web scale-to-zero**
   - Update `src/AppHost/infra/web.tmpl.yaml`:
     - `scale.minReplicas: 0`
     - set `scale.maxReplicas` to a small demo value (e.g. 1–2)
   - Validate in Portal:
     - stop traffic → replicas should drop to 0 after idle period.

2) **Add worker container app template**
   - Add `src/AppHost/infra/worker.tmpl.yaml` modeled after `web.tmpl.yaml`, but:
     - no external ingress (or internal-only, depending on desired health/ops)
     - same managed identity / ACR pull setup
     - same Key Vault secret refs for:
       - `ConnectionStrings__YummyZoomDb`
       - `ConnectionStrings__redis`
     - scaling options:
       - Option A (lowest latency): `minReplicas: 1` with tiny CPU/mem
       - Option B (lower cost): `minReplicas: 0` + accept pauses unless we add a scaler/job trigger

3) Confirm `azd` flow
   - Because infra is generated to disk (`src/AppHost/infra/`), verify whether:
     - adding worker requires running `azd infra gen` (may overwrite), or
     - adding `worker.tmpl.yaml` + AppHost graph change is sufficient.
   - Explicitly test: `azd deploy` from `src/AppHost` and confirm a `worker` Container App appears.

### Phase 4 — Convert periodic recon/maintenance to Azure Container Apps Jobs (best cost)

The read model maintenance hosted services are long-running loops with intervals. These fit ACA Jobs well.

1) Refactor each maintenance hosted service into a “run once and exit” runner in Application:
   - e.g. `IFullMenuMaintenance.RunOnceAsync()`, `ISearchIndexMaintenance.RunOnceAsync()`, etc.
   - Keep the existing logic, but remove infinite loops from the job runner entrypoint.

2) Add a small console entrypoint:
   - Option A: reuse `YummyZoom.Worker` with `--job <name>` arguments.
   - Option B: a dedicated `YummyZoom.Jobs` project.

3) Provision ACA Jobs in Bicep:
   - Add a new Bicep module (e.g. `src/AppHost/infra/jobs/*.bicep`) defining:
     - schedules (cron)
     - identity + Key Vault secret access
     - image (same ACR)
   - Start with the highest-cost/lowest-value loop first (typically the ones running every few minutes).

### Phase 5 — Layer cleanup (move orchestration to Application)

After Phase 1–4 are working, migrate code placement to match desired architecture:

- Move `OutboxProcessor` orchestration to Application (or `BackgroundProcessing` project).
- Keep Infrastructure responsible for:
  - EF Core queries and persistence
  - Redis/integration adapters
- Keep hosted-service “loop” code in Worker/Jobs only.

This phase is mostly “move files + adjust namespaces + update DI” and should be done last to reduce risk.

## Suggested sequencing (to avoid breaking behavior)

Recommended order for implementation in this repo:

1) Phase 0 (outbox adaptive backoff + max-attempts enforcement)
2) Phase 1 + Phase 2 (worker host + DI split) — keep web `minReplicas: 1` during this step
3) Phase 3 (web scale-to-zero + add worker template)
4) Phase 4 (convert recon/maintenance loops to ACA Jobs)
5) Phase 5 (layer cleanup)

## Validation checklist

- Local:
  - `dotnet run --project src/AppHost/AppHost.csproj` starts both web + worker.
  - Create an outbox message (trigger a domain event) and confirm worker publishes it.
- Azure:
  - `web` scales to 0 when idle.
  - Outbox continues to drain (either via worker app or job schedule).
  - No maintenance jobs run in web (confirm via logs/revision behavior).

## Open questions / clarifications

1) **Outbox latency requirement**: is “near real-time” required (seconds), or is “batched every 30–60s” acceptable?
2) **Worker availability**: should outbox draining continue even when web is at 0 (usually yes)?
3) **Recon/maintenance frequency**: are the current `ReconInterval` values required, or can we switch to cron-based jobs?
4) **Cache invalidation**: do we require `CacheInvalidationSubscriber` in web for correctness, or can we rely on TTL/distributed cache?
5) **Failure handling**: what is the desired behavior when outbox messages exceed `MaxAttempts` (dead-letter table, alerting, admin UI)?

## Additional suggestions (beyond the original guide)

- Add structured metrics around outbox:
  - processed count, failure count, “oldest unprocessed age”, dead-letter count.
- Consider making outbox “event-driven” later (optional complexity):
  - Postgres `LISTEN/NOTIFY` to reduce polling
  - or move to a queue-based outbox dispatcher if/when Service Bus is introduced
- Make background services opt-in via config per host:
  - e.g. `OUTBOX__ENABLED=true` only on worker.

