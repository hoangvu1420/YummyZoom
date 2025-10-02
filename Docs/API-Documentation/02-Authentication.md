### **Authentication**

This guide explains how clients authenticate with the YummyZoom API and obtain/refresh tokens. It reflects the real API endpoints and flows implemented in the service.

### Overview

- **Protocol**: Bearer tokens issued by ASP.NET Core Identity (BearerToken scheme).
- **Primary flow**: Phone-based One-Time Password (OTP) login.
- **Token endpoints**: Provided via Identity API. You will receive `accessToken` and `refreshToken` on successful sign-in.
- **Auth header**: Include the access token in every request.

```http
Authorization: Bearer <access_token>
```

### Phone OTP Authentication

Two public endpoints handle OTP-based sign-in. In development, the OTP code is returned in the response for convenience; in production, it is delivered via SMS and the API returns `202 Accepted`.

#### Request OTP

- **Method/Path**: `POST /api/v1/users/auth/otp/request`
- **Authorization**: Public
- **Body**
```json
{ "phoneNumber": "+15551234567" }
```
- **Responses**
  - 200 OK (development):
    ```json
    { "code": "123456" }
    ```
  - 202 Accepted (production): no body; code is sent via SMS
  - 400/500: Problem details on error

#### Verify OTP (Sign-In)

- **Method/Path**: `POST /api/v1/users/auth/otp/verify`
- **Authorization**: Public
- **Body**
```json
{ "phoneNumber": "+15551234567", "code": "123456" }
```
- **Success**
  - Returns a sign-in that emits bearer tokens (see Token Response below)
  - Also indicates onboarding flags in the application response model prior to token emission

### Token Response and Usage

On successful OTP verification, the server signs the user in with the `Bearer` scheme. The Identity bearer token handler returns a JSON payload containing access and refresh tokens. The fields follow the standard Identity bearer token shape:

```json
{
  "tokenType": "Bearer",
  "accessToken": "<jwt>",
  "expiresIn": 3600,
  "refreshToken": "<refresh-token>"
}
```

- **token_type**: Always `Bearer`.
- **access_token**: JWT used in the `Authorization` header.
- **expires_in**: Access token lifetime in seconds.
- **refresh_token**: Use to obtain a new access token when the current one expires.
- **Refresh lifetime**: Refresh tokens expire after 7 days.

Include the access token in all subsequent requests:

```http
GET /api/v1/users/me HTTP/1.1
Host: api.yummyzoom.com
Authorization: Bearer <access_token>
```

### Refreshing Tokens

The Identity API exposes a refresh endpoint. Send the `refreshToken` in the request body to obtain a new access token.

- **Method/Path**: `POST /api/v1/users/refresh`
- **Body**
```json
{ "refreshToken": "<refresh-token>" }
```
- **Response**
```json
{
  "tokenType": "Bearer",
  "accessToken": "<new-jwt>",
  "expiresIn": 3600,
  "refreshToken": "<new-refresh-token>"
}
```

Notes:
- If the refresh token is expired or invalid, the server returns `401 Unauthorized` or a problem response.

### Completing Signup (Onboarding)

After first OTP sign-in, the user may need to complete onboarding to create the platform profile.

- **Method/Path**: `POST /api/v1/users/auth/complete-signup`
- **Authorization**: Authenticated
- **Body**
```json
{ "name": "Jane Doe", "email": "jane@example.com" }
```
- **Success**: `200 OK`

You can check whether onboarding is required:

- **Method/Path**: `GET /api/v1/users/auth/status`
- **Authorization**: Authenticated
- **Response**
```json
{ "isNewUser": true, "requiresOnboarding": true }
```

### Set Password (OTP Users)

After signing in with OTP, users can optionally set a password to enable username/password login.

- **Method/Path**: `POST /api/v1/users/auth/set-password`
- **Authorization**: Authenticated (Bearer)
- **Body**
```json
{ "newPassword": "S3cret!" }
```
- **Success**: `204 No Content`
- **Notes**:
  - Only works if no password exists yet for the account. If a password is already set, the server returns a problem response indicating to use change-password instead.
  - Username remains the E.164 phone number (e.g., `+15551234567`) unless you update email/username.

### Identity Endpoints (Email/Password)

The API also exposes the standard Identity endpoints for email/password flows via the Identity API mapping. The typical endpoints include:

- `POST /api/v1/users/register` – Register with email and password
- `POST /api/v1/users/login` – Obtain bearer tokens using email/password
- `POST /api/v1/users/refresh` – Refresh tokens

Request/response bodies follow the same bearer token response shape shown above for successful sign-in.

#### Username rule and `/login` contract

- **Username equals phone**: In YummyZoom, the username is the phone number in E.164 format (e.g., `+15551234567`).
- **Library field naming**: The Identity API expects the username in a field named `email`. In our application, that field carries the username (phone number) by convention.
- **Password source**: OTP-created accounts have no password by default; use `POST /api/v1/users/auth/set-password` first to enable password login.

Endpoint details:

```http
POST /api/v1/users/login
```

Query parameters (optional):
- `useCookies`: boolean
- `useSessionCookies`: boolean

Request body (username provided in `email` field):
```json
{
  "email": "+15551234567",
  "password": "string"
}
```

Successful response (example):
```json
{
  "tokenType": "Bearer",
  "accessToken": "<jwt>",
  "expiresIn": 3600,
  "refreshToken": "<refresh-token>"
}
```

Notes:
- Treat the `email` property as the username. For OTP users, set it to the phone number in E.164 format.
- After setting a real email and optionally changing username rules, the same field will carry that email value.

### Authorization

- Most endpoints require authentication; some are public (e.g., OTP request and certain restaurant menu reads).
- Role- and resource-based permissions are enforced via claims. Clients should just provide a valid token; the server evaluates permissions.

### Error Responses

Authentication and authorization failures return standard problem details:

- **401 Unauthorized**
```json
{ "status": 401, "title": "Unauthorized", "type": "https://tools.ietf.org/html/rfc7235#section-3.1" }
```

- **403 Forbidden**
```json
{ "status": 403, "title": "Forbidden", "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3" }
```

Validation failures (e.g., malformed phone number) return `400 Bad Request` with validation details.

### Field Conventions

- All JSON uses `camelCase`.
- IDs are UUID strings.
- Timestamps are ISO 8601 UTC.

### Quick Start (OTP, Development)

1) Request code:
```bash
curl -X POST https://localhost:5001/api/v1/users/auth/otp/request \
  -H 'Content-Type: application/json' \
  -d '{ "phoneNumber": "+15551234567" }'
```

2) Verify code and get tokens:
```bash
curl -X POST https://localhost:5001/api/v1/users/auth/otp/verify \
  -H 'Content-Type: application/json' \
  -d '{ "phoneNumber": "+15551234567", "code": "123456" }'
```

3) Use access token:
```bash
curl https://localhost:5001/api/v1/users/me \
  -H 'Authorization: Bearer <access_token>'
```

### See Also

- Core Concepts: `./03-Core-Concepts.md`
- Real-time Events: `./04-Real-time-Events-API.md`
- Error Codes: `./Appendices/01-Error-Codes.md`


