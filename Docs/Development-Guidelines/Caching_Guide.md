# Caching Guide

This guide explains how caching works in YummyZoom, when and how to apply it to queries, and tips for testing and operating the cache efficiently.

## Overview
- Runtime: Redis is provisioned via .NET Aspire and injected as `ConnectionStrings:Redis`.
- DI: The app registers `IDistributedCache` (Redis) when available and falls back to `IMemoryCache` in other environments.
- Abstractions: The Application layer exposes a small set of cache interfaces and a MediatR pipeline behavior for cache-aside.
- Behavior: Queries opt-in by implementing a marker interface. The `CachingBehaviour` does get/set around handlers.
- Tests: Functional tests boot a Redis Testcontainer to validate distributed cache behavior.

## Architecture
- Cache layers
  - Distributed cache: Redis via `IDistributedCache` (default in web runtime).
  - Memory cache: `IMemoryCache` (fallback; useful for local/micro caches).
- Application layer abstractions
  - `src/Application/Common/Caching/ICacheService.cs`
  - `src/Application/Common/Caching/CachePolicy.cs`
  - `src/Application/Common/Caching/ICacheKeyFactory.cs`
  - `src/Application/Common/Caching/ICacheInvalidationPublisher.cs`
  - `src/Application/Common/Caching/ICacheableQuery.cs`
  - `src/Application/Common/Caching/ICacheSerializer.cs`
- Infrastructure implementations
  - `src/Infrastructure/Caching/DistributedCacheService.cs` (Redis)
  - `src/Infrastructure/Caching/MemoryCacheService.cs` (in-memory)
  - `src/Infrastructure/Caching/JsonCacheSerializer.cs` (UTF8 JSON via DomainJson)
  - `src/Infrastructure/Caching/DefaultCacheKeyFactory.cs` (namespaced keys)
- Pipeline
  - `src/Application/Common/Behaviours/CachingBehaviour.cs` registered in `src/Application/DependencyInjection.cs`

## HTTP Caching Layer
- Purpose: Reduce bandwidth and reprocessing by allowing clients/CDNs to reuse responses safely.
- Mechanisms used in this project:
  - ETag (weak) and Last-Modified validators with conditional GET (304 Not Modified)
  - Cache-Control headers to hint client-side TTLs
- Helpers:
  - `src/Web/Infrastructure/Http/HttpCaching.cs`
    - `BuildWeakEtag(Guid restaurantId, DateTimeOffset lastRebuiltAt)` → `W/"r:{id}:t:{ticks}"`
    - `ToRfc1123(DateTimeOffset)` for `Last-Modified`
    - `MatchesIfNoneMatch(...)`, `NotModifiedSince(...)` for conditional checks
- Example usage:
  - `GET /restaurants/{id}/menu` endpoint sets ETag, Last-Modified, and `Cache-Control: public, max-age=300`, and returns 304 on validator match.
    - See: `src/Web/Endpoints/Restaurants.cs`
- When to use HTTP caching:
  - Large payloads (e.g., Full Menu JSON), public resources, and scenarios where client reuse saves bandwidth.
  - Endpoints with clear last-modified or rebuild timestamps to drive validators.
- How to add HTTP caching to an endpoint:
  1) Compute an ETag input (e.g., resource ID + rebuild timestamp or version).
  2) Use `HttpCaching.BuildWeakEtag` and `ToRfc1123`.
  3) If `MatchesIfNoneMatch` or `NotModifiedSince` → set headers and return 304.
  4) Otherwise set validators + a reasonable `Cache-Control` and return the payload.
- OutputCache:
  - Not currently enabled, but can be considered for short-lived public endpoints (e.g., search) with vary-by parameters.
  - Prefer server-side cache (Redis) for data reuse and HTTP caching for payload reuse at the edge; they are complementary.

## Dependency Injection
- Connection & registration happen in `src/Infrastructure/DependencyInjection.cs`:
  - If `ConnectionStrings:redis` or `Cache:Redis:ConnectionString` exists → `IDistributedCache` (Redis) + `ICacheService=DistributedCacheService`.
  - Else → `IMemoryCache` + `ICacheService=MemoryCacheService`.
  - `ICacheKeyFactory` is always registered; `ICacheSerializer` is used by the distributed service.

## How Cache-Asides Work
- Any request implementing `ICacheableQuery<TResponse>` is intercepted by `CachingBehaviour<TRequest,TResponse>`.
- Flow:
  1) Try `ICacheService.GetAsync<TResponse>(CacheKey)`
  2) On miss, execute handler
  3) On success, `SetAsync(CacheKey, response, Policy)`
  4) Return response
- Fail-open: exceptions during cache get/set are logged and the handler result is returned.

Relation to HTTP caching:
- Server-side caching (Redis/Memory) reduces compute and DB load across requests and instances.
- HTTP caching prevents re-downloading payloads when clients already have a valid copy.
- Use both strategically: server-side caching for data reuse; HTTP validators on large or popular GETs.

## Adopting Caching in a Query
1) Decide if the query is cacheable (see next section).
2) Implement `ICacheableQuery<TResponse>` on the request type.
3) Provide:
   - `CacheKey`: deterministic, versioned key based on the request inputs.
   - `Policy`: TTLs, optional sliding expiration, and logical tags.
4) No handler changes required — `CachingBehaviour` will perform get/set around the handler.

Example (implemented):
- `src/Application/Restaurants/Queries/GetRestaurantPublicInfo/GetRestaurantPublicInfoQuery.cs`
  - Key: `restaurant:public-info:v1:{RestaurantId:N}`
  - Policy: 2m TTL, tag `restaurant:{id}:public-info`

## Should This Query Be Cached?
Prefer caching when most of the following apply:
- Read-heavy and frequently repeated.
- Stable enough that modest staleness (e.g., 30–300s) is acceptable.
- Deterministic by a small set of inputs (e.g., route ID, select query params).
- Not user/identity sensitive, or the sensitivity can be part of the key safely without PII.

Avoid or defer caching when:
- Highly volatile or correctness requires immediate freshness.
- Response is rarely reused or the input space is huge (unbounded keys).
- Contains PII or sensitive data that shouldn’t be stored in cache (default policy is to avoid caching such data).

## Selecting the Cache Layer
- Default: `ICacheService` resolves to Redis in normal runtime. Use this for cross-instance coherence.
- Memory cache: Used automatically only when Redis is not configured, and suitable for ephemeral dev contexts and micro-caches.

HTTP vs Server-side
- HTTP cache: controlled by headers; effective with clients/proxies/CDNs; reduces bandwidth and app work on 304s.
- Server-side cache: independent of clients; improves latency and load even when clients miss HTTP cache.
- Often both are used: e.g., Full Menu uses HTTP validators; public info uses server-side cache.

## Key Conventions
- Format: `namespace:vN:part1:part2:...`
  - `namespace`: feature area (e.g., `restaurant:public-info`)
  - `vN`: bump on response shape/contract changes
  - parts: normalized inputs, lower-cased where applicable, GUIDs as `N` format
- Use `ICacheKeyFactory` (`DefaultCacheKeyFactory`) for consistent normalization if constructing keys dynamically.

## Policy Guidelines
- `CachePolicy.WithTtl(TimeSpan ttl, params string[] tags)` is the common path.
- TTL suggestions:
  - Restaurant public info: 2–5 minutes
  - Review summary: 2–10 minutes
  - Search result pages: 30–60 seconds
  - Order status: 5–15 seconds
- Sliding expiration: prefer absolute TTLs unless you need sliding for smoothing.
- Stale-while-revalidate: reserved for a later phase; don’t enable until supported end-to-end.
- Tags: specify logical groupings for later invalidation bus (e.g., `restaurant:{id}:public-info`).

## Current Adoptions
- Restaurant public info query: cached via `ICacheableQuery` with 2m TTL.
  - `src/Application/Restaurants/Queries/GetRestaurantPublicInfo/GetRestaurantPublicInfoQuery.cs`
- Functional test validating cache hit on second call:
  - `tests/Application.FunctionalTests/Features/Restaurants/Queries/GetRestaurantPublicInfoQueryTests.cs`
- HTTP caching on Full Menu endpoint via ETag + Last-Modified + Cache-Control (returns 304 when applicable):
  - `src/Web/Endpoints/Restaurants.cs` (Get restaurant menu)
  - `src/Web/Infrastructure/Http/HttpCaching.cs`
- Full Menu query: server-side cached with 5m TTL and tag `restaurant:{id}:menu`; invalidated via pub/sub after read-model upsert.
  - `src/Application/Restaurants/Queries/GetFullMenu/GetFullMenuQuery.cs`
  - Invalidation publisher wired in `FullFullMenuViewMaintainer`.
  - Functional test verifies invalidation → fresh data:
    - `tests/Application.FunctionalTests/Features/Restaurants/Queries/GetFullMenuQueryTests.cs`

## Testing
- Unit tests (recommended):
  - Key generation and normalization
  - Behavior hit/miss logic using a stub `ICacheService`
- Functional tests (current pattern):
  - First call populates cache; update DB; second call returns cached value
  - Redis Testcontainer is started in test infrastructure
- Contract tests:
  - Ensure response shape remains unchanged even when cached

## Observability
- Logging: `CachingBehaviour` logs cache hits and set failures at Debug/Warning.
- Metrics (planned):
  - `cache_hits_total`, `cache_misses_total`, `cache_evictions_total`, `cache_invalidate_msgs_total`
- Tracing (optional): add Activity tags for cache operations to show up in telemetry.

## Invalidation Strategy (Roadmap)
- Event-driven invalidation via pub/sub (Redis):
  - `ICacheInvalidationPublisher` and a subscriber will map domain/read-model events to tags/keys
  - Read model maintainers publish invalidations after successful updates
- Tag semantics:
  - Examples: `restaurant:{id}:menu`, `restaurant:{id}:public-info`, `restaurant:{id}:reviews`.

## Invalidation Testing
- Test runtime notes:
  - Functional tests start a Redis Testcontainer and inject `ConnectionStrings:Redis`; the `CacheInvalidationSubscriber` hosted service runs in tests (only unrelated background services are disabled).
  - Because pub/sub is asynchronous, tests poll briefly (<=2s) after publishing invalidations to allow the subscriber to process messages.
- Example patterns:
  - Warm cache by calling the query twice.
  - Trigger a change that updates the read model (e.g., creating a menu item to rebuild FullMenuView).
  - Drain the outbox to ensure maintainers run and publish.
  - Poll and re-query; assert the response reflects the update (e.g., `LastRebuiltAt` advanced).

## Local Development & Operations
- Run locally with Aspire:
  - `dotnet run --project src/AppHost` to bring up Postgres + Redis + Web
- Clearing cache during development:
  - Change the cache key version (`vN`) to invalidate whole keyspaces after shape changes
  - Manual delete can be done via Redis CLI (`DEL key`) or using application endpoints/scripts (if added)
- Safety:
  - Default to fail-open on cache errors
  - Avoid long TTLs for volatile data
  - Don’t cache sensitive user-specific content unless explicitly designed and reviewed

## Common Pitfalls
- Missing versioning in keys → hard to invalidate after contract changes
- Caching errors without monitoring → silent misses/penalties
- Overly granular keys for search → low hit rate and memory bloat
- Caching NotFound or failures inadvertently → consider whether to only cache successful results (behavior can be adjusted if needed)

## Step-by-Step: Caching a New Query
1) Evaluate cache suitability (see section above)
2) Implement `ICacheableQuery<TResponse>` on the request type
3) Define `CacheKey` and `Policy` with TTL and tags
4) Add/adjust functional tests to assert cache hit semantics
5) Monitor logs and, when available, metrics to confirm hit rates

## File Index
- Core
  - `src/Application/Common/Behaviours/CachingBehaviour.cs`
  - `src/Application/DependencyInjection.cs`
- Abstractions
  - `src/Application/Common/Caching/ICacheService.cs`
  - `src/Application/Common/Caching/CachePolicy.cs`
  - `src/Application/Common/Caching/ICacheKeyFactory.cs`
  - `src/Application/Common/Caching/ICacheInvalidationPublisher.cs`
  - `src/Application/Common/Caching/ICacheableQuery.cs`
  - `src/Application/Common/Caching/ICacheSerializer.cs`
- Infrastructure
  - `src/Infrastructure/DependencyInjection.cs`
  - `src/Infrastructure/Caching/DistributedCacheService.cs`
  - `src/Infrastructure/Caching/MemoryCacheService.cs`
  - `src/Infrastructure/Caching/JsonCacheSerializer.cs`
  - `src/Infrastructure/Caching/DefaultCacheKeyFactory.cs`
  - `src/Infrastructure/Caching/RedisInvalidationPublisher.cs`
  - `src/Infrastructure/Caching/CacheInvalidationSubscriber.cs`
  - `src/Infrastructure/Caching/NoOpInvalidationPublisher.cs`
- Functional Tests
  - `tests/Application.FunctionalTests/Infrastructure/Cache/RedisTestcontainer.cs`
  - `tests/Application.FunctionalTests/Infrastructure/TestInfrastructure.cs`
  - `tests/Application.FunctionalTests/Features/Restaurants/Queries/GetRestaurantPublicInfoQueryTests.cs`

---

With these patterns and conventions, developers can safely and consistently introduce caching for read-heavy endpoints, reaping performance gains while preserving correctness and maintainability.
