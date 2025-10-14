# Cloudinary Media Service — Implementation Plan

Status: Proposed (October 13, 2025)
Owners: Infrastructure team, Web/API team
Related: Docs/Architecture/YummyZoom_Project_Documentation.md, Docs/Future-Plans/Data_Seeding_Solution.md, Docs/Future-Plans/Restaurant_Bundle_Seeding_Migration_Plan.md, Docs/Development-Guidelines/Authoring_Restaurant_Bundles.md

## 1) Background & Goals

YummyZoom currently stores image links (restaurant logos/backgrounds and menu item images) as raw URLs in the Domain. We need a robust media solution to upload, manage, and deliver images efficiently while keeping the Domain clean and vendor-agnostic.

Goals
- Provide server-side media uploads and URL generation with sane, cacheable transformations.
- Optionally enable secure client-direct uploads with signed parameters.
- Keep Domain unchanged (store URLs only) while enabling lifecycle management in Infrastructure.
- Fit Clean Architecture conventions (Interfaces in Application, implementation in Infrastructure, DI/Options-based config).

Non-Goals (initially)
- Video, audio, and large-file workflows beyond simple images.
- Content moderation, AV scanning, or auto-tagging (can be future enhancements).
- Reworking Domain aggregates to store vendor IDs.

## 2) Architecture Overview

Clean Architecture Placement
- Application: new `IMediaStorageService` abstraction + DTOs.
- Infrastructure: Cloudinary implementation, options binding, DI registration, and optional fake/no-op for tests.
- Web: minimal endpoints for uploads (restaurant branding and menu items) and client-direct signing.
- Domain: unchanged; continues to store string URLs for Logo/Background/MenuItem.ImageUrl.

Key Abstractions (Application)
- `IMediaStorageService`
  - `Task<Result<MediaUploadResult>> UploadAsync(MediaUploadRequest req, CancellationToken ct = default)`
  - `Task<Result<MediaDeleteResult>> DeleteAsync(string publicId, CancellationToken ct = default)`
  - `Result<string> BuildUrl(string publicId, MediaTransform? transform = null)`
  - `Result<ClientDirectUploadParams> CreateClientDirectUpload(string folder, TimeSpan ttl, IReadOnlyDictionary<string,string>? context = null)`
- DTOs (under `Application/Common/Models/Media/`)
  - `MediaUploadRequest` (Stream/FileName/ContentType/Folder/Tags/Overwrite/PublicIdHint)
  - `MediaUploadResult` (PublicId, SecureUrl, Width, Height, Bytes, Format, Version)
  - `MediaDeleteResult` (PublicId, Deleted)
  - `MediaTransform` (Width, Height, CropMode, Gravity, QualityAuto, FormatAuto, DPRAuto)
  - `ClientDirectUploadParams` (CloudName, ApiKey, Timestamp, Signature, Folder, PublicId, UploadUrl)

Infrastructure (Cloudinary)
- `CloudinaryOptions` (Section: `Cloudinary`)
  - `CloudName`, `ApiKey`, `ApiSecret`, `DefaultFolder` (e.g., `yummyzoom`), `Secure` (true), optional `PrivateCdn`, `CdnSubdomain`.
- `CloudinaryMediaService : IMediaStorageService` (uses `CloudinaryDotNet`)
  - Maps our `MediaTransform` to Cloudinary `Transformation`.
  - Uploads with `use_filename`, `unique_filename=true`, `overwrite=false` (branding can opt-in overwrite with stable publicId).
  - Deletes by `public_id`.
  - Builds secure delivery URLs with `q_auto`, `f_auto`, `dpr_auto`.
  - Generates signed params for client-direct uploads (time-limited).

Foldering & Naming
- Base: `yummyzoom/{env}/` where `{env}` ∈ Development|Staging|Production.
- Branding: `yummyzoom/{env}/restaurants/{restaurantId}/branding/{logo|background}` (stable `public_id` -> safe overwrite, avoids orphans).
- Menu items: `yummyzoom/{env}/restaurants/{restaurantId}/items/{menuItemId}` (unique filename).

URL Transform Presets
- `thumb`: w=200, h=200, crop=fill, gravity=auto, q=auto, f=auto, dpr=auto.
- `card`: w=600, h=400, crop=fill, gravity=auto, q=auto, f=auto, dpr=auto.
- `hero`: w=1200, h=800, crop=fill, gravity=auto, q=auto, f=auto, dpr=auto.
- Expose via `MediaTransformPresets` helper or constants under Web/UI.

Security & Validation
- Accept only image content types (`image/jpeg`, `image/png`, `image/webp`); size ≤ 10 MB.
- Strip EXIF by default; do not expose secrets; sign only expected client-direct params.
- Secrets in Azure Key Vault; bind with Options; never log secrets.

Domain Remains Unchanged; MediaLink Later (Optional)
- We will not store vendor IDs in aggregates. For lifecycle ops (delete/replace), introduce a later `MediaLink` mapping table managed in Infrastructure to tie `(ResourceType, ResourceId, Field)` to `PublicId`.

## 3) User Flows & Endpoints (Initial)

Server-Side Upload (Branding)
1. POST `/api/v1/restaurants/{restaurantId}/media/logo` (multipart/form-data)
   - Validates image; uploads to stable `public_id` `.../branding/logo` with `overwrite=true`.
   - Updates `Restaurant.LogoUrl` via application command; returns `{ url, publicId }`.
2. POST `/api/v1/restaurants/{restaurantId}/media/background`: same semantics with `public_id` `.../branding/background`.

Server-Side Upload (Menu Item Image)
1. POST `/api/v1/restaurants/{restaurantId}/menu-items/{menuItemId}/image` (multipart/form-data)
   - Uploads to `.../items/{menuItemId}`; returns `{ url, publicId }`.
   - Calls existing UpdateMenuItemDetails command to set `ImageUrl`.

Client-Direct Upload (Optional Phase)
1. GET `/api/v1/restaurants/{restaurantId}/media/direct-upload-params?scope=item&menuItemId=...`
   - Returns signed params for a temp folder (e.g., `.../temp`).
2. Client uploads directly to Cloudinary; gets `public_id`.
3. POST `/api/v1/restaurants/{restaurantId}/menu-items/{menuItemId}/image/finalize` with `publicId`
   - Optionally rename/move to canonical path; update `ImageUrl`.

Delete/Replace
- Branding: safe overwrite by stable `public_id`.
- Menu items: immediate replace sets new URL; later, `MediaLink` enables deletion of old `public_id` via outbox.

Seeding & Bundles
- Bundles may continue to use placeholder/external URLs.
- Optionally add a helper script later to rewrite placeholders to Cloudinary-based URLs for demo environments.

## 4) Configuration & Environments

AppSettings & Key Vault
- `appsettings.*.json`:
  ```json
  {
    "Cloudinary": {
      "CloudName": "<cloud>",
      "ApiKey": "<key>",
      "DefaultFolder": "yummyzoom",
      "Secure": true
    },
    "Features": {
      "Cloudinary": true
    }
  }
  ```
- Store `ApiSecret` (and optionally `ApiKey`) in Azure Key Vault; builder already supports Key Vault.

Feature Flag
- `Features:Cloudinary` gates registration of the real service; otherwise use `FakeMediaStorageService` for local/test.

Local Dev
- Default to fake implementation; enable real Cloudinary with developer-specific overrides.

## 5) Phased Delivery

Phase 1 — Foundations (Interfaces & Fake) [1–2 days]
- Add Application interfaces & DTOs.
- Implement `FakeMediaStorageService` returning deterministic example URLs.
- Register in DI by default; add feature flag plumbing.
- Unit tests for interface surface.

Acceptance Criteria
- Build green; tests cover basic upload URL flow; no Cloudinary dependency.

Phase 2 — Cloudinary Integration (Server-Side) [2–3 days]
- Add `CloudinaryDotNet` package to Infrastructure.
- Implement `CloudinaryMediaService` and `CloudinaryOptions`.
- DI: bind options; choose implementation via feature flag.
- Foldering & transformation mapping implemented.

Acceptance Criteria
- Manual upload to Cloudinary works; returns secure URL; transformations build correctly.
- Secrets resolved from Key Vault/appsettings.

Phase 3 — Web Endpoints (Branding & Menu Items) [2 days]
- Add minimal endpoints under Restaurants group for logo/background and menu item images.
- Application handlers update Domain (URL only) using existing commands.
- Validation for content type/size; error mapping to Result.

Acceptance Criteria
- Uploads succeed; restaurant/menu item reflects new URL; 4xx/5xx paths covered.

Phase 4 — Client-Direct Upload (Optional) [2–3 days]
- Implement `CreateClientDirectUpload` + endpoints for signed params and finalize.
- Docs for frontend usage; TTL-limited signatures.

Acceptance Criteria
- Client-direct flow proven with a sample; URLs finalized and stored.

Phase 5 — MediaLink Mapping & Cleanup (Optional but Recommended) [3–5 days]
- Add Infrastructure-owned EF entity/table `media_links` mapping `(ResourceType, ResourceId, Field)` → `PublicId`.
- On upload/finalize, upsert mapping and store `LastSeenUrl`.
- Outbox subscribers react to `MenuItemDeleted`, `RestaurantDeleted`, and branding changes to delete prior `public_id`s.
- Backfill script to parse existing Cloudinary URLs and seed `media_links`.

Acceptance Criteria
- Deleting or replacing entities cleans up remote assets; repeated runs are idempotent.

Phase 6 — Observability & Operations [1–2 days]
- Structured logging (upload/delete events, public_id, folder, bytes).
- Safe metrics (success/failure counts, latency) via existing logging.
- Runbook for rotating credentials and disabling feature flag.

## 6) Implementation Notes (Code-Level)

Projects & Files
- Application
  - `src/Application/Common/Interfaces/IServices/IMediaStorageService.cs`
  - `src/Application/Common/Models/Media/*.cs`
- Infrastructure
  - `src/Infrastructure/Media/Cloudinary/CloudinaryOptions.cs`
  - `src/Infrastructure/Media/Cloudinary/CloudinaryMediaService.cs`
  - `src/Infrastructure/Media/Fakes/FakeMediaStorageService.cs`
  - DI: `src/Infrastructure/DependencyInjection.cs` → in `AddExternalServices()` register:
    - `builder.Services.Configure<CloudinaryOptions>(builder.Configuration.GetSection("Cloudinary"));`
    - Conditional `AddScoped<IMediaStorageService, CloudinaryMediaService>()` when enabled, else Fake.
- Web
  - Endpoints in `src/Web/Endpoints/Restaurants.cs` (media subroutes) or a new `Media.cs` group.

Validation
- Enforce content type/size in Web layer before calling service.
- Map Cloudinary exceptions to `Error.Validation/Failure/Problem` consistently.

Security
- Secrets: `ApiSecret` only in Key Vault; log only non-sensitive metadata.
- Client-direct: sign only expected parameters; limit TTL (e.g., 5 minutes).

Performance & Caching
- Prefer dynamic transforms with `q_auto,f_auto,dpr_auto`; rely on CDN caching.
- For frequently used hero/thumbnail sizes, keep transformation presets stable.

Seeding Impact
- Keep bundle JSONs as-is; no change required. Optionally, later convert placeholders to Cloudinary URLs in demo environments.

Docs & DX
- Add a short guide under `Docs/Development-Guidelines/` for using the upload endpoints, presets, and local dev setup.

## 7) Risks & Mitigations

Orphaned Assets (without MediaLink)
- Mitigate with stable `public_id` overwrite for branding; later add `MediaLink` + outbox cleanup.

Secrets Exposure
- Store in Key Vault; never log secrets; restrict access roles.

Vendor Lock-In
- Contained via `IMediaStorageService`; future providers can implement the same interface.

Rate Limits / Costs
- Use `q_auto,f_auto` to optimize bandwidth; keep thumbnails reasonable; consider lazy-loading in UI.

Client-Direct Misuse
- TTL-bound signatures; minimal allowed params; server-side finalize validation.

## 8) Rollback Plan
- Feature flag `Features:Cloudinary=false` switches to Fake service instantly.
- Endpoints remain but return deterministic example URLs (affects only media freshness, not core flows).
- No schema changes to Domain, so rollback is low risk.

## 9) Acceptance Checklist

Phase 1
- Interface + DTOs exist; Fake service wired; unit tests passing.

Phase 2
- Real Cloudinary uploads work in Dev/Staging with Key Vault secrets.
- URLs serve with correct transforms.

Phase 3
- Branding and menu item upload endpoints deployed; end-to-end update of URLs verified.

Phase 4 (Optional)
- Client-direct upload + finalize flow documented and tested.

Phase 5 (Optional)
- `media_links` table present; old assets deleted on replace/delete; backfill executed safely.

Ops
- Runbook for enabling/disabling Cloudinary; credential rotation steps.

## 10) Future Enhancements
- Video (short clips) with poster image generation.
- Moderation/AV scanning and auto-tagging.
- Webhook handling for upload status and derived transformation completion.
- Admin UI for media browsing and manual cleanup.

