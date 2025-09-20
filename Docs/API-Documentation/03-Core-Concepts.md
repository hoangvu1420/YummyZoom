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

See the Error Codes appendix for a categorized list of errors and meanings.

### Request/Response Style

- **No global envelope**: Endpoints return the resource or result directly. Collections are returned via the Pagination shape above.
- **IDs**: Always UUID strings in both request and response bodies.
- **Dates**: Always ISO 8601 UTC strings.

### Cross-References

- Authentication: `./02-Authentication.md`
- Real-time events: `./04-Real-time-Events-API.md`
- Error codes: `./Appendices/01-Error-Codes.md`


