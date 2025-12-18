# Restaurant Profile & Operations (Provider)

Base path: `/api/v1/`

These endpoints let authorized restaurant staff manage the restaurant profile, business hours, address/geolocation, and the accepting‑orders operational switch.

Cross‑refs: See Authentication (Docs/API-Documentation/02-Authentication.md) for obtaining a JWT. Public restaurant info and reviews are documented under Customer reference.

---

## PUT /restaurants/{restaurantId}/profile

Update basic profile attributes: name, description, logo URL, and/or contact info (phone, email).

- Authorization: Must be authenticated and satisfy policy `MustBeRestaurantStaff` for the specified `restaurantId`.

### Request

#### Path Parameters

| Parameter        | Type  | Description                                    |
| ---------------- | ----- | ---------------------------------------------- |
| `restaurantId`   | UUID  | The restaurant being managed.                  |

#### Request Body (at least one field required)

```json
{
  "name": "Pasta Palace",
  "description": "Fresh handmade pasta.",
  "logoUrl": "https://cdn.example.com/pasta/logo.png",
  "phone": "+1 (415) 555-2121",
  "email": "contact@pastapalace.example"
}
```

Field notes and limits

| Field        | Type     | Required | Rules                                                                                   |
| ------------ | -------- | -------- | --------------------------------------------------------------------------------------- |
| `name`       | string   | No       | Max 100 chars; not empty if provided.                                                   |
| `description`| string   | No       | Max 500 chars; not empty if provided.                                                   |
| `logoUrl`    | string   | No       | Optional; if provided must be valid http(s) URL.                                        |
| `phone`      | string   | No       | If provided, basic phone format; min length ~10; digits/space/`()+-.` allowed.          |
| `email`      | string   | No       | If provided, basic email pattern `local@domain.tld`.                                    |

At least one of the fields above must be provided; otherwise validation fails.

### Response

- 204 No Content — update applied.

#### Error Responses

- 400 Validation — example keys:
  - `Restaurant.NameTooLong`, `Restaurant.DescriptionTooLong`, `Restaurant.InvalidLogoUrl`
  - `Restaurant.Contact.PhoneInvalidFormat`, `Restaurant.Contact.EmailInvalidFormat`
  - "At least one field must be provided" (validator)
- 401 Unauthorized — missing/invalid JWT.
- 403 Forbidden — caller is not staff/owner of `restaurantId`.
- 404 Not Found — `Restaurant.NotFound`.

### Business Rules & Validations

- Partial updates: only provided fields are changed; others remain as-is.
- Contact updates co-validate phone/email together; omitted values default to current persisted values before validation.
- Name, description, and logo validations follow the Restaurant aggregate constraints (length caps, URL pattern).

---

## PUT /restaurants/{restaurantId}/business-hours

Update the restaurant’s business hours string.

- Authorization: `MustBeRestaurantStaff`.

### Request

#### Path Parameters

| Parameter      | Type | Description |
| -------------- | ---- | ----------- |
| `restaurantId` | UUID | Target restaurant. |

#### Request Body

```json
{
  "businessHours": "09:00-17:30"
}
```

Field notes

| Field            | Type   | Required | Rules                                                                                         |
| ---------------- | ------ | -------- | --------------------------------------------------------------------------------------------- |
| `businessHours`  | string | Yes      | Format `HH:mm-HH:mm` (24h); start < end; max 200 chars; trimmed and validated in domain.      |

### Response

- 204 No Content — update applied.

#### Error Responses

- 400 Validation — `Restaurant.BusinessHours.InvalidFormat`, `Restaurant.BusinessHours.FormatTooLong`, or required errors.
- 401 Unauthorized — missing/invalid JWT.
- 403 Forbidden — not staff/owner.
- 404 Not Found — `Restaurant.NotFound`.

### Business Rules & Validations

- Hours string must match domain validator (same-day window; exact `HH:mm-HH:mm`).
- Emits `RestaurantBusinessHoursChanged` domain event on success.

---

## PUT /restaurants/{restaurantId}/location

Update address fields and, optionally, geolocation coordinates.

- Authorization: `MustBeRestaurantStaff`.

### Request

#### Path Parameters

| Parameter      | Type | Description |
| -------------- | ---- | ----------- |
| `restaurantId` | UUID | Target restaurant. |

#### Request Body

```json
{
  "street": "123 Market St",
  "city": "San Francisco",
  "state": "CA",
  "zipCode": "94105",
  "country": "USA",
  "latitude": 37.7936,
  "longitude": -122.3965
}
```

Field notes and limits

| Field       | Type    | Required | Rules                                                                                  |
| ----------- | ------- | -------- | -------------------------------------------------------------------------------------- |
| `street`    | string  | Yes      | Not empty; max 200 chars (API validator) / 100 chars (domain address cap used).        |
| `city`      | string  | Yes      | Not empty; max 100.                                                                    |
| `state`     | string  | Yes      | Not empty; max 100.                                                                    |
| `zipCode`   | string  | Yes      | Not empty; max 20.                                                                     |
| `country`   | string  | Yes      | Not empty; max 100.                                                                    |
| `latitude`  | number  | No       | Optional; must be provided together with `longitude`; range [-90, 90].                 |
| `longitude` | number  | No       | Optional; must be provided together with `latitude`; range [-180, 180].                |

If only address is provided, geocoordinates remain unchanged. If both `latitude` and `longitude` are provided, coordinates are updated.

### Response

- 204 No Content — update applied.

#### Error Responses

- 400 Validation — address field caps; latitude/longitude out-of-range; required fields missing.
- 401 Unauthorized — missing/invalid JWT.
- 403 Forbidden — not staff/owner.
- 404 Not Found — `Restaurant.NotFound`.

### Business Rules & Validations

- Address updates validated via domain `Address.Create` (non-empty fields, length caps).
- Geo updates validated via domain `GeoCoordinates` (inclusive ranges). If provided, both lat & lng must be present.
- Emits `RestaurantLocationChanged` and/or `RestaurantGeoCoordinatesChanged` domain events.

---

## PUT /restaurants/{restaurantId}/accepting-orders

Toggle whether the restaurant is currently accepting orders.

- Authorization: `MustBeRestaurantStaff`.
- Full path: `PUT /api/v1/restaurants/{restaurantId}/accepting-orders`.

### Request

#### Path Parameters

| Parameter      | Type | Description |
| -------------- | ---- | ----------- |
| `restaurantId` | UUID | Target restaurant. |

#### Request Body

```json
{
  "isAccepting": true
}
```

Field notes

| Field         | Type    | Required | Description |
| ------------- | ------- | -------- | ----------- |
| `isAccepting` | boolean | Yes      | `true` to accept new orders; `false` to stop accepting new orders. |

Note: This endpoint only toggles the accepting-orders switch. To update address/geolocation, use `PUT /restaurants/{restaurantId}/location`.

### Response

#### 200 OK

```json
{
  "isAccepting": true
}
```

#### Error Responses

- 401 Unauthorized — missing/invalid JWT.
- 403 Forbidden — not staff/owner.
- 404 Not Found — `Restaurant.SetAcceptingOrders.NotFound`.

### Business Rules & Validations

- When enabled, emits `RestaurantAcceptingOrders`; when disabled, emits `RestaurantNotAcceptingOrders`.
- `IsActive()` for a restaurant is `IsVerified && IsAcceptingOrders`; verification is typically established during onboarding/provisioning.

---

## Common Notes

- Rate limiting and error envelope: see platform conventions and Appendices/01-Error-Codes (to be populated) for standard error object structure and codes.
- Versioning: All examples use `/api/v1/` as the base path.
- Security: All endpoints above require a valid bearer token and restaurant membership with role Owner or Staff to satisfy `MustBeRestaurantStaff`.
