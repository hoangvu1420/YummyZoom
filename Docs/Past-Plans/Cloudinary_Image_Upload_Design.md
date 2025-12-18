# Cloudinary Image Upload — Design & Plan (v2)

Status: Proposed (Dec 2025)
Owners: Web/API, Infrastructure
Related: `Docs/Future-Plans/Cloudinary_Media_Service_Implementation_Plan.md` (foundational), `Docs/API-Documentation/API-Reference/Restaurant/02-Menu-Management.md`

## 1) Current State (Code Scan)
- Image fields are plain URLs set via existing commands/endpoints:
  - Menu items: `CreateMenuItem`/`UpdateMenuItemDetails` accept `imageUrl` (see `Restaurants.MenuItems.cs`, `Application.MenuItems.Commands.*`).
  - Restaurant branding: `UpdateRestaurantProfile` accepts `logoUrl`; public projections expose `logoUrl` and `backgroundImageUrl` (see `Restaurants.Public.cs`).
  - Read models (`FullMenuViewMaintainer`, `RedisTeamCartStore`) project `ImageUrl` as-is.
- No media storage abstraction or upload endpoint exists; URLs are caller-provided.
- No Cloudinary/third-party media integration yet; no server-side validation for images.

## 2) Goals
- Provide a reliable image upload flow backed by Cloudinary, with minimal coupling to domain models.
- Avoid duplicate uploads on retries; keep uploads scoped to a restaurant and optional item id.
- Keep Domain unchanged (store URL strings); lifecycle/deletion handled in Infrastructure later.
- Offer a single, reusable server-side upload endpoint; optionally add convenience scoped routes.
- Enable FE to fetch upload params and reuse returned URLs in create/update flows.

## 3) Design Overview
- **Abstraction**: Introduce `IMediaStorageService` in Application for upload/delete/url-build; Cloudinary implementation in Infrastructure; Fake implementation for local/tests.
- **Endpoint Shape**: Central upload endpoint `POST /api/v1/media/uploads` (authenticated) taking multipart/form-data (`file`) plus metadata (`restaurantId`, optional `menuItemId`, `scope` = `restaurant-logo|restaurant-background|menu-item`). Returns `{ url, publicId, width, height, bytes, format }`.
  - Convenience wrappers can live under Restaurants group and delegate to the central handler for UX ergonomics, but the core logic is centralized.
- **Idempotency**: Require `Idempotency-Key` header; cache first successful result per (tenant, key). Also allow deterministic `public_id` for branding and `overwrite=true` to make retries safe.
- **Foldering**: `yummyzoom/{env}/restaurants/{restaurantId}/branding/{logo|background}` (stable `public_id`); `.../items/{menuItemId}` (unique filename or overwrite if provided).
- **Validation**: Max size 10 MB; content types `image/jpeg|png|webp`; reject animated formats if not supported; strip EXIF via Cloudinary defaults.
- **Security**: `MustBeRestaurantStaff` policy; enforce restaurant scope on `restaurantId`/`menuItemId`; secrets from Key Vault; do not log secrets or raw uploads.
- **Duplication & Orphans**: Use idempotency + deterministic `public_id` for branding; later add `MediaLink` table + outbox cleanup to delete old assets on replace/delete.
- **FE Integration**: Upload first → get URL/publicId → pass into existing create/update endpoints (`imageUrl` fields). Optional: add client-direct signed upload flow later.

## 4) Components & Changes
### Application
- Interface: `IMediaStorageService` with `UploadAsync(MediaUploadRequest)`, `DeleteAsync(publicId)`, `BuildUrl(...)`.
- Models: `MediaUploadRequest` (Stream, FileName, ContentType, Folder, PublicIdHint, Overwrite, Tags, IdempotencyKey, Scope), `MediaUploadResult` (PublicId, SecureUrl, Width, Height, Bytes, Format).
- Validation helpers for image content type/size (shared by Web layer).

### Infrastructure
- Package: `CloudinaryDotNet` dependency.
- Options: `CloudinaryOptions` (`CloudName`, `ApiKey`, `ApiSecret`, `DefaultFolder`, `Secure`, `PrivateCdn`, `CdnSubdomain`).
- Service: `CloudinaryMediaService : IMediaStorageService` mapping transforms and executing uploads with `use_filename=true`, `unique_filename=true` (or `overwrite=true` when stable `public_id` supplied for branding/idempotent retry).
- Fake: `FakeMediaStorageService` returning deterministic placeholder URLs for local/test.
- DI: In `DependencyInjection.AddExternalServices`, bind options; pick real vs fake via feature flag (`Features:Cloudinary`).

### Web (Endpoints & Handlers)
- New endpoint group (e.g., `MediaUploads`) or add to Restaurants group:
  - `POST /api/v1/media/uploads`
    - Auth: `MustBeRestaurantStaff`
    - Multipart fields: `file`, `restaurantId` (required), `menuItemId` (optional), `scope`
    - Headers: `Idempotency-Key` (required)
    - Logic: validate size/type; derive folder/publicId; call `IMediaStorageService.UploadAsync`; return metadata.
  - Optional convenience routes delegating to the same handler:
    - `POST /api/v1/restaurants/{restaurantId}/media/logo` (`scope=restaurant-logo`, stable publicId, overwrite=true)
    - `POST /api/v1/restaurants/{restaurantId}/menu-items/{menuItemId}/image` (`scope=menu-item`)
- Command integration: After upload, FE calls existing `UpdateRestaurantProfile` or `UpdateMenuItemDetails` with the returned `url` to persist the image.

### Frontend Integration
- Flow: select file → call upload endpoint with `Idempotency-Key` → receive `{ url, publicId }` → pass `url` into `CreateMenuItem`/`UpdateMenuItemDetails`/`UpdateRestaurantProfile` as today.
- Retry-safe: reuse the same `Idempotency-Key` to avoid duplicate uploads.
- Display: use returned `url` (already `https`); optionally request transformed variants via Cloudinary if we expose a helper.

### Observability & Ops
- Log upload attempts with correlation id, restaurantId, scope, size (no secrets or file contents).
- Metrics: success/failure counts, latency, rejection reasons (type/size), Cloudinary error codes.
- Runbook: enable/disable via feature flag; rotate `ApiSecret` via Key Vault; fallback to Fake service if Cloudinary unavailable.

## 5) Idempotency & Duplication Strategy
- Server enforces `Idempotency-Key` (UUID) + tenant context; stores the first successful result for a short TTL (e.g., 24h) in cache (Redis) to serve retries.
- Branding uploads use stable `public_id` + `overwrite=true` to keep a single asset per logo/background; retries overwrite instead of duplicating.
- Optional content-hash short-circuit for additional safety when the same bytes are seen within a window.

## 6) Data Model & Cleanup (Later Phase)
- Add `media_links` table (Infrastructure-owned) to map `(ResourceType, ResourceId, Field, PublicId, LastSeenUrl, UpdatedAt)`.
- On upload/finalize, upsert mapping. On replace/delete events (`MenuItemDeleted`, `RestaurantDeleted`, branding change), enqueue delete to Cloudinary via outbox consumer.
- Background cleanup job to delete orphaned temp uploads (not finalized within TTL).

## 7) Delivery Plan (Iterations)
1) **Foundation**: Interfaces + Fake service + feature flag; unit tests.
2) **Cloudinary Integration**: Real service, options binding, DI; manual E2E sanity.
3) **Upload Endpoint**: Central `POST /media/uploads` + validations + idempotency cache; docs for FE flow.
4) **Convenience Routes**: Logo/background/menu-item image wrappers reusing central logic; update API docs.
5) **Cleanup & MediaLink (optional)**: Table + outbox consumer + orphan cleanup.
6) **Client-Direct (optional)**: Signed params & finalize endpoint if needed for mobile/web direct uploads.

## 8) Open Questions
- Max file size and allowed formats (default: 10 MB, jpeg/png/webp, no animated).
- Should we force a canonical transformation (e.g., `f_auto,q_auto,dpr_auto`) in returned URL, or return original and let FE request variants?
- How long should idempotency cache live, and should we persist it in DB vs Redis?
- Deletion policy for replaced menu item images (immediate delete vs keep last N versions).

## 9) Acceptance Criteria (Initial Phases)
- Upload endpoint returns URL/publicId with validation and idempotency; rejects bad types/sizes.
- Feature flag off: fake URLs; on: Cloudinary upload works in dev/staging with Key Vault secrets.
- Existing create/update flows can consume returned URL with no schema changes.
- Logs/metrics present for uploads; no secrets leaked.
