### **Core Concepts**

This document defines the universal, reusable models and conventions used across the YummyZoom REST API. Read this before any endpoint-specific docs.

### JSON Conventions

- **Naming**: All JSON fields use `camelCase`.
- **Dates and times**: ISO 8601 UTC strings (e.g., `2025-09-20T10:00:00Z`).
- **Enums**: Serialized as strings (e.g., `"Preparing"`).
- **IDs**: All identifiers are UUID strings. Strongly-typed IDs used internally are serialized as primitives in the API.
- **Numbers**: Monetary values use decimals; do not treat them as floats client-side.

### Standard Models

#### Money

Represents a currency amount.

- **Fields**
  - `amount` (number): Decimal amount with two fractional digits.
  - `currency` (string): 3-letter ISO 4217 currency code (e.g., `"USD"`).

Example:
```json
{
  "amount": 55.50,
  "currency": "USD"
}
```

Notes:
- Server-side amounts are rounded to two decimals to ensure consistency.

#### Address

Reusable postal address object.

- **Fields**
  - `street` (string)
  - `city` (string)
  - `state` (string)
  - `zipCode` (string)
  - `country` (string)
  - `label` (string, optional): Friendly tag such as "Home" or "Work".
  - `deliveryInstructions` (string, optional)

Example:
```json
{
  "street": "123 Foodie Lane",
  "city": "Tastytown",
  "state": "CA",
  "zipCode": "90210",
  "country": "USA",
  "label": "Home",
  "deliveryInstructions": "Leave at the front door."
}
```

### Pagination

List endpoints return a consistent paged shape. Query parameters typically include `pageNumber` and `pageSize` (endpoint-specific defaults are documented on each endpoint).

Response shape:
```json
{
  "items": [ /* array of results */ ],
  "pageNumber": 1,
  "totalPages": 10,
  "totalCount": 100
}
```

- **items**: Array containing the current page of results.
- **pageNumber**: 1-based page index.
- **totalPages**: Total pages given the current `pageSize`.
- **totalCount**: Total number of matching records.

### Idempotency

Critical mutation endpoints support idempotency to prevent duplicate operations from network retries or client errors.

#### Usage

Send an `Idempotency-Key` header with a unique UUID v4 identifier:

```http
POST /api/v1/orders/initiate
Authorization: Bearer <token>
Idempotency-Key: 550e8400-e29b-41d4-a716-446655440000
Content-Type: application/json

{
  "customerId": "...",
  "restaurantId": "..."
}
```

#### Behavior

- **First Request**: Processes normally and caches the successful response
- **Duplicate Requests**: Returns the cached response without re-execution
- **Cache Duration**: 5 minutes after successful completion
- **Scope**: Keys are scoped per user and command type for security

#### Supported Endpoints

- `POST /api/v1/orders/initiate` - Prevent duplicate order creation
- `POST /api/v1/team-carts` - Prevent duplicate team cart creation
- `POST /api/v1/team-carts/{id}/items` - Prevent duplicate item additions
- `POST /api/v1/team-carts/{id}/lock` - Prevent duplicate cart locking
- `POST /api/v1/team-carts/{id}/convert` - Prevent duplicate order conversions

#### Requirements

- Must be a valid UUID v4 format (e.g., `xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx`)
- Should be unique per logical operation
- Optional but recommended for all supported endpoints

### Error Handling

Errors follow RFC 7807 Problem Details. Validation errors use the `errors` dictionary.

Common status codes:
- **400 Bad Request**: Validation or request issues.
- **401 Unauthorized**: Missing/invalid credentials.
- **403 Forbidden**: Authenticated but not permitted.
- **404 Not Found**: Resource does not exist.
- **500 Internal Server Error**: Unhandled error.

ProblemDetails example:
```json
{
  "status": 404,
  "title": "The specified resource was not found.",
  "detail": "Restaurant with ID '...' not found.",
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4"
}
```

ValidationProblemDetails example:
```json
{
  "status": 400,
  "title": "One or more validation errors occurred.",
  "detail": "fieldA: must not be empty | fieldB: invalid value",
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "errors": {
    "fieldA": ["must not be empty"],
    "fieldB": ["invalid value"]
  }
}
```

#### ProblemDetails conventions in YummyZoom

We use RFC 7807 Problem Details for errors. In addition to the standard fields:

- Title: carries the machine-readable error “code” from our Result pattern (e.g., `MenuItem.Invalid`, `Authentication.OtpInvalid`). Clients SHOULD key logic on Title.
- Detail: human-readable description suitable for display/logging.
- Type: RFC reference URI indicating the general HTTP semantics (e.g., `https://tools.ietf.org/html/rfc7231#section-6.5.1`), not a product-specific taxonomy.
- Status: HTTP status code.
- traceId: distributed trace identifier for diagnostics (when available).

Example (business error returned via CustomResults):
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "MenuItem.Invalid",
  "status": 400,
  "detail": "Invalid",
  "traceId": "00-056eecec5619689e167cfbb47d79f031-80783a52973989bd-01"
}
```

Notes:
- Titles are stable and versioned by domain (e.g., `Authentication.OtpExpired`), enabling deterministic client handling.
- Validation errors use `ValidationProblemDetails` and include an `errors` dictionary as shown above.
- We may later add a separate `errorCode` field for telemetry; Title remains the canonical machine code.

See the Error Codes appendix for a categorized list of errors and meanings.

### Request/Response Style

- **No global envelope**: Endpoints return the resource or result directly. Collections are returned via the Pagination shape above.
- **IDs**: Always UUID strings in both request and response bodies.
- **Dates**: Always ISO 8601 UTC strings.

### Cross-References

- Authentication: `./02-Authentication.md`
- Real-time events: `./04-Real-time-Events-API.md`
- Error codes: `./Appendices/01-Error-Codes.md`
