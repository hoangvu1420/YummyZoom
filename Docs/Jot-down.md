I'll scan the `src\Infrastructure` folder to understand its current organization before proposing an improved structure aligned with Clean Architecture.

[1 tool called]

I scanned the current `src\Infrastructure` layout and noted key areas: EF Core persistence, outbox/messaging, caching, identity, background hosted services, notifications (Firebase), payments (Stripe), realtime notifiers, serialization helpers, and a Redis-backed team cart store.

### Proposed Infrastructure structure (no code changes)
- Infrastructure/
  - DI/
    - DependencyInjection.cs
  - Persistence/
    - EfCore/
      - ApplicationDbContext.cs
      - ApplicationDbContextInitialiser.cs
      - DesignTime/
        - DesignTimeApplicationDbContextFactory.cs
      - Configurations/
        - Common/
          - AuditableEntityConfiguration.cs
          - EfJsonbExtensions.cs
        - AccountTransactionConfiguration.cs
        - CouponConfiguration.cs
        - CouponUserUsageConfiguration.cs
        - CustomizationGroupConfiguration.cs
        - DeviceConfiguration.cs
        - FullMenuViewConfiguration.cs
        - InboxMessageConfiguration.cs
        - MenuCategoryConfiguration.cs
        - MenuConfiguration.cs
        - MenuItemConfiguration.cs
        - OrderConfiguration.cs
        - OutboxMessageConfiguration.cs
        - RestaurantAccountConfiguration.cs
        - RestaurantConfiguration.cs
        - RestaurantReviewSummaryConfiguration.cs
        - ReviewConfiguration.cs
        - RoleAssignmentConfiguration.cs
        - SearchIndexItemConfiguration.cs
        - SupportTicketConfiguration.cs
        - TagConfiguration.cs
        - TeamCartConfiguration.cs
        - TodoListConfiguration.cs
        - UserConfiguration.cs
        - UserDeviceSessionConfiguration.cs
      - Interceptors/
        - AuditableEntityInterceptor.cs
        - ConvertDomainEventsToOutboxInterceptor.cs
        - DispatchDomainEventsInterceptor.cs
        - SoftDeleteInterceptor.cs
      - Extensions/
        - SoftDeleteExtensions.cs
      - Migrations/
        - (all migration files and snapshot)
      - ReadModels/
        - FullMenu/
        - Reviews/
        - Search/
      - Models/
        - InboxMessage.cs
        - OutboxMessage.cs
        - CouponUserUsage.cs
      - Db/
        - DbConnectionFactory.cs
    - Repositories/
      - (all repository implementations)
  - Messaging/
    - Outbox/
      - IOutboxProcessor.cs
      - OutboxProcessor.cs
      - OutboxPublisherHostedService.cs
    - Invalidation/
      - NoOpInvalidationPublisher.cs
      - RedisInvalidationPublisher.cs
      - CacheInvalidationSubscriber.cs
  - Caching/
    - Abstractions/
      - DefaultCacheKeyFactory.cs
    - Distributed/
      - DistributedCacheService.cs
    - Memory/
      - MemoryCacheService.cs
    - Serialization/
      - JsonCacheSerializer.cs
  - Identity/
    - ApplicationUser.cs
    - IdentityResultExtensions.cs
    - IdentityService.cs
    - YummyZoomClaimsPrincipalFactory.cs
  - BackgroundProcessing/
    - HostedServices/
      - TeamCartExpirationHostedService.cs
    - Options/
      - TeamCartExpirationOptions.cs
  - Notifications/
    - Push/
      - Firebase/
        - FirebaseAdminSdkConfig.cs
        - FcmService.cs
  - Payments/
    - Stripe/
      - StripeOptions.cs
      - StripeService.cs
  - Realtime/
    - Notifiers/
      - NoOpOrderRealtimeNotifier.cs
      - NoOpTeamCartRealtimeNotifier.cs
  - Serialization/
    - Converters/
      - AggregateRootIdJsonConverterFactory.cs
    - Json/
      - DomainJson.cs
      - OutboxJson.cs
  - StateStores/
    - TeamCart/
      - RedisTeamCartStore.cs
      - TeamCartStoreOptions.cs
  - GlobalUsings.cs
  - Infrastructure.csproj

### Key improvements
- Persistence/EfCore clearly encapsulates EF concerns: context, configurations, migrations, interceptors, design-time, and read models.
- Messaging separates Outbox and Cache Invalidation concerns from generic Caching.
- BackgroundProcessing groups all hosted services and related options.
- StateStores isolates Redis-backed domain-specific stores (e.g., TeamCart) from generic caching.
- Serialization is centralized for domain/outbox JSON and converters.
- DI isolated under `DI` to signal composition root for this layer.

### File-to-folder mapping highlights
- `Data/*` → `Persistence/EfCore/*` (maintaining subfolders: Configurations, Interceptors, Migrations, ReadModels, Extensions; move `DbConnectionFactory.cs` under `Persistence/EfCore/Db/`).
- `Data/Models/*` → `Persistence/EfCore/Models/*`.
- `Repositories/*` → `Persistence/Repositories/*`.
- `Outbox/*` → `Messaging/Outbox/*`.
- `Caching/*` → split: `Caching/Abstractions/DefaultCacheKeyFactory.cs`, `Caching/Distributed/DistributedCacheService.cs`, `Caching/Memory/MemoryCacheService.cs`, `Caching/Serialization/JsonCacheSerializer.cs`, and move invalidation publishers/subscriber to `Messaging/Invalidation/`.
- `BackgroundServices/*` → `BackgroundProcessing/HostedServices/*`; `TeamCartExpirationOptions.cs` → `BackgroundProcessing/Options/`.
- `TeamCartStore/*` → `StateStores/TeamCart/*`.
- `Serialization/*` → `Serialization/Converters/*` and `Serialization/Json/*`.
- `Realtime/*` → `Realtime/Notifiers/*`.
- `Notifications/Firebase/*` → `Notifications/Push/Firebase/*`.
- `Identity/*` unchanged under `Identity/`.
- `Payments/Stripe/*` unchanged under `Payments/Stripe/`.
- `DependencyInjection.cs` → `DI/DependencyInjection.cs`.

### Notes on Clean Architecture alignment
- Infrastructure-only responsibilities are clearly grouped; no domain logic introduced.
- EF Core persistence is a single cohesive area, making testability and migrations cleaner.
- Messaging concerns are decoupled from caching to reflect different use cases (outbox vs invalidation).
- Hosted services are discoverable in one place with their options classes.
- Domain-specific external state (team cart) is separated from generic caching to avoid conflation.

If you want, I can produce a step-by-step move plan (no code changes) or a PowerShell script to reorganize folders/files when you’re ready.