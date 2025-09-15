## Phase 4 — User Lifecycle and Admin: Implementation Outline (Updated for Phone OTP)

Status context: As of Phase 3 completion, ordering, menu, and TeamCart flows are implemented and tested. This Phase focuses on user lifecycle (registration → profile → secure email change → addresses → payment methods → deactivate/delete) and platform/admin capabilities (user management; platform-wide queries and moderation hooks). It now also includes phone-number-first authentication with OTP. The plan aligns with Next_Steps.md, Features-Design, and the Phone_OTP_Login_Design docs.

---

### 1) Goals and Non‑Goals

- Goals:
  - Deliver customer “Profile & Auth” experiences end-to-end (self-service updates; secure email change; addresses; payment methods).
  - Add phone-number sign-up/login with OTP while keeping email/password compatible for existing clients.
  - Implement RoleAssignments management and enforcement for restaurant-scoped permissions (Owner/Staff) with unique constraints and “last owner” safety.
  - Provide admin capabilities: list/search users, view details, deactivate/reactivate, fulfill deletion (GDPR-style anonymization), and support oversight of orders/tickets (read models + queries).
  - Keep Identity ↔ Domain alignment per Two-Identity-Model; zero-DB authorization on requests via cached claims.
  - Maintain API compatibility with existing client apps; add versioned endpoints with clear error contracts and pagination.
- Non‑Goals:
  - Build a full visual Admin UI in this phase; expose APIs and admin-friendly DTOs for future UI.
  - Integrate external search beyond SQL filters for admin/user lists.

---

### 2) Business Requirements Mapping

- Customer features:
  - Manage profile, addresses, and payment methods; secure email change; order history and reordering → ensure profile endpoints and queries exist to support checkout and saved methods.
  - Phone-first auth option with OTP codes; minimal friction sign-up via phone.
- Admin/Support features:
  - View platform users with filters; view user details; deactivate/reactivate; fulfill deletion requests; monitor orders and reviews; moderate reviews (moderation in Phase 5; hooks now).
- Restaurant features (RoleAssignments):
  - Admin/Owners assign/revoke/promote roles; enforce “at least one owner”.

References: Features-Design, Two-Identity-Model, Auth-Pattern, and Phone_OTP_Login_Design_Aligned.

---

### 3) Domain and Application Layer Work

3.1 User lifecycle — Commands/Handlers

- RegisterUserCommand (exists): verify validations, return `UserId`.
- UpdateUserProfileCommand: name, phone. Owner-only (self). Result-only response.
- RequestPhoneOtpCommand: normalize phone; provision identity user if missing; generate OTP; send via SMS.
- VerifyPhoneOtpCommand: verify OTP; confirm phone; return identity user id (Web issues tokens).
- AddAddressCommand / RemoveAddressCommand / SetDefaultAddressCommand: maintain an ordered list with default flag; validate; limit count (e.g., 10).
- AddPaymentMethodCommand / RemovePaymentMethodCommand / SetDefaultPaymentMethodCommand: accept token from gateway; never store raw card data; maintain default.
- RequestEmailChangeCommand → ConfirmEmailChangeCommand: two-step; single active request; on confirm emit `UserEmailChanged` and invalidate sessions.
- DeactivateUserCommand / ReactivateUserCommand: set active flag; emit `UserDeactivated` to trigger logout.
- DeleteUserAccountCommand: initiates saga (see 3.3); allow Admin force-delete as needed.

3.2 User queries (Dapper)

- GetUserProfileQuery (self): profile, addresses, payment methods, recent order summary.
- GetUserByEmailQuery (internal) for legacy auth.
- GetUserByPhoneQuery (internal) optional helper for support/ops.

3.3 Deletion/Anonymization Saga (Process Manager)

- Triggered by `UserDeleted` domain event.
- Steps with outbox and retries:
  1) Remove RoleAssignments for user.
  2) Scrub PII in Orders (customer id, delivery address phone/name), Reviews (author), SupportTickets.
  3) Remove/close external linkages: payment gateway customer/source tokens, push device registrations.
  4) Write audit record and completion marker.
- Idempotency: store saga state; resume on failure.

3.4 RoleAssignments — Commands/Handlers

- AssignRoleCommand, UpdateRoleAssignmentCommand, RevokeRoleAssignmentCommand per design.
- Invariants enforced in handlers:
  - Uniqueness on (UserId, RestaurantId).
  - “Last owner” cannot be revoked.
- Emit events: RoleAssignmentCreated/Deleted → project to Identity roles (optional) and cached claims.

---

### 4) Infrastructure Layer

- Identity integration:
  - `CreateIdentityUserAsync` remains the path for email registrations; phone OTP path provisions `ApplicationUser` with `UserName = PhoneE164` and `PhoneNumber` set.
  - Claims factory keeps current permissions model; login remains zero-DB via cached claims.
  - On `UserEmailChanged`: update Identity + Domain atomically; invalidate sessions.
- Phone OTP components:
  - `IPhoneOtpService` backed by `UserManager` + Identity token provider for purpose “PhoneLogin”.
  - `ISmsSender` abstraction (Logging implementation now; Twilio/Azure later).
  - `IPhoneNumberNormalizer` to enforce E.164 at boundaries.
- Persistence and projections:
  - UserProfileReadModel for self/admin views.
  - AdminUserListReadModel for admin list with basic filters.
  - Audit trail for admin actions (deactivate/reactivate/delete, role changes).
- Background jobs:
  - Deletion/Anonymization saga runner; cleanup for email change tokens.
  - Optional claims resync on RoleAssignment changes (or rely on next login/force logout).

---

### 5) Web API Layer (v1)

5.1 Endpoints — Customer (self)

- POST /api/users/register → 201 { userId }
- POST /api/users/auth/otp/request → 202
- POST /api/users/auth/otp/verify → 200 (issues bearer tokens via Identity Bearer scheme)
- GET /api/users/me → 200 UserProfileDto
- PUT /api/users/me/profile → 204
- POST /api/users/me/addresses → 201 { addressId }
- DELETE /api/users/me/addresses/{addressId} → 204
- POST /api/users/me/addresses/{addressId}/default → 204
- POST /api/users/me/payment-methods → 201 { paymentMethodId }
- DELETE /api/users/me/payment-methods/{paymentMethodId} → 204
- POST /api/users/me/payment-methods/{paymentMethodId}/default → 204
- POST /api/users/me/email-change/requests → 202
- POST /api/users/me/email-change/confirm → 204
- POST /api/users/me/deactivate → 204
- DELETE /api/users/me → 202 (starts deletion saga)

5.2 Endpoints — RoleAssignments

- POST /api/restaurants/{restaurantId}/roles → 201 { roleAssignmentId }
- PUT /api/role-assignments/{roleAssignmentId} → 204
- DELETE /api/role-assignments/{roleAssignmentId} → 204
- GET /api/restaurants/{restaurantId}/staff?… → 200 Paginated staff list

5.3 Endpoints — Admin

- GET /api/admin/users?query=&isActive=&from=&to=&page=&pageSize= → 200 Paginated list
- GET /api/admin/users/{userId} → 200 AdminUserDetailsDto
- POST /api/admin/users/{userId}/deactivate → 204
- POST /api/admin/users/{userId}/reactivate → 204
- DELETE /api/admin/users/{userId} → 202 (deletion saga)
- GET /api/admin/orders?… → 200 Paginated list (oversight)

5.4 Contracts & policies

- Authorization: `[Authorize]` with policies for self (`Policies.MustBeUserOwner`) and restaurant-scoped actions; admin endpoints use `[Authorize(Roles="Admin")]` plus policy where needed.
- Versioning: route or header versioning; all new endpoints under v1.
- Error model: consistent `ProblemDetails` codes; validation errors mapped from FluentValidation.
- Pagination: `page`, `pageSize` with reasonable caps (e.g., 100) and continuation metadata.

---

### 6) Client App Integration Considerations

- Auth flows:
  - Phone OTP: call `/users/auth/otp/request` then `/users/auth/otp/verify` → receive the same bearer token shape as MapIdentityApi.
  - Keep existing email/password login/refresh.
  - On email change confirmation or deactivation, next request gets `401` and a `ForceLogout` push; clients clear session.
- Profile screens:
  - Single `GET /users/me` powering profile, addresses, and payment methods. Use ETags to minimize traffic.
- Error handling UX:
  - Email-in-use, invalid/expired OTP, or rate-limit exceeded → actionable messages; align error codes for i18n mapping.
- Offline and retries:
  - Address/payment writes can accept `Idempotency-Key` (optional).
- Admin tools:
  - Server-side pagination/sorting; request only needed columns.

---

### 7) Data Model and Indexing

- Users: ensure unique index on Email (case-insensitive collation); add unique filtered index on PhoneNumber (E.164; `PhoneNumber IS NOT NULL`).
- Addresses: FK to Users; unique filtered index on `(UserId, IsDefault) WHERE IsDefault = 1`.
- PaymentMethods: FK to Users; store gateway token and brand/last4 only; unique filtered index on `(UserId, IsDefault) WHERE IsDefault = 1`.
- RoleAssignments: unique index `(UserId, RestaurantId)`; additional index on `(RestaurantId, Role)` for staff queries.
- Read models: AdminUserList and UserProfile read models with updated/created timestamps.

---

### 8) Testing Strategy

- Unit: validators; application services (email change orchestration; default switching; last-owner checks); phone normalizer.
- Functional: end-to-end OTP request/verify (first-time and returning user); admin deactivate/reactivate; deletion saga kickoff.
- Integration: Identity ↔ Domain transactionality; Dapper queries for admin/user reads.
- Contract: new web endpoints (OTP, profile, admin) with authorization behaviors and error shapes.

---

### 9) Observability, Security, and Compliance

- Observability: structured logs for admin actions; saga telemetry; dashboards (OTP failure rate, OTP volume, deletion backlog).
- Security: rate limits on public endpoints (stricter on `/users/auth/otp/*`); webhook signature checks; least-privilege DB roles; encrypt sensitive columns where stored.
- Privacy: PII classification; configurable retention for audit records; optional data export endpoint.

---

### 10) Rollout Plan and Milestones

1) Phone OTP MVP (endpoints + DI + unique phone index) and client smoke test.
2) User self-service (profile, addresses, payment methods) + queries.
3) Secure email change flow end-to-end (token storage + email sending + session invalidation).
4) RoleAssignments with invariants; staff list query and events.
5) Admin user list/details + deactivate/reactivate.
6) Deletion/Anonymization Saga with partial scope (Orders + Reviews) → expand as needed.
7) Contract tests; perf pass on hot queries; finalize API docs.

---

### 11) Notable Recommendations and Considerations

- Adopt the Two-Identity-Model strictly: Identity is SSoT for credentials and email; Domain mirrors/reacts via events and orchestrations.
- Keep authorization zero-DB on requests: generate claims once at login; when RoleAssignments change, either wait for next login or implement a push-based logout/refresh.
- Enforce RoleAssignments uniqueness on `(UserId, RestaurantId)` and implement “last owner” guard in handlers.
- Make deletion a resilient, idempotent saga with explicit states and compensations; expose status to Admins.
- Maintain strict DTO boundaries; never expose domain types; document error codes for client mapping.
- Prefer Dapper for read-side admin queries; cap page size and index for sort columns.
- Favor phone-first UX where appropriate; normalize all phones to E.164 at boundaries.

---

### 12) Definition of Done (Phase 4)

- All endpoints implemented and protected; validators in place (including OTP endpoints).
- Read models materialized and wired to domain events.
- Identity ↔ Domain sync verified with tests.
- Contract tests green; observability added; docs updated.

---

### 13) Step‑by‑Step Implementation Checklist (Updated)

Use this as the concrete execution order. Each step is small, testable, and keeps client contracts stable. Mark items complete as you go.

A) Foundations and Pre‑work
- [ ] Align Identity ↔ Domain IDs in `IIdentityService` (confirm `ApplicationUser.Id == Domain UserId`).
- [ ] Add/verify DB indexes: Users.Email (unique, CI), RoleAssignments (UserId, RestaurantId), filtered defaults for addresses/payment methods.
- [ ] Add/verify DB index: Users.PhoneNumber unique filtered (E.164; `PhoneNumber IS NOT NULL`).
- [ ] Create email change token storage (table or cache) with TTL + unique constraint per user.
- [ ] Define shared error codes and map to `ProblemDetails`.

A1) Phone OTP Enablement
- [ ] Interfaces in Application: `IPhoneNumberNormalizer`, `ISmsSender`, `IPhoneOtpService`.
- [ ] Commands + validators: `RequestPhoneOtp`, `VerifyPhoneOtp`.
- [ ] Infrastructure: Identity-backed `IPhoneOtpService`; dev `ISmsSender`; default phone normalizer.
- [ ] Web endpoints: `/api/users/auth/otp/request` and `/api/users/auth/otp/verify` (anonymous).
- [ ] Token issuance in Web via `IdentityConstants.BearerScheme` to match `MapIdentityApi`.
- [ ] Add rate limits for OTP endpoints; log OTP events (redacted phone).

B) User Self‑Service (Profile, Addresses, Payment Methods)
- [ ] Commands: `UpdateUserProfile`, `AddAddress`, `RemoveAddress`, `SetDefaultAddress`.
- [ ] Validators: field lengths, requireds, max 10 addresses; default uniqueness.
- [ ] Queries: `GetUserProfile` (self) returning profile + addresses + payment methods.
- [ ] Web Endpoints (`/api/users/me/...`), Auth policy `MustBeUserOwner` in place.
- [ ] Tests: unit (validators, handlers) + functional (happy/edge) + contract (endpoints, auth).

C) Payment Methods (Tokenized)
- [ ] Command trio: `AddPaymentMethod`, `RemovePaymentMethod`, `SetDefaultPaymentMethod` (store token, brand, last4 only).
- [ ] Integrate gateway tokenization via existing abstraction; no PAN storage.
- [ ] Update `GetUserProfile` to include payment summary; pagination not required.
- [ ] Tests: integration for token save/remove; contract tests for endpoints.

D) Secure Email Change
- [ ] Command: `RequestEmailChange` (generate token, store, send email via notifications service).
- [ ] Command: `ConfirmEmailChange` (validate token, update Identity + Domain in one transaction).
- [ ] Emit `UserEmailChanged` → handler to invalidate sessions (logout all devices) and notify old email.
- [ ] Web endpoints: `/api/users/me/email-change/requests` and `/confirm`.
- [ ] Tests: unit (token lifecycle), functional (end‑to‑end), contract (error codes: email in use, token expired/invalid).

E) Deactivate/Reactivate
- [ ] Commands: `DeactivateUser`, `ReactivateUser` with policy checks; emit `UserDeactivated`.
- [ ] Handler to invalidate refresh tokens and live sessions.
- [ ] Endpoints: `/api/users/me/deactivate`, `/api/admin/users/{id}/reactivate`.
- [ ] Tests: functional (authz), contract (401 on next call), logging assertions.

F) RoleAssignments
- [ ] Commands: `AssignRole`, `UpdateRoleAssignment`, `RevokeRoleAssignment` with uniqueness `(UserId, RestaurantId)` and “last owner” guard.
- [ ] Query: `GetStaffForRestaurant` (paginated) and `GetUserRoleAssignments` (self).
- [ ] Events → claims projection (at login) and optional Identity roles sync.
- [ ] Endpoints: create/update/delete; list staff.
- [ ] Tests: unit (last owner), functional (authz paths), contract (staff listing paging/sorting).

G) Admin Users API
- [ ] Read models: `AdminUserListReadModel`, `UserProfileReadModel` (for admin details).
- [ ] Queries: list with filters; details view with recent activity.
- [ ] Endpoints: list, details, deactivate/reactivate, delete (saga kickoff).
- [ ] Tests: Dapper query integration; contract tests for pagination, filters, sorting.

H) Deletion/Anonymization Saga
- [ ] Define saga state table (per user) + outbox integration; idempotent steps.
- [ ] Step handlers: remove RoleAssignments; scrub Orders/Reviews/Support; remove gateway customer + device sessions.
- [ ] Completion marker + audit log; expose status (admin query optional).
- [ ] Endpoint: `/api/users/me` DELETE (202) and admin DELETE.
- [ ] Tests: unit (state machine), integration (PII scrub), resilience (retry/backoff).

I) Client Integration and Backward Compatibility
- [ ] Document endpoint contracts and error codes; add examples.
- [ ] Add ETag support to `/users/me` and instruct clients to cache.
- [ ] Implement `ForceLogout` notification on email change/deactivate; verify client handling.
- [ ] Optional: support `Idempotency-Key` header for write endpoints; return `Idempotency-Key` echo.

J) Observability and Security
- [ ] Add structured logs and event IDs for admin actions and saga steps.
- [ ] Add rate limits on `/users/*` and `/admin/*` with stricter limits for `/users/auth/otp/*`.
- [ ] Dashboard panels: email change failures, deletion saga backlog, admin actions per day, OTP success/failure rates.
- [ ] Security review: secrets, PII classification, column encryption decisions, webhook signatures.

K) Rollout
- [ ] Promote behind feature flags where applicable.
- [ ] Run migrations; backfill read models.
- [ ] Smoke tests in staging with contract test suite; verify dashboards.
- [ ] Enable in production incrementally; monitor and iterate.

