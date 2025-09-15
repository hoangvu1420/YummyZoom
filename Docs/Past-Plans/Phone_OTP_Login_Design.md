## Phone Number + OTP Login — Architecture‑Aligned Design

This refines the prior OTP design to fit YummyZoom’s established patterns:
- CQRS in Application (MediatR commands + validators; queries when needed)
- Thin Web endpoints using `ISender`
- Infrastructure services behind Application interfaces (Identity, SMS, normalization)
- Result pattern and consistent error contracts

---

### 1) Layer Responsibilities

- Web (Endpoints)
  - Map minimal API routes under `Users` (or `Auth`).
  - Send commands via MediatR. For OTP verify, after success, issue bearer tokens using Identity bearer scheme to preserve compatibility with `MapIdentityApi`.

- Application (CQRS)
  - Commands orchestrate OTP flows (generate, send, verify). No direct Identity or SMS dependencies; use interfaces from `Application.Common.Interfaces.IServices`.
  - Validators enforce phone format and basic constraints.
  - Return `Result<T>`; never manipulate `HttpContext` or tokens.

- Infrastructure
  - Implements `IPhoneOtpService` using `UserManager<ApplicationUser>` + token providers.
  - Implements `ISmsSender` (dev logger, prod SMS later) and `IPhoneNumberNormalizer` (E.164).
  - Exposes helper to build `ClaimsPrincipal` (`IUserClaimsPrincipalFactory<ApplicationUser>`) for token issuance in Web.

---

### 2) Use Cases & Commands

2.1 Request OTP
- Command: `RequestPhoneOtpCommand(string PhoneNumber)` : `IRequest<Result<Unit>>`
- Flow
  1) Normalize phone via `IPhoneNumberNormalizer`. If invalid → `Error.Validation("Phone.Invalid")`.
  2) Check per‑phone rate limit (see §7). On violation → `Error.TooManyRequests("Otp.RateLimited")`.
  3) Call `IPhoneOtpService.EnsureUserExistsAsync(phone)` → returns `UserId` (identity GUID) and flag `isNew`.
  4) Call `IPhoneOtpService.GenerateLoginCodeAsync(userId)` → returns OTP.
  5) `ISmsSender.SendAsync(phone, message)`.
  6) Return `Result.Success()`.

- Validator
  - `PhoneNumber` not empty; normalized length 8–15 digits; E.164 allowed prefixes.

2.2 Verify OTP (Sign‑In)
- Command: `VerifyPhoneOtpCommand(string PhoneNumber, string Code)` : `IRequest<Result<VerifyPhoneOtpResponse>>`
- Response DTO: `VerifyPhoneOtpResponse(Guid IdentityUserId, bool IsFirstSignIn)`
- Flow
  1) Normalize phone.
  2) Resolve user by phone via `IPhoneOtpService.FindByPhoneAsync(phone)`. Missing → `Error.Unauthorized("Otp.Invalid")`.
  3) Verify via `IPhoneOtpService.VerifyLoginCodeAsync(userId, code)`.
  4) On success, ensure `PhoneNumberConfirmed = true` (in service) and return `IdentityUserId` + first‑sign‑in flag.
  5) No tokens issued here (Web does it to match existing pattern).

- Validator
  - `Code` required, 4–8 digits (configurable), `PhoneNumber` rules as above.

Notes
- If you decide to create user at verification time (not request), move user provisioning to 2.3 step; current approach provisions at request time for clearer UX.

---

### 3) Application Interfaces (new)

Add under `Application.Common.Interfaces.IServices`:

```csharp
public interface IPhoneNumberNormalizer
{
    string? Normalize(string rawPhone); // returns E.164 or null if invalid
}

public interface ISmsSender
{
    Task SendAsync(string phoneE164, string message, CancellationToken ct = default);
}

public interface IPhoneOtpService
{
    Task<(Guid IdentityUserId, bool IsNew)> EnsureUserExistsAsync(string phoneE164, CancellationToken ct = default);
    Task<string> GenerateLoginCodeAsync(Guid identityUserId, CancellationToken ct = default); // numeric code
    Task<bool> VerifyLoginCodeAsync(Guid identityUserId, string code, CancellationToken ct = default);
    Task ConfirmPhoneAsync(Guid identityUserId, CancellationToken ct = default);
    Task<Guid?> FindByPhoneAsync(string phoneE164, CancellationToken ct = default);
}
```

Implementation lives in Infrastructure (Identity + SMS provider), preserving clean architecture.

---

### 4) Web Endpoints (Minimal APIs)

Extend `src/Web/Endpoints/Users.cs` (or create `Auth.cs`) with two routes; keep `MapIdentityApi<ApplicationUser>()` as‑is.

```csharp
group.MapPost("/auth/otp/request", async ([FromBody] RequestPhoneOtpCommand cmd, ISender sender) =>
{
    var result = await sender.Send(cmd);
    return result.ToIResult(AcceptedOnSuccess: true); // 202 on success
})
.WithName("Auth_Otp_Request")
.WithStandardResults()
.AllowAnonymous();

group.MapPost("/auth/otp/verify", async ([FromBody] VerifyPhoneOtpCommand cmd, ISender sender, UserManager<ApplicationUser> users, IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory, HttpContext http) =>
{
    var result = await sender.Send(cmd);
    if (!result.IsSuccess) return result.ToIResult();

    var user = await users.FindByIdAsync(result.Value.IdentityUserId.ToString());
    if (user is null) return Results.Unauthorized();

    var principal = await claimsFactory.CreateAsync(user);
    await http.SignInAsync(IdentityConstants.BearerScheme, principal);
    return Results.Ok(); // bearer tokens emitted by the handler
})
.WithName("Auth_Otp_Verify")
.WithStandardResults()
.AllowAnonymous();
```

Notes
- `ToIResult` is used consistently across endpoints; add the `AcceptedOnSuccess` convenience if not present.
- Token issuance remains in Web to align with how `MapIdentityApi` operates.

---

### 5) Infrastructure Implementations

5.1 Phone OTP Service (Identity)
- Implement `IPhoneOtpService` using `UserManager<ApplicationUser>` and Identity token provider:
  - Generate: `GenerateUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, "PhoneLogin")`
  - Verify: `VerifyUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, "PhoneLogin", code)`
  - Confirm phone: set `PhoneNumberConfirmed=true` on success.
- Ensure new users are created with `UserName = phoneE164`, `PhoneNumber = phoneE164`, `Email = null`.
- Add lifetime for the phone provider code in Identity options (e.g., 3–5 minutes).

5.2 SMS Sender
- Add `ISmsSender` dev implementation that logs the message.
- Wire prod sender later (Twilio/Azure) without changing Application.

5.3 Phone Normalizer
- Implement `IPhoneNumberNormalizer` using library or simple E.164 rules (strip non‑digits, apply country defaults if configured).

---

### 6) Data & Migrations

- Unique index on `AspNetUsers.PhoneNumber` (filtered):
  - `migrationBuilder.CreateIndex("UX_AspNetUsers_PhoneNumber", "AspNetUsers", "PhoneNumber", unique: true, filter: "\"PhoneNumber\" IS NOT NULL");`
- Lengths: keep <= 50 chars (current). Store normalized E.164.
- Backfill strategy: detect duplicates before adding unique index; soft‑block OTP for conflicting numbers until resolved.

---

### 7) Security & Rate Limiting

- Use ASP.NET Core rate limiter in Web layer:
  - Keys: `otp:req:{phone}`, `otp:verify:{phone}`; allow small burst + cooldown.
- Failed attempts → temporary lock using `UserManager.AccessFailedAsync` and lockout options, or a simple in‑memory counter behind an interface for now.
- OTP TTL 3–5 minutes; single‑use.
- Logging: redact phone except last 2–3 digits in info logs; full phone allowed only at debug with dev profile.

---

### 8) Contracts & Errors

- Use existing `ProblemDetails` mapping: codes like `Phone.Invalid`, `Otp.RateLimited`, `Otp.InvalidOrExpired`.
- Response for `/verify` must match bearer token response from Identity. If the bearer handler writes tokens automatically on `SignInAsync`, return 200 with its payload. If not, expose a small adapter to mirror MapIdentityApi output.

---

### 9) Testing Strategy

- Unit: normalizer, validators, `IPhoneOtpService` logic (with fake UserManager), rate limiter.
- Functional: request→verify happy path, invalid code, expired code, resend, new vs existing user.
- Contract: endpoints shapes, error mapping, token response schema.
- Integration: EF migration (unique phone), Identity token provider TTL behavior.

---

### 10) Rollout & Checklist

Order of work (aligns with project checklists):
1) Add interfaces (`IPhoneNumberNormalizer`, `ISmsSender`, `IPhoneOtpService`).
2) Add commands + validators (`RequestPhoneOtpCommand`, `VerifyPhoneOtpCommand`).
3) Implement Infrastructure services (Identity + SMS + normalizer) and register in DI.
4) Create endpoints; configure rate limiting policies; add logs/metrics.
5) Add EF migration for unique `PhoneNumber` and run locally.
6) Tests (unit/functional/contract/integration); finalize API docs.
7) Staged rollout; monitor OTP request volume and failure/lockouts.

Done criteria
- Commands and validators in place; endpoints wired; tokens issued via bearer scheme; phone uniqueness enforced; logs/metrics present; tests green.

