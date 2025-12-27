# Payout Operations (Provider)

Base path: `/api/v1/`

These endpoints let restaurant owners and staff check payout eligibility, request a payout, and view payout history.

Authorization: Read endpoints require `MustBeRestaurantStaff`. Requesting a payout requires `MustBeRestaurantOwner`.

---

## Payout Eligibility

### GET /restaurants/{restaurantId}/account/payout-eligibility

Returns whether the restaurant can request a payout now, along with available balance and any ineligibility reason.

- Authorization: `MustBeRestaurantStaff`
- Response 200: `PayoutEligibilityDto`
  - Fields: `isEligible`, `availableAmount`, `currency`, `hasPayoutMethod`, `nextEligibleAt`, `ineligibilityReason`
- Notes:
  - `ineligibilityReason` is one of `PayoutMethodMissing`, `InsufficientBalance`, `WeeklyCadence`, or null if eligible.
  - `nextEligibleAt` is set only when `WeeklyCadence` applies.

Example response

```json
{
  "isEligible": false,
  "availableAmount": 0,
  "currency": "VND",
  "hasPayoutMethod": true,
  "nextEligibleAt": "2025-10-01T00:00:00Z",
  "ineligibilityReason": "WeeklyCadence"
}
```

---

## Request a Payout

### POST /restaurants/{restaurantId}/account/payouts

Requests a payout. If `amount` is omitted, the server uses the available balance.

- Authorization: `MustBeRestaurantOwner`
- Request body
  - `amount` (optional) — decimal
  - `idempotencyKey` (optional) — string
- Response 201: `RequestPayoutResponse`
  - Fields: `payoutId`, `status`, `amount`, `currency`

Example request

```json
{
  "amount": 150000,
  "idempotencyKey": "payout-req-2025-01-01-001"
}
```

Example response

```json
{
  "payoutId": "f47ac10b-...",
  "status": "Requested",
  "amount": 150000,
  "currency": "VND"
}
```

---

## Payout History

### GET /restaurants/{restaurantId}/account/payouts?pageNumber=1&pageSize=20&status=&from=&to=

Returns paginated payout history for a restaurant.

- Authorization: `MustBeRestaurantStaff`
- Query params (all optional)
  - `status` — payout status filter
  - `from`, `to` — ISO 8601 timestamps applied to `requestedAt`
  - `pageNumber` (default 1)
  - `pageSize` (default 20)
- Response 200: Paginated list of `PayoutSummaryDto`
  - Fields: `payoutId`, `amount`, `currency`, `status`, `requestedAt`, `completedAt`, `failedAt`

Example response item

```json
{
  "payoutId": "f47ac10b-...",
  "amount": 150000,
  "currency": "VND",
  "status": "Completed",
  "requestedAt": "2025-09-14T10:20:00Z",
  "completedAt": "2025-09-14T10:20:07Z",
  "failedAt": null
}
```

---

## Payout Detail

### GET /restaurants/{restaurantId}/account/payouts/{payoutId}

Returns payout details.

- Authorization: `MustBeRestaurantStaff`
- Tenancy: `payoutId` must belong to `restaurantId`; otherwise 404.
- Response 200: `PayoutDetailsDto`
  - Fields: `payoutId`, `restaurantId`, `restaurantAccountId`, `amount`, `currency`, `status`,
    `requestedAt`, `completedAt`, `failedAt`, `providerReferenceId`, `failureReason`, `idempotencyKey`

Example response

```json
{
  "payoutId": "f47ac10b-...",
  "restaurantId": "b1c2...",
  "restaurantAccountId": "c3d4...",
  "amount": 150000,
  "currency": "VND",
  "status": "Processing",
  "requestedAt": "2025-09-14T10:20:00Z",
  "completedAt": null,
  "failedAt": null,
  "providerReferenceId": "mock_f47ac10b",
  "failureReason": null,
  "idempotencyKey": "payout-req-2025-01-01-001"
}
```

---

## Backend Flow (Business Logic)

This summarizes the core payout logic enforced by the backend:

- Authorization: only restaurant owners can submit payout requests; staff can only read eligibility/history/details.
- Eligibility checks: payout method must exist, available balance must be positive, and the weekly cadence rule must be satisfied.
- Weekly cadence: a restaurant can request a payout once every 7 days, anchored to the most recent completed payout timestamp (or the latest requested timestamp if none have completed yet).
- Amount rules: if `amount` is omitted, the system uses the full available balance; otherwise the request must be > 0 and within available balance.
- Holds and settlement: requesting a payout places a hold against the account balance; on completion the hold is released and the balance is settled; on failure the hold is released with no settlement.
- Processing: after a request is created, the backend moves the payout to `Processing` and the mock provider completes or fails it asynchronously.
- Idempotency: providing the same `idempotencyKey` for the same restaurant returns the existing payout instead of creating a duplicate.

---

## Status Reference

`PayoutStatus` values
- `Requested`
- `Processing`
- `Completed`
- `Failed`
- `Canceled` (reserved for future use; no public API yet)
