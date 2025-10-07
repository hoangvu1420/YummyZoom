
### Endpoint coverage and behaviors
- Necessary: Verify and align routes in `src/Web/Endpoints/Users.cs` with docs
  - Ensure these exist and match: `/api/v1/users/auth/otp/request`, `/verify`, `/refresh`, `/auth/status`, `/auth/complete-signup`, `/auth/set-password`, `/login`.
  - Confirm `otp/request` returns 200 with `{ code }` in Development and 202 with empty body in Production.
  - Ensure `auth/status` and `complete-signup` are present; make `complete-signup` idempotent (return 200 on repeated calls).
- Necessary: Clarify password login payload
  - Keep request field named `email` but validate as E.164 phone; update validation messages to say “phone in E.164” (Application validator for login model; endpoint action in `Users.cs`).
  - Add request/response summaries and examples for OpenAPI (if using Swashbuckle in `Web/Program.cs`).

### ProblemDetails and error typing
- Necessary: Standardize RFC 7807 responses
  - Extend `src/Web/Infrastructure/CustomExceptionHandler.cs` (and any filters in `src/Web/Infrastructure/CustomResults.cs`) to produce consistent ProblemDetails with stable `type` and optional `errorCode`.
  - Define canonical `type` values and `errorCode` constants (e.g., add to `src/SharedKernel/Constants`), covering `Otp.Invalid`, `Otp.Expired`, `Otp.Throttled`, `Refresh.Invalid`, `Auth.InvalidCredentials`, `Password.AlreadySet`.
  - Ensure Application handlers (e.g., `Application/Auth/Commands/*`) throw/return typed errors mapped by the handler.

### Rate limiting and lockouts
- Necessary: Add throttling for OTP request/verify
  - Introduce ASP.NET rate limiting in `src/Web/Program.cs` or `src/Web/DependencyInjection.cs`; create named policies for `otp-request` and `otp-verify`.
  - On throttle, return 429 with `Retry-After` header. Consider 423 (Locked) for temporary verify lockouts if policy includes lockout behavior.
  - Document limits and headers; wire policies to OTP endpoints in `Users.cs`.
  - If per-phone throttling is needed, use partitioned limiter by phone number; otherwise IP-based.

### Token issuance and refresh rotation
- Necessary: Confirm and enforce refresh rotation
  - Locate token generation/validation (DI hints in `src/Infrastructure/DependencyInjection.cs`); if no explicit token service, add one (e.g., `Infrastructure/Identity/Tokens/TokenService`).
  - Implement refresh token rotation: on successful refresh, issue a new refresh token and invalidate the previous one immediately (persisted store).
  - Standardize error when reusing an old refresh token (401 with `type=authentication` and `errorCode=Refresh.Invalid`).
- Recommended: Add `issuedAt` and `expiresAt` to token payload
  - Compute based on JWT claims; include alongside `expiresIn`. If not adding fields, document reliance on JWT `iat`/`exp` and advise clients on drift.

### OTP policy clarity and services
- Necessary: Lock OTP length and TTL
  - Set fixed length and TTL in `src/Infrastructure/Identity/PhoneOtp/*.cs` (`IdentityPhoneOtpService`, `StaticPhoneOtpService`) and in options (`StaticPhoneOtpOptions`).
  - Validation: enforce exact length server-side; return 400 with ProblemDetails `type=validation` and clear `detail`.
- Optional: Dev-vs-Prod signal
  - In Development only, add `X-YZ-Env: Development` header on `otp/request` in `Users.cs`. Do not include environment in production payloads.

### Auth status and onboarding
- Necessary: Validate `GET /auth/status` and `POST /auth/complete-signup`
  - Ensure `auth/status` returns `{ isNewUser, requiresOnboarding }` from the correct source of truth (Application query).
  - Ensure `complete-signup` writes the profile and is idempotent; return 200 `{}` on repeats with no side effects.

### OpenAPI/documentation alignment
- Necessary: Sync OpenAPI with MD docs
  - Add/confirm Swashbuckle setup in `src/Web/Program.cs`; annotate `Users.cs` endpoints with summaries, examples, and response types including ProblemDetails schemas.
  - Document 200 vs 202 for `otp/request`, and 429 throttling with `Retry-After`.

### Telemetry/logging
- Recommended: Structured logging for auth flows
  - Log `errorCode`, `type`, correlation id; never log OTPs/secrets. Add enrichment in `Program.cs` or Serilog config.

### Tests to lock contracts
- Recommended: Add/expand tests in `tests/Web.ApiContractTests` and `tests/Application.FunctionalTests`
  - OTP request (200 dev vs 202 prod), verify success/error cases, throttling `429` with `Retry-After`.
  - Refresh rotation behavior and reuse of old refresh token → 401 with expected ProblemDetails.
  - `auth/status` and `complete-signup` idempotency.

### Likely impacted files
- `src/Web/Endpoints/Users.cs`: Align routes, responses, headers; apply rate-limit policies.
- `src/Web/Infrastructure/CustomExceptionHandler.cs` and `src/Web/Infrastructure/CustomResults.cs`: ProblemDetails typing and shaping.
- `src/Web/Program.cs` and/or `src/Web/DependencyInjection.cs`: Add RateLimiter, Swagger/OpenAPI, auth config.
- `src/Infrastructure/DependencyInjection.cs`: JWT options; register token service and OTP options.
- `src/Infrastructure/Identity/PhoneOtp/*`: Enforce OTP TTL/length; ensure dev/prod behavior.
- `src/Application/Auth/Commands/*`: VerifyPhoneOtp, RequestPhoneOtp, CompleteSignup, SetPassword, Refresh flow; return typed errors.
- `src/SharedKernel/Constants/*`: Add `ErrorCodes`, `ProblemTypes`.
- Tests under `tests/Web.ApiContractTests` and `tests/Application.FunctionalTests`.

- Status: I located the auth endpoints and OTP services via search, plus the global exception handler and DI. Next, I’ll verify each endpoint’s exact behavior and the presence of refresh rotation and rate limiting to finalize the change list.