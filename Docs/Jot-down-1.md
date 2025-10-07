
### Changes to implement (ASP.NET Core best practices)

- Add global rate limiting middleware
  - Services: register rate limiting with named policies in `src/Web/DependencyInjection.cs` (or `Program.cs`):
    - otp-request-ip: fixed window per IP (e.g., 5/min, 30/hour).
    - otp-verify-ip: fixed window per IP (e.g., 10/5min).
  - Pipeline: call `app.UseRateLimiter()` early (after exception handler, before auth/endpoint mapping) in `src/Web/Program.cs`.
  - Rejections: configure `RejectionStatusCode = 429` and `OnRejected` to set `Retry-After` header.

- Apply policies to OTP endpoints
  - In `src/Web/Endpoints/Users.cs`:
    - `/auth/otp/request` → `.RequireRateLimiting("otp-request-ip")`
    - `/auth/otp/verify` → `.RequireRateLimiting("otp-verify-ip")`

- Implement per-phone throttling and lockouts (business-level)
  - Add store interface `IOtpThrottleStore` in `src/Application/Common/Interfaces/IServices/` with methods:
    - `IncrementRequestCount(phone, window)`; `GetRequestCount(phone, window)`; `GetRetryAfter(phone, window)`; `Reset(phone)`
    - `RecordFailedVerify(phone)`; `GetFailedVerifyCount(phone, window)`; `SetLockout(phone, duration)`; `GetLockoutRemaining(phone)`
  - Provide implementation using distributed cache in `src/Infrastructure/Caching/` (e.g., Redis via `IDistributedCache`) with atomic increments and expirations. Use keys per phone.
  - Register in `src/Infrastructure/DependencyInjection.cs`.

- Enforce per-phone throttles in handlers
  - `RequestPhoneOtpCommandHandler`:
    - Before generating code, check per-phone windows (e.g., 3/min, 10/hour). If exceeded, return Result failure with code `Otp.Throttled`. Include retry seconds in error description.
  - `VerifyPhoneOtpCommandHandler`:
    - If locked out, return failure `Otp.LockedOut`.
    - On invalid code: increment failed count; once threshold hit (e.g., 5 failures in 10 min), set lockout (e.g., 5 min) and return `Otp.LockedOut`.
    - On success: `ConfirmPhoneAsync(...)` and reset failed counters/lockout.

- Map throttling/lockout to HTTP responses
  - Keep using `CustomResults` with current pattern:
    - `Otp.Throttled` → 429 Too Many Requests; add `Retry-After` header from store’s remaining wait.
    - `Otp.LockedOut` → 423 Locked (or 429; choose one and document), include `Retry-After`.
  - Implement header setting in endpoint layer:
    - When `result.ToIResult()` returns a Problem, add a small helper (e.g., extension on `IResult` or wrap in endpoint) to set `Retry-After` when `error.Code` is `Otp.Throttled`/`Otp.LockedOut`.
    - Alternatively, carry `retryAfterSeconds` in `Error.Description` and set the header right before returning.

- Configuration and tuning
  - Add `appsettings` section:
    - `RateLimiting: OtpRequest: PerIp: {PerMinute, PerHour}, PerPhone: {PerMinute, PerHour}`
    - `RateLimiting: OtpVerify: PerIp: {Per5Min}, PerPhone: {FailedAttemptsWindowMinutes, LockoutMinutes, MaxFailedAttempts}`
  - Bind to options and use in both middleware policies and `IOtpThrottleStore`.

- Security/infra considerations
  - If behind a proxy/CDN, enable forwarded headers to get client IP before rate limiting.
  - Prefer Redis for consistency across instances; fallback to `IMemoryCache` for dev.

- Documentation
  - Update `Docs/API-Documentation/02-Authentication.md`:
    - Document 429 for throttled OTP request with `Retry-After`.
    - Document lockout on verify failures (423 or 429) with `Retry-After`.
    - Clarify per-phone vs per-IP protections.
  - Note: Development still returns 200 with `{ code }` for `/otp/request`; throttles apply equally.

- Tests
  - `tests/Web.ApiContractTests`: assert 429 and `Retry-After` after exceeding `/auth/otp/request` policy.
  - `tests/Application.FunctionalTests`: simulate failed verify attempts and assert lockout response and header; assert reset after success or lockout duration elapses.

- Impacted files
  - `src/Web/DependencyInjection.cs` or `src/Web/Program.cs`: AddRateLimiter config; UseRateLimiter.
  - `src/Web/Endpoints/Users.cs`: Apply `.RequireRateLimiting(...)`; set `Retry-After` for throttled/locked responses.
  - `src/Application/Auth/Commands/RequestPhoneOtp/*`, `VerifyPhoneOtp/*`: Throttle and lockout checks.
  - `src/Infrastructure/Caching/*`: `IOtpThrottleStore` implementation.
  - `Docs/API-Documentation/02-Authentication.md`: Behavior and headers.
  - appsettings.*: thresholds.