# Auth & User Management – Product and Technical Spec

Version: 1.0 (2025-09-16)

Owner: Core Platform

Status: Draft (matches current implementation)

## 1. Scope

Defines how users authenticate, sign up, and manage their profile and devices in YummyZoom. Covers Identity (ASP.NET Identity), Domain User aggregate, OTP-based phone login, onboarding, roles/policies, profile, address, and device registration.

Out of scope: Restaurant staff/owner admin flows, payments account linking, social auth, SSO.

## 2. Goals & Non‑Goals

- Goals
  - Provide passwordless phone OTP login.
  - Separate “verify phone” from “complete signup” to support progressive onboarding.
  - Keep a 1:1 mapping between Identity user and Domain User aggregate.
  - Ensure existing users can login and access features immediately after OTP verify.
  - Offer clear client branching based on onboarding status.
  - Provide testable, deterministic flows (static OTP in dev/test).

- Non‑Goals
  - Building a full user state machine beyond “domain user exists” (v1).
  - Server‑side UI for onboarding.
  - Multi‑factor auth beyond phone OTP.

## 3. Key Concepts & Entities

- Identity User (ASP.NET Identity)
  - Primary record used by authentication and roles.
  - Username = E.164 phone for phone‑based accounts.
  - Email may be temporary for phone signups.
  - PhoneNumber is confirmed after successful OTP verification.

- Domain User (DDD aggregate: `User`)
  - Business entity for profile, addresses, payments, etc.
  - ID equals Identity user Guid (`UserId = IdentityUserId`).
  - Created during Complete Signup, not during OTP verify.

- Onboarding Status (v1)
  - Derived from existence of a Domain User for the Identity id.
  - `IsNewUser = true` → requires signup; `false` → returning user.

- Roles & Policies
  - Role: `User` (baseline customer role).
  - Policies:
    - `CompletedOTP`: requires authenticated Identity user (after OTP verify).
    - `CompletedSignup`: requires role `User` (implies full customer access).

## 4. Authentication Flows

### 4.1 Phone OTP – Request

- Endpoint: `POST /users/auth/otp/request`
- Request: `{ phoneNumber: string }`
- Behavior:
  - Normalize and validate phone.
  - Ensure Identity user exists for phone (create if missing; attempt add `Roles.User`).
  - Generate login code.
  - Send via SMS (logging sender in dev). In Development, response contains `{ code }`. In Production, returns `202 Accepted` without code.

- Errors:
  - Validation error if phone invalid.
  - Identity creation errors (rare) surfaced as `Otp.UserCreateFailed`.

### 4.2 Phone OTP – Verify

- Endpoint: `POST /users/auth/otp/verify`
- Request: `{ phoneNumber: string, code: string }`
- Behavior:
  - Find Identity user by phone; verify token; confirm phone.
  - Returns a SignIn result for the Identity Bearer scheme which emits token JSON.
- Response (Bearer token JSON): `{ access_token, refresh_token, token_type, expires_in, ... }`

- Errors:
  - `Otp.Invalid` for bad/expired code.
  - `Otp.UserNotFound` if user missing (unexpected after request step).

### 4.3 Complete Signup

- Endpoint: `POST /users/auth/complete-signup` (Requires Authentication)
- Request: `{ name: string, email?: string }`
- Behavior:
  - Creates Domain User with ID = current Identity user Guid, if missing.
  - Uses request Name; uses Email if provided; phone inferred from Identity username.
  - Idempotent: if Domain User exists, returns success.
- Response: `200 OK` on success.

- Errors:
  - `Auth.Unauthorized` when unauthenticated.
  - Validation errors on name/email.

### 4.4 Auth Status

- Endpoint: `GET /users/auth/status` (Requires Authentication)
- Behavior:
  - Determines onboarding state by checking whether a Domain User exists for the current Identity user id.
  - Response: `{ isNewUser: boolean, requiresOnboarding: boolean }` where `requiresOnboarding == isNewUser`.

- Errors:
  - `401 Unauthorized` when not authenticated.

### 4.5 Returning Login

- For existing users, after OTP verify returns tokens, calling `/users/auth/status` yields `{ isNewUser:false, requiresOnboarding:false }`.
- Client can route directly to main experience.

## 5. Client Integration

### 5.1 New User (First‑Time)

1) `POST /users/auth/otp/request` → display SMS code input (or dev code).
2) `POST /users/auth/otp/verify` → receive token JSON; store access/refresh tokens.
3) `GET /users/auth/status` → if `isNewUser`, call `POST /users/auth/complete-signup` with profile name/email.
4) Optionally prompt for address; navigate to home.

### 5.2 Returning User

1) Request OTP → Verify OTP (receive tokens).
2) Call `GET /users/auth/status` → if `isNewUser:false`, navigate to home.

## 6. Authorization Model

- Identity roles: provided by ASP.NET Identity. Baseline `Roles.User` is assigned when the account is created in the OTP request flow (best‑effort; failures are ignored in MVP Static provider).
- Policies:
  - `CompletedOTP`: endpoints needing just a verified sign‑in (rare; typically immediately after verify).
  - `CompletedSignup`: endpoints requiring a full Domain User (most user features).

## 7. User Profile & Address

- Get My Profile
  - Endpoint: `GET /users/me` (Requires Authorization)
  - Returns aggregated view including name, email, phone, and primary address if present.

- Complete Profile (Existing Users)
  - Endpoint: `PUT /users/me/profile` (Requires Authorization)
  - Command updates `Name` and optionally `Email` on the Domain User.
  - Use only after signup created the Domain User.

- Upsert Primary Address
  - Endpoint: `PUT /users/me/address` (Requires Authorization)
  - Creates/updates the primary address in the Domain User aggregate.

## 8. Device Management

- Register Device
  - Endpoint: `POST /users/devices/register` (Requires Authorization)
  - Stores device token for push notifications.

- Unregister Device
  - Endpoint: `POST /users/devices/unregister` (Requires Authorization)
  - Removes device token.

## 9. Role Assignments (Admin/Owner tooling)

- Create Role Assignment: `POST /users/role-assignments`.
- Delete Role Assignment: `DELETE /users/role-assignments/{id}`.
- Used by administrative flows; not part of customer onboarding.

## 10. Error Model & Validation

- Shared Result envelope represents success/failure.
- Common codes used:
  - `Phone.Invalid`, `Otp.Invalid`, `Otp.UserNotFound`, `Otp.UserCreateFailed`
  - `Auth.Unauthorized`
  - `User.UserNotFound`, `User.DuplicateEmail`, `User.EmailUpdateFailed`, `User.ProfileUpdateFailed`
- Validation occurs at command validators and service boundaries.

## 11. Security Considerations

- OTP code length and numeric constraint enforced by validator.
- Static code provider only for non‑production (Development) or explicit configuration.
- Rate limiting and lockout (future):
  - Per‑phone rate limit for request.
  - Per‑user/token attempt lockout on verify.
- Transport: HTTPS only; no OTP over insecure channels.
- Phone normalization ensures consistent matching; E.164 format.

## 12. Data & Identity Mapping

- Identity user → `Guid` key.
- Domain User `Id` is `UserId` wrapping the same `Guid`.
- Domain User is created in Complete Signup only.
- On OTP verify, only Identity is mutated (PhoneNumberConfirmed true).

## 13. Telemetry & Auditing (recommended)

- Log OTP request and verify attempts (without code values in prod).
- Emit metrics: request count, verify success/failure, signup completion rate.
- Audit identity changes (phone confirmation) and domain profile updates.

## 14. Configuration

- Static OTP options: `Otp:StaticCode` (defaults to `111111`).
- Authentication: Identity Bearer configured with 7‑day refresh lifetime (current default).

## 15. API Reference (Summary)

- `POST /users/auth/otp/request`
  - Body: `{ phoneNumber }`
  - Dev Response: `200 { code }`; Prod: `202 Accepted`.

- `POST /users/auth/otp/verify`
  - Body: `{ phoneNumber, code }`
  - Response: `200 Bearer token JSON { access_token, refresh_token, ... }`

- `GET /users/auth/status` (auth)
  - Response: `200 { isNewUser, requiresOnboarding }`

- `POST /users/auth/complete-signup` (auth)
  - Body: `{ name, email? }`
  - Response: `200 OK`

- `GET /users/me` (auth)
  - Response: profile aggregate JSON.

- `PUT /users/me/profile` (auth)
  - Body: `{ name, email? }`
  - Response: `204 No Content`

- `PUT /users/me/address` (auth)
  - Body: address fields
  - Response: `200 { addressId }`

## 16. State & Sequence (Textual)

### 16.1 New User
1) Client → Request OTP (phone)
2) Server → Ensure Identity user; send/generate code
3) Client → Verify OTP (phone + code) → receive tokens
4) Client → Auth Status → {new:true,onboard:true}
5) Client → Complete Signup (name, email?) with bearer auth
6) Server → Create Domain User; OK
7) Client → Optional profile/address → Home

### 16.2 Returning User
1) Request OTP → Verify OTP → receive tokens
2) Client → Auth Status → {new:false,onboard:false}
3) Client → Home

## 17. Edge Cases

- Verify with invalid/expired code → `Otp.Invalid`.
- CompleteSignup unauthenticated → `Auth.Unauthorized`.
- CompleteProfile before signup → `User.UserNotFound`.
- Re‑running CompleteSignup → success (idempotent).

## 18. Testing Strategy

-- Functional tests (implemented):
  - Signup Flow: token issuance from OTP verify; status flags; completing signup creates Domain User.
  - Returning User: verify + tokens; status indicates no onboarding; profile endpoints work.
  - Validation/Auth: invalid OTP, unauthorized signup, pre‑signup profile failure.
  - Integration: signup → address → profile aggregate matches.

## 19. Future Work

- Dedicated Onboarding service with persisted milestones.
- Add rate limiting and lockouts.
- Additional auth factors (email OTP, device‑bound passkeys).
- Session management (device list, revoke).
- User deletion and data export for privacy.

---

Appendix A: Implementation Pointers

- Key Commands: `RequestPhoneOtpCommand`, `VerifyPhoneOtpCommand`, `CompleteSignupCommand`, `CompleteProfileCommand`, `UpsertPrimaryAddressCommand`, `GetMyProfileQuery`.
- Services: `IPhoneOtpService` (Identity/Static implementations), `IIdentityService`, `IUserAggregateRepository`, `IUnitOfWork`.
- Policies: configured in `DependencyInjection.AddAuthenticationServices()`.
