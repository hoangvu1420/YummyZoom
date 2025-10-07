## Phase 1 — Foundation & User Authentication (Step 0: Backend API Analysis & Synchronization)

Date: 2025-10-07

Scope: Customer app authentication and initial identity lifecycle to support Sign In, Sign Up (onboarding), token handling, and guarded navigation as defined in `docs/core/overall-roadmap.md` Phase 1 and the workflow in `docs/core/standard-workflow.md` Step 0.

---

### 1) Current Backend Capabilities (from API docs)

Based on `docs/API-Documentation/02-Authentication.md` and `docs/API-Documentation/API-Reference/Customer/01-Authentication-and-Profile.md`:

- Phone OTP flow:
  - `POST /api/v1/users/auth/otp/request`
  - `POST /api/v1/users/auth/otp/verify` → returns bearer tokens `{ tokenType, accessToken, expiresIn, refreshToken }`
- Token refresh:
  - `POST /api/v1/users/refresh`
- Onboarding & status:
  - `GET /api/v1/users/auth/status` → `{ isNewUser, requiresOnboarding }`
  - `POST /api/v1/users/auth/complete-signup` (name required, email optional)
- Optional password login (after OTP user sets password):
  - `POST /api/v1/users/auth/set-password`
  - `POST /api/v1/users/login` (Identity field `email` carries the phone in E.164)
- Profile & device endpoints for later phases:
  - `GET /api/v1/users/me`, `PUT /api/v1/users/me/profile`, `PUT /api/v1/users/me/address`
  - `POST /api/v1/users/devices/register` / `unregister`

Conclusion: The documented endpoints cover all Phase 1 needs for OTP-first sign-in, token issuance, refresh, onboarding, and auth status.

---

### 2) Mobile Client Requirements (from Roadmap Phase 1)

- Sign In, Sign Up, Forgot Password screens; loading indicators; error messaging.
- ViewModel methods: `login(email, password)`, `register(...)` (email/password path) AND OTP path.
- Persist tokens securely (Hive); attach `Authorization: Bearer` automatically via HTTP client interceptor.
- Route guarding: redirect unauthenticated users to Sign In.

Notes:
- Because the platform is phone-first OTP, the initial MVP will prefer OTP flows and use password only if a user has set it. The UI can still show a password path with guidance.

---

### 3) Identified Gaps and Proposed Backend Changes

No blocking functional gaps identified for Phase 1. The following proposals aim to simplify mobile integration, reduce ambiguity, and minimize churn:

P1 — Standardize phone username field contract on `/login` (clarification-only)
- Today, the field is named `email` but must contain the E.164 phone number. Keep contract but explicitly document it in OpenAPI and problem responses (already described in MD docs). Ensure server-side validation message reflects that `email` represents phone for this product.

P1 — Consistent Problem Details for Auth Errors
- Ensure OTP and login endpoints return RFC 7807 problem details with stable `type` values (e.g., `authentication`, `validation`) and clear `detail`. The docs already show examples; request confirmation that production follows the same structure consistently.

P2 — Explicit Environment Behavior for OTP Request
- The docs specify 200 (dev with code) vs 202 (prod SMS). Propose a response header `X-YZ-Env: Development|Production` or include an `environment` field only in dev responses to help client-side toggling of flows during internal testing. Optional.

P3 — Token Expiry Metadata (non-blocking)
- Optionally include `issuedAt` and absolute `expiresAt` timestamps alongside `expiresIn` to simplify client scheduling for token refresh.

P3 — Rate Limiting & Abuse Signals (non-blocking)
- Provide `Retry-After` on throttled OTP requests and stable status code (e.g., 429). Document any per-phone limits so the app can show user-friendly wait messages.

---

### 4) Mobile-Oriented API Contracts (for confirmation)

OTP Request
- Request: `{ phoneNumber: "+15551234567" }`
- Dev: `200 { code: "123456" }`; Prod: `202 {}`
- Errors: 400 validation with problem details; 429 throttling optional.

OTP Verify (Sign-In)
- Request: `{ phoneNumber: "+15551234567", code: "123456" }`
- Success: token payload `{ tokenType, accessToken, expiresIn, refreshToken }`
- Errors: 400 validation; 401 invalid/expired code.

Refresh Token
- Request: `{ refreshToken: "..." }`
- Success: same token payload shape; 401 if invalid/expired.

Auth Status
- Response: `{ isNewUser: boolean, requiresOnboarding: boolean }`

Complete Signup
- Request: `{ name: string, email?: string }`
- Success: `200 {}` (idempotent)
- Errors: 400 validation.

Password Login (optional path)
- Request: `{ email: "+15551234567", password: string }` (email carries phone)
- Success: token payload.

---

### 5) Open Questions to Backend Team

Q1. OTP Code Policy
- What are the exact OTP length/rules and expiry window? Current docs imply 4–8 digits; confirm fixed length and exact TTL.

Q2. Throttling & Lockouts
- Are there rate limits per phone/IP for OTP requests and verify attempts? What are the 429/423 behaviors and headers returned?

Q3. Refresh Token Rotation
- Are refresh tokens rotated on every refresh (as examples suggest) and is the previous refresh token immediately invalidated? Confirm error code when using an old token after rotation.

Q4. Dev vs Prod Behavior Flags
- Can we rely on a header or a field to distinguish dev from prod responses on OTP request, or should the client infer from environment config only?

Q5. Username Evolution
- If a user later sets a real email, does `/login` continue to expect `email`=phone, or does it accept either phone or email? If both are supported, how does the server disambiguate?

Q6. Token Lifetime Configuration
- Access token TTL is 3600s in docs. Can the server return absolute `expiresAt` and `issuedAt` to reduce client clock drift issues?

Q7. Problem Details Contract
- Can we lock the `type` values and include optional `errorCode` for client-side telemetry? E.g., `Otp.Invalid`, `Refresh.Invalid`, `Password.AlreadySet`.

---

### 6) Acceptance Criteria for Backend Readiness (Phase 1)

- Endpoints in Section 4 respond as documented in production and staging.
- Problem details follow RFC 7807 with stable `type` and clear `detail`.
- Token payload includes `tokenType`, `accessToken`, `expiresIn`, `refreshToken` (and optionally `issuedAt`, `expiresAt`).
- OTP request differentiates 200 (dev) vs 202 (prod); throttling returns 429 with `Retry-After` if applicable.

---

### 7) Client Integration Notes

- Store tokens in Hive; add `AuthInterceptor` to attach `Authorization: Bearer <accessToken>`.
- On 401 with `authentication` type, attempt refresh flow once; if refresh fails, clear tokens and redirect to Sign In.
- Guard routes via `GoRouter` redirect based on token presence/validity and `requiresOnboarding` status.
- Support both OTP and password login UI; guide users that the username field expects phone in E.164.

---

Status: Ready to share with backend for confirmation. No blocking changes identified; proposals P1-P3 are quality-of-life improvements.



---

### 8) Backend Response: Problem Details Convention

**RFC 7807 Structure:**
- `title` = Error code (e.g., `Otp.Invalid`, `MenuItem.Invalid`)
- `detail` = Human-readable message
- `type` = RFC URI
- Clients should key off `title` for programmatic handling

**Example:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Otp.Invalid",
  "status": 400,
  "detail": "Invalid or expired code"
}
```

---

### 9) Backend Update: Rate Limiting Implementation (2025-10-07)

**Status: ✅ IMPLEMENTED**

**Limits:**
- Per-IP: 5/min, 30/hour (request); 10/5min (verify)
- Per-Phone: **1/min**, 10/hour (request); 5 failures → 5min lockout

**Response Codes:**
- **429**: Rate limit exceeded → title: `Otp.Throttled`
- **423**: Account locked → title: `Otp.LockedOut`
- **Retry-After header** included with wait time

**Key Points:**
- Strict 1 request/minute per phone
- Limits reset on successful verification
- Use `title` field for error handling

---

### 10) Rate Limiting Update: Stricter Limits (2025-10-07)

**Status: ✅ UPDATED**

**Change:** Reduced OTP requests from 3/min to **1/min per phone**

**Frontend Notes:**
- Show countdown timer using `Retry-After` header
- Clear messaging about 1-minute wait

---

### 11) Backend Implementation: P2 Environment Header (2025-10-07)

**Status: ✅ IMPLEMENTED**

**Added:** `X-YummyZoom-Environment` header to all OTP responses (`POST /api/v1/users/auth/otp/request`)

**Response Headers:**
```
X-YummyZoom-Environment: Development|Production|Test
```

**Benefits:**
- Explicit environment detection (no more inferring from 200 vs 202)
- Better debugging and testing support
- Backward compatible

**Usage:** `response.headers['X-YummyZoom-Environment']`

---

### 12) Backend Responses to Frontend Questions (2025-10-07)

**Q1. OTP Code Policy**
- **Length**: 6 digits (fixed)
- **Format**: Numeric only
- **Expiry**: 5 minutes (ASP.NET Core Identity default)
- **Dev vs Prod**: Static "111111" (dev) vs generated (prod)

**Q2. Throttling & Lockouts**
- **Per-Phone**: 1/min, 10/hour (requests); 5 failures → 5min lockout
- **Per-IP**: 5/min, 30/hour (requests); 10/5min (verify)
- **Response Codes**: 429 (`Otp.Throttled`), 423 (`Otp.LockedOut`)
- **Headers**: `Retry-After` included

**Q3. Refresh Token Rotation**
- **Rotation**: Yes, every refresh invalidates old token
- **Lifetime**: 7 days
- **Endpoint**: `/api/Users/Refresh`
- **Error**: 401 for old/invalid tokens

**Q4. Environment Detection**
- **Status**: ✅ Implemented (see P2 above)
- **Header**: `X-YummyZoom-Environment: Development|Production|Test`
- **Usage**: Reliable environment detection

**Q5. Username Evolution**
- **Login Field**: Always expects phone number in `email` field
- **Email Updates**: Only update profile, not login credentials
- **No Email Login**: Phone remains sole authentication method

**Q6. Token Timestamps**
- **Current**: Only `expiresIn` (3600s access, 7 days refresh) included in response body
- **No Absolute Times**: `issuedAt`/`expiresAt` not included by default
- **Enhancement**: Would require custom token response

**Q7. Problem Details**
- **Status**: ✅ Already RFC 7807 compliant
- **Error Codes**: Stable `title` field (`Otp.Invalid`, `Otp.Throttled`, etc.)
- **Client Usage**: Key off `title` for programmatic handling
- **Telemetry Ready**: Consistent error codes across all endpoints

