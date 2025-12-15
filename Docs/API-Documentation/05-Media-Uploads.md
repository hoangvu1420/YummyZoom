# Media Uploads API

## Overview
Server-hosted image uploads backed by Cloudinary (or a fake store in dev/test). Supports restaurant-scoped uploads and pre-restaurant registration uploads. All responses include hosted URL metadata.

## Authentication & Authorization
- Auth required for all routes.
- Restaurant scopes: policy `MustBeRestaurantStaff` for the specified restaurant.
- Registration scope: authenticated user; no restaurant permission required.
- Idempotency: `Idempotency-Key` header **required** (UUID v4). Successful responses are cached for 24h per key + user + scope + correlation.

## Content Rules
- Multipart/form-data file field: `file`.
- Allowed types: `image/jpeg`, `image/png`, `image/webp`.
- Max size: 10 MB.
- Rejects empty files; returns 400/413/415 accordingly.

## Endpoints
### 1) General upload
`POST /api/v1/media/uploads`

**Form fields & rules**
- `file` (required): image file.
- `scope` (optional): defaults to `menu-item`. Supported: `restaurant-registration`, `restaurant-logo`, `restaurant-background`, `menu-item`, `misc`.
- `restaurantId`:
  - Required for restaurant scopes (`restaurant-logo|restaurant-background|menu-item|misc`).
  - Ignored for `restaurant-registration`.
- `menuItemId`:
  - Optional.
  - If `scope=menu-item` **and** `menuItemId` is provided → stored under the menu item with deterministic publicId.
  - If `scope=menu-item` **and** `menuItemId` is missing → you **must** provide `correlationId`; stored under pending path.
- `correlationId` (GUID):
  - Required for `restaurant-registration`.
  - Required for `scope=menu-item` when `menuItemId` is missing (pre-create uploads).
  - Optional otherwise; used for idempotency/foldering tags.
- `Idempotency-Key` header: required (UUID v4) for all uploads.

**Responses**
- `200 OK` with body:
```json
{
  "publicId": "string",
  "secureUrl": "https://...",
  "width": 0,
  "height": 0,
  "bytes": 0,
  "format": "jpg"
}
```
- Errors: `400` (missing/invalid fields), `401/403` (authz), `413` (too large), `415` (type), `500` (storage failure).

**Behavior & routing**
- `restaurant-logo` → `env/restaurants/{restaurantId}/branding` publicId=`logo`, overwrite=true.
- `restaurant-background` → `env/restaurants/{restaurantId}/branding` publicId=`background`, overwrite=true.
- `menu-item`:
  - With `menuItemId` → `env/restaurants/{restaurantId}/items/{menuItemId}`, publicId=`{menuItemId}`, overwrite=true.
  - Without `menuItemId` + `correlationId` → `env/restaurants/{restaurantId}/items/pending/{correlationId}`, publicId=`{correlationId}`, overwrite=true (pre-create flow).
- `restaurant-registration` → `env/registrations/{correlationId}`, overwrite=true.
- `misc` → `env/restaurants/{restaurantId}/misc`, publicId auto unless provided.

### 2) Convenience restaurant routes
All require `MustBeRestaurantStaff` and an existing restaurant. For menu-item image convenience, the menu item must already exist.

1. `POST /api/v1/restaurants/{restaurantId}/media/logo`
   - Form: `file`
   - Scope: `restaurant-logo`

2. `POST /api/v1/restaurants/{restaurantId}/media/background`
   - Form: `file`
   - Scope: `restaurant-background`

3. `POST /api/v1/restaurants/{restaurantId}/menu-items/{menuItemId}/image`
   - Form: `file`
   - Scope: `menu-item`

## Usage Patterns
- **Restaurant onboarding (no restaurant yet)**
  1) Client generates `correlationId` (GUID) and `Idempotency-Key`.
  2) Upload image with `scope=restaurant-registration`, `correlationId` set; get `secureUrl/publicId`.
  3) Submit registration payload including the returned `secureUrl` (and optionally `publicId`).

- **Menu item pre-create upload (id not yet created)**
  1) Client generates `correlationId` (GUID) and `Idempotency-Key`.
  2) Upload via `/api/v1/media/uploads` with `scope=menu-item`, set `restaurantId`, omit `menuItemId`, include `correlationId` (stored under pending path).
  3) Use returned `secureUrl` when calling `CreateMenuItem`; optionally overwrite later with the real `menuItemId`.

- **Restaurant branding update**
  1) Upload via logo/background endpoint (or general with scope and restaurantId).
  2) Use returned `secureUrl` in `UpdateRestaurantProfile` request.

- **Menu item image (existing item)**
  1) Upload via menu-item endpoint (or general with scope + ids).
  2) Use returned `secureUrl` in `UpdateMenuItemDetails` request.

## Idempotency Details
- Header: `Idempotency-Key: <uuidv4>`.
- Cache key includes userId + scope + restaurantId/menuItemId + correlationId (if provided).
- First successful upload result is replayed for duplicate keys for 24h.

## Error Examples
- Missing restaurant for scoped upload: `400 Media.RestaurantIdRequired`.
- Missing correlation for registration: `400 Media.CorrelationRequired`.
- Invalid idempotency key: `400 IdempotencyKey.InvalidFormat`.
