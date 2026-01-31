# Vendor Decoupling (ACL) Plan

This doc captures an architecture smell-test pass for vendor coupling (Stripe, Cloudinary, Firebase), and a concrete plan to reduce blast radius by introducing explicit Anti-Corruption Layer (ACL) boundaries.

## Why This Matters

The current implementation works, but vendor concepts and IDs leak across layers. That makes changes like “add another payment provider” or “swap media storage” expensive and risky, and it increases the chance a small billing change touches many files.

## Smell Test Findings (Current State)

### 1) Vendor SDK imported outside one boundary folder

- `Stripe.net` is referenced by `src/Application/Application.csproj`.
  - Even if Application code does not currently `using Stripe;`, the project reference itself is a coupling leak.
- Vendor SDK packages also exist in Infrastructure as expected:
  - `Stripe.net` in `src/Infrastructure/Infrastructure.csproj`
  - `CloudinaryDotNet` in `src/Infrastructure/Infrastructure.csproj`
  - `FirebaseAdmin` in `src/Infrastructure/Infrastructure.csproj`

### 2) Vendor nouns appear in non-boundary layers

Payment (Stripe):

- Web boundary names vendor directly:
  - `src/Web/Endpoints/StripeWebhooks.cs`
- Application use-case and command names vendor directly:
  - `src/Application/Orders/Commands/HandleStripeWebhook/*`
  - `src/Application/TeamCarts/Commands/HandleTeamCartStripeWebhook/*`
- Application service interface includes vendor term in a parameter name:
  - `src/Application/Common/Interfaces/IServices/IPaymentGatewayService.cs` has `stripeSignatureHeader`

Media (Cloudinary):

- API-level result models include a Cloudinary-specific identifier shape:
  - `src/Application/Common/Models/Media/MediaModels.cs` uses `PublicId` / `PublicIdHint`

Notifications (Firebase/FCM):

- Session model includes vendor token name:
  - `src/Application/Common/Models/UserDeviceSession.cs` uses `FcmToken`

### 3) Database schema stores vendor IDs as primary identifiers

- `ProcessedWebhookEvents` uses Stripe event ID (`evt_...`) as the primary key:
  - `src/Application/Common/Models/ProcessedWebhookEvent.cs` (`Id` is “Stripe Event ID”)
  - EF configuration in `src/Infrastructure/Persistence/EfCore/ApplicationDbContext.cs`

This is “vendor ID as primary identifier” and makes multi-provider webhook ingestion harder later.

### 4) API responses include vendor IDs / vendor status codes

- Order initiation response includes `PaymentIntentId` and `ClientSecret`:
  - `src/Application/Orders/Commands/InitiateOrder/InitiateOrderCommand.cs`
- TeamCart member payment initiation response includes `PaymentIntentId` and `ClientSecret`:
  - `src/Application/TeamCarts/Commands/InitiateMemberOnlinePayment/InitiateMemberOnlinePaymentCommand.cs`

### 5) "Simple billing change" touches many files

Given the above leaks, changes to payment/webhook behavior can cascade across:

- Web endpoints
- Application command names/types
- Application DTO contracts
- DB idempotency model
- Infrastructure provider implementation

## Target Design (North Star)

### Principles

- Domain and Application should speak in domain language (payment session, gateway reference, push token), not vendor language (Stripe payment intent, FCM token, Cloudinary public ID).
- Vendor SDKs should be referenced only inside explicit boundary folders in Infrastructure.
- Web should expose stable routes/contracts, but can internally translate to domain-centric models.
- Database should store internal identifiers as primary keys; vendor IDs should be stored as external reference fields with uniqueness constraints.

### Proposed Boundary Folders (ACL)

Recommended vendor integration folders:

- `src/Infrastructure/Integrations/Stripe/*`
- `src/Infrastructure/Integrations/Cloudinary/*`
- `src/Infrastructure/Integrations/Firebase/*`

Hard rule: only these folders may reference vendor SDK namespaces (`Stripe`, `CloudinaryDotNet`, `FirebaseAdmin.*`).

## Plan (No Code Changes Yet)

### Phase 0: Guardrails and inventory

1) Inventory vendor SDK references and vendor noun usage (already partially done in this analysis).
2) Add architecture tests to prevent regressions:
   - Fail if `YummyZoom.Application` or `YummyZoom.Domain` references vendor assemblies.
   - Fail if non-ACL namespaces contain `using Stripe;`, `CloudinaryDotNet`, `FirebaseAdmin`.

### Phase 1: Stop the bleeding (project/package references)

1) Remove `Stripe.net` from `src/Application/Application.csproj`.
2) Keep vendor SDK packages only in `src/Infrastructure/Infrastructure.csproj`.

### Phase 2: Payment API normalization (Application surface)

1) Make `IPaymentGatewayService` vendor-agnostic:

- `ConstructWebhookEvent(string json, string stripeSignatureHeader)`
  - Replace with something like:
    - `ConstructWebhookEvent(string json, IReadOnlyDictionary<string, string> headers)`
    - or `ConstructWebhookEvent(string json, WebhookSignature signature)`

2) Rename `PaymentIntentResult` to a provider-neutral concept:

- Example: `PaymentSessionResult(SessionId, ClientToken)`
  - Stripe maps: `SessionId = pi_...`, `ClientToken = client_secret`

3) Normalize webhook event types:

- Replace raw vendor event strings (e.g. `payment_intent.succeeded`) in Application decision logic with internal categories:
  - Example: `PaymentSucceeded`, `PaymentFailed`, `Unsupported`
  - Keep `RawEventType` for logging/diagnostics only.

### Phase 3: Remove vendor nouns from Web and Application entrypoints

1) Rename commands/handlers away from vendor names:

- `HandleStripeWebhook*` -> `HandlePaymentWebhook*`
- `HandleTeamCartStripeWebhook*` -> `HandleTeamCartPaymentWebhook*`

2) Web endpoint:

- Preferred: introduce `POST /api/webhooks/payments` in a provider-neutral endpoint group.
- Optional compatibility: keep `POST /api/stripe-webhooks` as an alias that forwards to the neutral command.

### Phase 4: Fix webhook idempotency schema

Replace “vendor event id as PK” with provider-neutral idempotency storage:

- New table shape:
  - `Id` (Guid, PK)
  - `Provider` (string)
  - `ProviderEventId` (string)
  - `ProcessedAt` (DateTime)
  - Unique index: `(Provider, ProviderEventId)`

Migration strategy options:

- If historical records matter: migrate existing `ProcessedWebhookEvents.Id` -> `ProviderEventId` with `Provider = "stripe"`.
- If not: recreate table and start fresh (still keep the unique constraint).

### Phase 5: Vendor-neutral API contracts (where feasible)

Payments:

- Prefer:
  - `paymentSessionId` instead of `paymentIntentId`
  - `clientToken` instead of `clientSecret`
- If external API must remain stable short-term:
  - Keep old JSON fields at the boundary (Web DTO), but map internally to neutral names.

Media:

- Replace `PublicId` / `PublicIdHint` with `ObjectKey` / `ObjectKeyHint`.

Notifications:

- Replace `FcmToken` with `PushToken` and add `Provider` only if/when needed.

## Scope Notes / Non-Goals

- This plan does not change payment flow semantics (still webhook-driven source of truth).
- This plan is explicitly about coupling boundaries and naming/contract hygiene.
- Multi-provider support is a likely follow-up, but not required to get the coupling benefits.

## Open Decision

Do we need to keep the public API stable (route + response field names) for now?

- If yes (recommended default): keep `POST /api/stripe-webhooks` and `paymentIntentId/clientSecret` externally, but translate internally to vendor-neutral models.
- If no: rename routes/fields now and update client(s) accordingly.
