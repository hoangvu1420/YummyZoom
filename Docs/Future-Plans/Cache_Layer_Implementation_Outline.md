# Cache Layer Implementation Outline

Purpose: establish a first-class caching layer that complements our CQRS/read-model strategy, reduces read latency and DB load, and cleanly integrates with our Clean Architecture and event-driven invalidation.

This outline proposes interfaces, patterns, and concrete adoption points for both existing features (e.g., FullMenuView, Search, ReviewSummary, Restaurant public info) and future features (e.g., TeamCart real-time view), following .NET caching best practices.


## Status Update (Current)
- Redis runtime: wired via .NET Aspire as `ConnectionStrings:Redis` and referenced by the Web project.
- DI registration: Web chooses Redis (`IDistributedCache`) when `ConnectionStrings:Redis` present, falls back to `IMemoryCache` otherwise.
- Tests: Redis Testcontainers integrated into functional test host; smoke test for `IDistributedCache` set/get passes.

Scope not yet done: application-level cache abstractions, MediatR caching behavior, invalidation bus, and adopting caching in specific queries.

## Action Plan (Next Steps)
1) Application abstractions
   - Define `ICacheService`, `CachePolicy`, `ICacheKeyFactory`, `ICacheInvalidationPublisher`, and marker `ICacheableQuery<T>`.
   - Provide default JSON serialization policy for cache payloads (reuse existing `DomainJson` options if suitable).
2) Implement cache services
   - `DistributedCacheService` backed by `IDistributedCache` (Redis), including tag support scaffolding.
   - `MemoryCacheService` for local/micro-cache scenarios (same interface, no tag pub/sub).
3) MediatR pipeline
   - Add `CachingBehaviour<TRequest,TResponse>` (cache-aside) and register after validation/authorization.
4) First adoptions (low risk)
   - Restaurant public info query and ReviewSummary query: add keys, policies (TTL), and mark as `ICacheableQuery<T>`.
   - Add unit tests for key generation and policy handling; functional tests to assert hit/miss behavior via counters or logs.
5) Invalidation bus
   - Implement `RedisCacheInvalidationBus` publisher + hosted subscriber (pub/sub channel).
   - Hook maintainers (`FullMenuViewMaintainer`, `SearchIndexMaintainer`, `ReviewSummaryMaintainer`) to publish targeted invalidations on successful updates.
6) Observability
   - Add metrics (hit/miss, set/evict, invalidate msgs) and structured logs; optional traces.
7) Optional web output caching
   - Evaluate `OutputCache` for selected public endpoints (search, restaurant info) with short TTLs.
8) TeamCart (separate PR)
   - Design write-through real-time VM in Redis; commands/events update Redis; queries read only from cache.

## Objectives
- Performance: reduce latency and DB load on hot paths (read-heavy endpoints).
- Correctness: predictable freshness with clear invalidation rules tied to domain/read-model events.
- Simplicity: opt-in, discoverable abstractions with minimal friction for handlers.
- Horizontal scale: distributed cache + pub/sub invalidation to keep multi-instance nodes coherent.
- Observability: metrics for hit/miss, latency, size, and invalidations.

## Non-Goals
- Caching command-side behavior or aggregates (we keep domain writes consistent via repositories and UoW).
- DIY second-level caching inside EF Core. We rely on dedicated read models and targeted app-level cache.


## What to Cache (Initial Catalog)
- FullMenuView JSON by restaurantId for API responses (already ETag/304 at HTTP layer).
- Restaurant public info by restaurantId.
- ReviewSummary by restaurantId (denormalized counts/average).
- Search result pages for common queries (small TTL, geospatial variants bucketed).
- Order status by orderId (very short TTL) to smooth polling.
- TeamCart real-time view model in Redis (authoritative, write-through; strictly no SQL for real-time view per docs).

## Caching Patterns & Where They Fit
- Cache-aside for most query handlers: on miss, execute handler → cache result → return.
- Write-through for TeamCart real-time VM: command/event handlers update Redis first-class structures; queries read only from cache.
- Stale-while-revalidate (optional, phase 2) for popular search endpoints.
- HTTP caching already used for FullMenuView via ETag/Last-Modified/Cache-Control; can add OutputCache for select public endpoints.


## Architectural Placement (Clean Architecture)
- Application layer: define cache abstractions, marker interfaces, and MediatR behavior.
  - Interfaces: ""ICacheService"", ""ICacheSerializer"", ""ICachePolicy"", ""ICacheKeyFactory"", ""ICacheInvalidationPublisher"".
  - Marker: ""ICacheableQuery<T>"" exposes key inputs and policy.
  - Pipeline: ""CachingBehaviour<TRequest,TResponse>"" implements cache-aside for cacheable queries.
- Infrastructure layer: provide concrete implementations.
  - ""MemoryCacheService"" using ""IMemoryCache"" (dev/local, micro-cache scenarios).
  - ""DistributedCacheService"" using ""IDistributedCache"" backed by Redis (StackExchange.Redis).
  - Redis pub/sub invalidation: ""RedisCacheInvalidationBus"" and a hosted subscriber to fan-out evictions across instances.
- Web layer: optional OutputCache for public endpoints (short TTL, vary-by route/query), continue to emit HTTP validators (ETag/Last-Modified).

## Abstractions (Application)
- `ICacheService`
  - `Task<T?> GetAsync<T>(string key, CancellationToken)`
  - `Task SetAsync<T>(string key, T value, CachePolicy policy, CancellationToken)`
  - `Task RemoveAsync(string key, CancellationToken)`
  - `Task InvalidateByTagAsync(string tag, CancellationToken)` (no-op in memory; meaningful with Redis via tag-sets/pubsub)
- `CachePolicy`
  - `TimeSpan? AbsoluteExpiration`, `TimeSpan? SlidingExpiration`, `bool AllowStaleWhileRevalidate`, `TimeSpan? StaleTtl`, `string[] Tags`
- `ICacheKeyFactory`
  - Deterministic, namespaced keys; see Key Scheme below.
- `ICacheInvalidationPublisher`
  - Publishes `CacheInvalidationMessage { Keys, Tags, Reason, SourceEvent, OccurredUtc }`.
- `ICacheableQuery<TResponse>`
  - `string CacheKey { get; }`, `CachePolicy Policy { get; }` (or a `GetCacheKey()` + `GetPolicy()`)

### MediatR Caching Behaviour (Application)
- Registered after validation/authorization but before performance/logging behaviors.
- Logic (cache-aside):
  1) If `TRequest` implements `ICacheableQuery<TResponse>`, try `ICacheService.GetAsync<TResponse>(CacheKey)`.
  2) On miss, call inner handler; if success, `SetAsync` with provided `Policy` and tags; return value.
  3) Optionally set a small jitter to reduce stampedes.
  4) Never throw on cache failure; fall back to source and log warning.
## Invalidation Strategy
- Event-driven: map domain/read-model events to keys/tags.
  - On any write that changes projected or read-side state, publish invalidation messages through the outbox → invalidation bus.
  - Read-model maintainers (e.g., `FullMenuViewMaintainer`, `SearchIndexMaintainer`, `ReviewSummaryMaintainer`) emit targeted invalidations after successful UPSERT.
- Tag-based: group keys for coarse invalidation.
  - Example tags:
    - `restaurant:{id}:menu`, `restaurant:{id}:public-info`, `restaurant:{id}:reviews`
    - `search:geo:{geohash-precision}`, `search:q:{normalized}`
    - `order:{id}:status`
    - `teamcart:{id}`
- TTL fallback: even without explicit invalidation, short TTLs limit staleness for search and status endpoints.
- Cross-instance coherence: Redis pub/sub delivers `{tags=[...], keys=[...]}` messages to all nodes; each node evicts IMemoryCache and Redis entries as needed.

## Key Scheme & Namespacing
- Prefix everything with application and environment to avoid collisions:
  - `yz:{env}:{area}:{entity}:{id-or-composite}:v{n}`
- Examples:
  - FullMenuView: `yz:prod:menu:full:{restaurantId}:v1`
  - RestaurantPublicInfo: `yz:prod:restaurant:info:{restaurantId}:v1`
  - ReviewSummary: `yz:prod:reviews:summary:{restaurantId}:v1`
  - SearchRestaurants: `yz:prod:search:r:{qHash}:{city?}:{page}:{size}:v2`
  - OrderStatus: `yz:prod:order:status:{orderId}:v1`
  - TeamCart VM: `yz:prod:teamcart:vm:{teamCartId}:v1`
- Version bump (`vN`) is the safe-rollback lever when response shape changes.
## Candidate Policies (Initial)
- FullMenuView: `absolute=5m`, tag `restaurant:{id}:menu`; invalidate on any menu/menuitem/category/customization change; also emit HTTP ETag already in place.
- Restaurant Public Info: `absolute=10m`, tag `restaurant:{id}:public-info`; invalidate on `RestaurantUpdated`.
- ReviewSummary: `absolute=10m`, tag `restaurant:{id}:reviews`; invalidate on recompute.
- SearchRestaurants page: `absolute=60s` (no invalidation, TTL-only) or soft tag invalidation per geohash if desired.
- OrderStatus: `absolute=5s`, tag `order:{id}:status`; invalidate on lifecycle events.
- TeamCart VM: write-through; TTL `24h`; invalidate/remove on `TeamCartConverted|Expired`.

## Integration Points (Existing Code)
- Application pipeline: add `CachingBehaviour<,>` to `src/Application/DependencyInjection.cs` after Authorization and before Performance/Logging.
- Read-model maintainers:
  - `FullMenuViewMaintainer`: after UPSERT, publish invalidation `tags=[restaurant:{id}:menu]`.
  - `SearchIndexMaintainer`: after upsert/soft-delete, publish invalidation `tags=[search:*]` (if we choose tag invalidation) else rely on short TTL.
  - `ReviewSummaryMaintainer`: after recompute, publish `tags=[restaurant:{id}:reviews]`.
- Public endpoints:
  - Keep ETag/Last-Modified for `/restaurants/{id}/menu` as implemented.
  - Consider ASP.NET Core OutputCache for `/restaurants/search` with vary-by route+query and `max-age=30-60s`.
- TeamCart:
  - Real-time view model hosted service writes to Redis structures (hash or JSON) on relevant domain events.
  - `GetTeamCartRealTimeViewModelQuery` reads exclusively from cache.
## Concrete Implementations (Infrastructure)
- `DistributedCacheService` (Redis-backed via `IDistributedCache`):
  - JSON `System.Text.Json` serializer with camelCase, ignore nulls, allow large objects.
  - Use `DistributedCacheEntryOptions` mapping from `CachePolicy`.
  - Tagging: maintain a Redis Set per tag → `tag:{tag}` containing keys, or publish-only invalidations and rely on clients to compute keys.
  - Pub/Sub channel `yz:{env}:cache:invalidate` sending `{"keys":[],"tags":[],"reason":"...","ts":"..."}`.
- `MemoryCacheService` (IMemoryCache):
  - Same interface; tag invalidation implemented via in-process index `ConcurrentDictionary<string, HashSet<string>>` guarded carefully; adequate for dev.
- Hosted `CacheInvalidationSubscriber`:
  - Subscribes to Redis channel; evicts local memory entries and forwards to Redis if needed.

## Configuration & DI
- Packages:
  - `Microsoft.Extensions.Caching.StackExchangeRedis`
  - `StackExchange.Redis` (for pub/sub convenience if needed beyond IDistributedCache)
- App settings (example):
```json
{
  "Cache": {
    "Provider": "Redis", // or "Memory"
    "Redis": {
      "ConnectionString": "localhost:6379,ssl=false,abortConnect=false",
      "InstanceName": "yz:dev:",
      "PubSubChannel": "yz:dev:cache:invalidate"
    },
    "Defaults": {
      "JitterMs": 250,
      "FailOpen": true
    }
  }
}
```
- DI wiring (high level):
  - Register `ICacheService` to memory or distributed implementation based on config.
  - Add `IDistributedCache` via `AddStackExchangeRedisCache` when Provider=Redis.
  - Add hosted `CacheInvalidationSubscriber` when Provider=Redis.
  - Register `CachingBehaviour<,>` in Application.
## Minimal Developer Ergonomics
- For a cacheable query handler, implement `ICacheableQuery<T>`:
```csharp
public sealed record GetRestaurantPublicInfoQuery(Guid RestaurantId)
  : IRequest<Result<RestaurantPublicInfoDto>>, ICacheableQuery<Result<RestaurantPublicInfoDto>>
{
    public string CacheKey => CacheKeys.RestaurantPublicInfo(RestaurantId);
    public CachePolicy Policy => CachePolicies.RestaurantPublicInfo;
}
```
- Or, expose key/policy from handler via helper if we prefer not to mark the request type.
- Utilities: `CacheKeys` static helper class with consistent namespacing/versioning.

## HTTP Caching Additions (Web)
- Keep current ETag/304 flow for FullMenuView (already implemented in `src/Web/Infrastructure/Http/HttpCaching.cs` and `/restaurants/{id}/menu`).
- Consider `OutputCache` for:
  - `/restaurants/search` with vary-by `q, cuisine, lat, lng, radiusKm, pageNumber, pageSize`; TTL 30–60s.
  - `/restaurants/{id}/info` with vary-by route; TTL 120–300s.
## Observability & Ops
- Emit structured logs for cache operations (hit/miss, set, evict, invalidate message, duration).
- Metrics counters/gauges:
  - `cache_hits_total`, `cache_misses_total`, `cache_evictions_total`, `cache_invalidate_msgs_total`.
  - Per-keyspace stats (e.g., `menu:*`, `search:*`).
- Traces: wrap cache calls with Activity tags (keyspace, hit/miss) so they appear in Application Insights.

## Failure Modes & Safety
- Fail-open: if cache is unavailable or errors, proceed to source and log a warning.
- Reasonable TTLs; avoid long-lived wrongness.
- Size guardrails: avoid caching extremely large payloads (log and skip if above threshold, except approved doc types like FullMenuView JSON).
- No PII secrets in cache; if any sensitive payload is cached, encrypt at rest (Redis enterprise features or app-layer crypto). Default: do not cache sensitive per-user data.

## Rollout Plan
1) Phase A (Local):
   - Introduce abstractions and MemoryCache implementation; add CachingBehaviour; unit tests.
   - Mark low-risk queries (`GetRestaurantPublicInfo`, `ReviewSummary`) as cacheable.
2) Phase B (Non-Prod):
   - Wire Redis `IDistributedCache`; enable invalidation pub/sub; soak test under load.
   - Start tagging FullMenuView (still also HTTP cached).
3) Phase C (Prod Canary):
   - Enable for selected endpoints; monitor hit/miss and DB load.
4) Phase D:
   - Adopt for search pages with small TTL; add refresh-ahead if necessary.
5) TeamCart:
   - Implement write-through real-time VM in Redis and plug the `GetTeamCartRealTimeViewModelQuery` to read only from Redis.

## Event → Invalidation Mapping (Examples)
- Menu/MenuItem/MenuCategory/Customization events → `tags=[restaurant:{id}:menu]`
- Restaurant updated events → `tags=[restaurant:{id}:public-info]`
- Review created/updated/deleted → recompute summary → `tags=[restaurant:{id}:reviews]`
- Order lifecycle events → `tags=[order:{id}:status]`
- Search index maintainer UPSERT/soft-delete → optional `tags=[search:*]` (or TTL-only)
- TeamCart mutations → write-through VM; on `Converted|Expired` → remove key `teamcart:vm:{id}`

## Code Skeletons (Illustrative)
```csharp
// Application: marker interface
public interface ICacheableQuery<TResponse>
{
    string CacheKey { get; }
    CachePolicy Policy { get; }
}

// Application: pipeline
public sealed class CachingBehaviour<TRequest,TResponse> : IPipelineBehavior<TRequest,TResponse>
    where TRequest : notnull
{
    private readonly ICacheService _cache;
    private readonly ILogger<CachingBehaviour<TRequest,TResponse>> _logger;
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is not ICacheableQuery<TResponse> cq) return await next();
        var cached = await _cache.GetAsync<TResponse>(cq.CacheKey, ct);
        if (cached is not null) return cached;
        var response = await next();
        await _cache.SetAsync(cq.CacheKey, response, cq.Policy, ct);
        return response;
    }
}

// Infrastructure: DI (high level)
builder.Services.AddStackExchangeRedisCache(o => {
    o.Configuration = cfg.GetConnectionString("Redis") ?? cfg["Cache:Redis:ConnectionString"]; 
    o.InstanceName = cfg["Cache:Redis:InstanceName"]; 
});
builder.Services.AddSingleton<ICacheService, DistributedCacheService>();
builder.Services.AddHostedService<CacheInvalidationSubscriber>();
```

## Testing Strategy
- Unit tests: key generation, policies, pipeline behavior (hit/miss), invalidation mapping from events.
- Integration tests: with Redis (container) to validate pub/sub invalidation and cross-node behavior; fall back to Memory in CI if needed.
- Contract tests: ensure response shapes unchanged; verify headers for HTTP-cached endpoints.

## References & Best Practices
- Prefer `IDistributedCache` for multi-instance scenarios; `IMemoryCache` for local/micro-caches.
- Use short TTLs for volatile or computed queries; event-driven invalidation when writes are well-scoped.
- Avoid caching DB connections or EF contexts. Cache data, not infrastructure.
- Keep keys stable and versioned. Bump `vN` on shape changes rather than mass-deleting ad-hoc.

---

Summary: introduce a light, opt-in caching layer through a MediatR behavior and a Redis-backed cache with pub/sub invalidation, aligned with our CQRS/read-model approach. Start with low-risk read endpoints, keep HTTP caching for large blobs like FullMenuView, and implement write-through Redis for TeamCart real-time views.
