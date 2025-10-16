# Image Proxy Service — Implementation Plan

Status: Proposed (October 13, 2025)
Owners: Web/API team, Infrastructure team
Related: Docs/Architecture/YummyZoom_Project_Documentation.md, Docs/Future-Plans/Cloudinary_Media_Service_Implementation_Plan.md, Docs/Development-Guidelines/WebApi_Contract_Tests_Guidelines.md, Docs/Development-Guidelines/Rate_Limiting_Implementation_Guide.md

## 1) Background & Goals

Problem
- Frontend needs to display and sometimes process images (e.g., draw to canvas) that live on foreign domains. Many sources do not include permissive CORS headers, causing browser blocks (tainted canvas, fetch/image decode restrictions).

Goal
- Provide a server-side image proxy endpoint that fetches remote images and returns them from our domain with explicit CORS headers (Access-Control-Allow-Origin: *). The response should preserve content-type, stream efficiently, and follow YummyZoom’s Clean Architecture and operational standards.

Non-Goals (initial)
- Full media management (covered by Cloudinary plan).
- HTML page or arbitrary file proxying; scope is images only.

## 2) Architecture Placement (Clean Architecture)

- Web (Presentation): Minimal API group `Media` exposing `GET /api/v{version}/media/proxy`.
- Application: Define `IImageProxyService` abstraction and `ImageProxyOptions` (config binding) under `Application` to keep Web thin and testable.
- Infrastructure: Implement `HttpImageProxyService` using `IHttpClientFactory` with resilience; SSRF and content validation; optional caching.
- Domain: No changes.
- SharedKernel: Reuse `Result` for error mapping where appropriate (non-streaming code paths).

## 3) Public API Design

Endpoint
- `GET /api/v1/media/proxy?url={encodedUrl}[&sig=...&exp=...]`
- Optional HEAD support later: `HEAD /api/v1/media/proxy?url=...` for metadata-only checks.

Request
- Query `url` (required): absolute HTTPS URL to the target image.
- Query `sig`, `exp` (optional): HMAC signature and unix expiry to allow proxying non-allowlisted hosts safely (see Security).

Response
- 200 OK with streamed image bytes.
- Headers (always on success):
  - `Access-Control-Allow-Origin: *`
  - `Vary: Origin`
  - `Cache-Control: public, max-age={config}` (default 86400)
  - `Content-Type: {upstream content-type}` (fallback to `image/jpeg` if unknown and allowed)
  - Pass through `ETag`/`Last-Modified` when available.
- 4xx/5xx mapped via `CustomResults` for validation/denied scenarios.

OpenAPI
- Add summary/description and example.

Rate Limiting
- Apply `RequireRateLimiting("image-proxy-ip")` policy (per-IP) with modest defaults (e.g., 60/min) configurable via `RateLimitingOptions` or local options.

## 4) Options & Configuration

`ImageProxyOptions` (Application/Common/Configuration)
- `SectionName = "ImageProxy"`
- `AllowedHosts: string[]` — allowlist of hostnames or domain suffixes (e.g., ["images.example.com", ".fbcdn.net", ".cdninstagram.com"]).
- `RequireSignatureForNonAllowlisted: bool = true`
- `BlockPrivateNetworks: bool = true` — disallow RFC1918, loopback, link-local, metadata IPs.
- `MaxBytes: int = 5_000_000` — hard cap; abort if exceeded.
- `TimeoutSeconds: int = 10`
- `ConnectTimeoutSeconds: int = 5`
- `CacheSeconds: int = 86400` — client/proxy cache header; server-side caching optional.
- `AllowedContentTypes: string[] = ["image/jpeg","image/png","image/webp","image/gif"]`
- `UserAgent: string = "YummyZoom-ImageProxy/1.0"`
- `EnableServerCache: bool = false`
- `ServerCacheSeconds: int = 3600`
- `MaxRedirects: int = 3` (follow only same-scheme HTTPS)
- `EnableSignature: bool = true`
- `SignatureSecret: string?` (Key Vault)
- `SignatureHash: "SHA256"`

## 5) Security Model

SSRF & Abuse Protections
- Parse and validate URL: must be absolute HTTPS; no data/file/javascript schemes; enforce default ports (443) or allow 80 only if `AllowHttp` explicitly enabled (default false).
- DNS/IP validation: resolve hostname; reject targets in private/reserved ranges (10/8, 172.16/12, 192.168/16, 127/8, ::1, link-local, metadata 169.254/16, GCP/AWS metadata IPs).
- Allowlist: if host matches `AllowedHosts` (exact or suffix match), permit without signature.
- Signature mode: if not allowlisted, require `sig` + `exp`.
  - Compute `baseString = $"GET\n{url}\n{exp}"` and verify `HMAC(secret, baseString)` equals `sig` (hex/base64), and `exp` in future.
  - Provide a server-side helper (internal only) for generating signed URLs when needed.
- Content-type enforcement: only image types per options. Reject others with 415.
- Size limits: use `ResponseHeadersRead` and stop streaming if cumulative bytes exceed `MaxBytes`.
- Redirects: limit to `MaxRedirects`; preserve HTTPS; re-run validations post-redirect.
- Strip cookies/auth; send only `User-Agent` and `Accept: image/*`.
- Observability: structured logs for allowlist hit/miss, validation failures, upstream status/timeouts; never log full query strings containing secrets/tokens.

Rate Limiting
- Per-IP policy and optional per-host throttling to avoid hot-link amplification (future).

## 6) Resilience & Performance

HttpClient
- Register named client `ImageProxy` with `SocketsHttpHandler`:
  - `AutomaticDecompression = GZip | Deflate | Brotli`
  - `AllowAutoRedirect = false` (handle redirects manually to re-validate)
  - `ConnectTimeout = ConnectTimeoutSeconds`
- Add `AddStandardResilienceHandler()` (Microsoft.Extensions.Http.Resilience):
  - Short overall timeout (TimeoutSeconds)
  - Transient retry (small: 1–2 tries) on 408/5xx/connect failures, no retry on 4xx.

Streaming
- Use `HttpCompletionOption.ResponseHeadersRead` and stream to response body.
- Copy with bounded buffer; abort on limit; propagate upstream `Content-Type` and `Content-Length` (when <= MaxBytes).

Caching
- Phase 1: no server cache; set client/proxy `Cache-Control`.
- Phase 2 (optional): enable bounded in-memory or Redis cache for small assets when `EnableServerCache` is true (size-aware eviction).

## 7) Implementation Steps (Code-Level)

Application
- `src/Application/Common/Configuration/ImageProxyOptions.cs`
- `src/Application/Common/Interfaces/IServices/IImageProxyService.cs`
  - `Task<Result<ProxiedImage>> FetchAsync(Uri url, CancellationToken ct)` for validation-only paths (metadata), or
  - `Task<StreamResult> StreamAsync(Uri url, CancellationToken ct)` that returns a light wrapper with headers/content-type and a stream factory.
  - `record ProxiedImage(Stream Content, string ContentType, long? ContentLength, EntityTagHeaderValue? ETag, DateTimeOffset? LastModified)`

Infrastructure
- `src/Infrastructure/Http/ImageProxy/HttpImageProxyService.cs`
  - Use `IHttpClientFactory` (client name: "ImageProxy").
  - Validate URL (scheme/host), resolve and block private IPs.
  - Enforce content-type and size while streaming.
  - Handle up to `MaxRedirects` manually.
  - Map failures to `Error.Validation`/`Error.Problem`.
- `src/Infrastructure/DependencyInjection.cs`
  - `builder.Services.Configure<ImageProxyOptions>(config.GetSection(ImageProxyOptions.SectionName));`
  - `builder.Services.AddHttpClient("ImageProxy")...` with resilience.
  - `builder.Services.AddScoped<IImageProxyService, HttpImageProxyService>();`

Web
- Endpoint group `src/Web/Endpoints/Media.cs`:
  - `GET /api/v1/media/proxy` handler signature: `(string url, string? sig, long? exp, IImageProxyService svc, IOptions<ImageProxyOptions> opt, HttpContext ctx, CancellationToken ct)`
  - Validate signature if required. On success, stream response using `Results.Stream` and set headers:
    - `Access-Control-Allow-Origin: *`
    - `Vary: Origin`
    - `Cache-Control: public, max-age=...`
    - Copy `ETag`, `Last-Modified` when present.
  - `.WithSummary("Proxy remote images with permissive CORS")`
  - `.RequireRateLimiting("image-proxy-ip")`
  - `.Produces(StatusCodes.Status200OK)` (binary), plus standard problems.
- `src/Web/DependencyInjection.cs`:
  - Register `ImageProxyOptions` binding (see above).
  - Add rate-limiter policy `image-proxy-ip` using existing pattern.

Custom Results (optional)
- If needed, add a tiny wrapper `CorsResult : IResult` to ensure ACAO header is present even when global CORS is disabled in certain envs.

## 8) Security/Validation Details (Algorithms)

- Host Allowlist Match: exact or `EndsWith('.' + suffix)` ignoring case.
- IP Check: DNS resolve A/AAAA; for each address, ensure not in private/reserved ranges; consider re-check on redirect.
- Signature: `sig = hex(HMACSHA256(secret, $"GET\n{url}\n{exp}"))`; reject if missing/invalid or expired.
- Content-Type: Accept when upstream `Content-Type` starts with allowed list; if missing but file sniffing indicates image (optional, phase 2), proceed cautiously.
- Max Bytes: Track bytes copied; abort with 413-style problem when exceeding.

## 9) Testing Strategy

Web API Contract Tests (`tests/Web.ApiContractTests`)
- Happy path: stubbing `IImageProxyService` to return a known stream; assert 200, `Access-Control-Allow-Origin: *`, correct `Content-Type`.
- Validation: missing `url` -> 400; bad scheme -> 400; not allowlisted without signature -> 403/400.
- Limits: return 413 problem when exceeding `MaxBytes` (map as 400/Problem per conventions).

Infrastructure Integration Tests (`tests/Infrastructure.IntegrationTests`)
- Spin up a local test server that serves small images; assert streaming and headers; verify redirect handling and size limit abort.
- SSRF: attempt to target `127.0.0.1` or private CIDRs -> rejected.

Options Binding Tests
- Ensure defaults bind; override via in-memory configuration works.

## 10) Observability & Ops

- Log key events (allowlist hit, signature verified, upstream status, bytes, latency). Mask query secrets.
- Metrics (future): count successes/failures/timeouts; per-host sampling.
- Runbook: how to change allowlist, rotate signature secret, tune limits, and disable endpoint quickly (feature flag via `ImageProxy:Enabled` if needed).

## 11) Rollout Plan

Phase 1 — MVP (1–2 days)
- Options + service interface + Web endpoint with streaming, allowlist, basic SSRF checks, ACAO header, and rate limiting.
- Contract tests.

Phase 2 — Resilience + Redirects (0.5–1 day)
- Configure named HttpClient with `AddStandardResilienceHandler`; manual redirect validation; improved logging.

Phase 3 — Optional Signature Mode (0.5 day)
- Add `sig/exp` verification for non-allowlisted hosts; Key Vault secret; docs for generating signed URLs server-side.

Phase 4 — Optional Server Cache (0.5–1 day)
- Memory/Redis small-object cache with size guard; cache headers alignment.

Acceptance Criteria
- Frontend can load a foreign image via `/api/v1/media/proxy?url=...` and draw to canvas without taint.
- Large or disallowed responses are safely rejected with clear problem details.
- No open-proxy behavior (allowlist or valid signature required); SSRF mitigations in place.

## 12) Frontend Usage Notes

- HTML `<img>`: `<img src="/api/v1/media/proxy?url=${encodeURIComponent(actualUrl)}" crossOrigin="anonymous" />`
- Canvas/Fetch: `fetch('/api/v1/media/proxy?url=' + encodeURIComponent(actualUrl))` then `createImageBitmap` or draw.
- Do not pass secrets/tokens in the `url` query string.

## 13) File Map (Proposed)

- Application
  - `src/Application/Common/Configuration/ImageProxyOptions.cs`
  - `src/Application/Common/Interfaces/IServices/IImageProxyService.cs`
  - `src/Application/Common/Models/ImageProxy/ProxiedImage.cs` (if needed)
- Infrastructure
  - `src/Infrastructure/Http/ImageProxy/HttpImageProxyService.cs`
  - `src/Infrastructure/DependencyInjection.cs` (registrations)
- Web
  - `src/Web/Endpoints/Media.cs`
  - `src/Web/DependencyInjection.cs` (options + rate limiting policy)
  - `src/Web/Infrastructure/CorsResult.cs` (optional)
- Tests
  - `tests/Web.ApiContractTests/MediaProxyTests.cs`
  - `tests/Infrastructure.IntegrationTests/ImageProxy/HttpImageProxyServiceTests.cs`

---

This plan aligns with YummyZoom’s Clean Architecture patterns (thin Web endpoints, Application interfaces, Infrastructure implementations), uses existing rate limiting conventions, and emphasizes SSRF-safe, streaming-first behavior with explicit CORS headers to satisfy browser requirements.
