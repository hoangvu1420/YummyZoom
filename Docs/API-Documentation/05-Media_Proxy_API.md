# Media Proxy API

Status: Published (October 13, 2025)
Audience: Frontend engineers, QA
Related: Docs/Architecture/YummyZoom_Project_Documentation.md

## Overview

The Media Proxy lets the web/app clients fetch images from third‑party CDNs via YummyZoom so the browser receives permissive CORS headers. This avoids tainted canvas and other cross‑origin restrictions.

Key points
- Endpoint streams bytes back from a remote image URL.
- Always sets `Access-Control-Allow-Origin: *` and `Vary: Origin`.
- Validates and restricts targets (allowlist + basic SSRF checks).
- Adds cache headers and passes through `ETag`/`Last-Modified` when available.

## Endpoint

- Method: `GET`
- Path: `/api/v1/media/proxy`
- Query: `url` (required) — absolute, percent‑encoded HTTPS URL of the image to proxy.

Example
```
/api/v1/media/proxy?url=https%3A%2F%2Fimages.example.com%2Fmenu%2Fpho.webp
```

## Request Parameters

- `url` (string, required)
  - Must be an absolute URL. HTTPS only (HTTP blocked by default).
  - The host must be allowlisted on the backend; otherwise the request is rejected.
  - Encode with `encodeURIComponent` or `URLSearchParams`.

## Response

- Success: `200 OK` with streamed image bytes.
- Headers (on success):
  - `Access-Control-Allow-Origin: *`
  - `Vary: Origin`
  - `Cache-Control: public, max-age=86400` (configurable)
  - `Content-Type: image/*` (copied from upstream)
  - `ETag` (if upstream provided)
  - `Last-Modified` (if upstream provided)

Notes
- MVP does not implement conditional GET (no `304 Not Modified`). Clients still benefit from normal browser caching using `Cache-Control`.

## Rate Limiting

- Policy: `image-proxy-ip` (per‑IP, default 60 requests/minute; subject to change by ops).
- On limit exceeded: `429 Too Many Requests` with a simple text message.

## Error Handling (Problem Details)

The API returns RFC 7807 Problem Details on validation and proxy errors. Typical cases:

- `400 Bad Request`
  - `ImageProxy.InvalidUrl`: Missing or invalid `url` parameter.
- `403 Forbidden`
  - `ImageProxy.ForbiddenHost`: Host is not allowlisted.
  - `ImageProxy.PrivateAddress`: Target resolves to a private/reserved IP.
- `415 Unsupported Media Type`
  - `ImageProxy.UnsupportedType`: Upstream `Content-Type` is not an image.
- `413 Payload Too Large` (may surface as `400 Problem` depending on path)
  - `ImageProxy.TooLarge`: Image exceeds max allowed size (default 5 MB).
- `408/504 Timeout` (mapped to `400 Problem` in MVP)
  - `ImageProxy.Timeout`: Upstream took too long.
- `5xx`
  - `ImageProxy.FetchFailed` / `ImageProxy.UpstreamStatus`: Generic upstream failure.

Example error body
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "ImageProxy.ForbiddenHost",
  "status": 403,
  "detail": "The requested host is not allowlisted."
}
```

## Usage Examples

### HTML `<img>`
```html
<img
  src="/api/v1/media/proxy?url=https%3A%2F%2Fcdn.example.com%2Fitems%2F42.webp"
  crossOrigin="anonymous"
  alt="Menu item" />
```

### React helper
```tsx
const proxy = (u: string) => '/api/v1/media/proxy?url=' + new URLSearchParams({ url: u }).toString();

<img src={proxy(actualUrl)} crossOrigin="anonymous" alt="..." />
```

### Canvas/WebGL (untainted)
```ts
const img = new Image();
img.crossOrigin = 'anonymous';
img.src = '/api/v1/media/proxy?url=' + encodeURIComponent(actualUrl);
await img.decode();
ctx.drawImage(img, 0, 0);
```

### Fetch + Blob
```ts
const res = await fetch('/api/v1/media/proxy?url=' + encodeURIComponent(actualUrl));
if (!res.ok) {
  // Optionally read problem details: await res.json()
}
const blob = await res.blob();
```

### Next.js (Image component)
```tsx
const loader = ({ src }: { src: string }) => '/api/v1/media/proxy?url=' + encodeURIComponent(src);
<Image src={originalUrl} loader={loader} unoptimized alt="..." />
```

## Frontend Guidance

- Prefer direct image URLs for domains that already send permissive CORS headers; use the proxy only when needed.
- Always percent‑encode the full original URL inside the `url` query parameter.
- Add `crossOrigin="anonymous"` when you intend to draw to canvas.
- Avoid hot‑linking large images in tight loops; respect the rate limit.
- Do not include secrets/tokens inside the original `url` — they would flow through the proxy.

## Backend Constraints that Affect Clients

- Allowlist: Only hosts configured by ops in `ImageProxy:AllowedHosts` are permitted. If your source host is blocked, request an allowlist update.
- Size cap: Default 5 MB (`ImageProxy:MaxBytes`). Large originals will be rejected.
- HTTPS required by default (`ImageProxy:AllowHttp` is false).

## Change Log

- 2025‑10‑13: Initial publication of v1 media proxy.

