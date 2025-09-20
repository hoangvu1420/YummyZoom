### **Introduction**

Welcome to the YummyZoom API. This API enables consumer apps and partner systems to browse restaurants and menus, place and manage orders, submit reviews, and power real-time updates.

This documentation is organized for external developers. It focuses on HTTP contracts and practical examples, not internal architecture.

### Base URL and Versioning

- All endpoints are versioned via URL segment.
- Base path: `/api/v1/`

Example:
```http
GET /api/v1/restaurants/search?q=pizza&pageNumber=1&pageSize=20
```

### Authentication

- Authentication is bearer-token based.
- Preferred flow is phone OTP; email/password endpoints are also available.
- Start with the Authentication guide: `./02-Authentication.md`

### Core Concepts

Common models and conventions used across the API (IDs, money, addresses, pagination, errors) are defined here: `./03-Core-Concepts.md`.

### Real-time API

Select features provide real-time updates via SignalR (e.g., order status, group cart changes). See: `./04-Real-time-Events-API.md`.

### SDKs & Tools

- REST-first: Any HTTP client works.
- OpenAPI: Navigate to `/api` on a running instance for interactive docs and client generation.

### Support & Limits

- Rate limiting and quotas: See `./Appendices/02-Rate-Limiting.md` (if applicable).
- Error semantics: See `./Appendices/01-Error-Codes.md`.

### Next Steps

1) Read `02-Authentication.md` to obtain a bearer token.
2) Follow `01-Getting-Started.md` for a quick end-to-end example.
3) Explore API references under `API-Reference/` for feature-specific endpoints.


