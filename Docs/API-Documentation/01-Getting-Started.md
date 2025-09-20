### **Getting Started**

This quick guide shows a minimal end-to-end flow: authenticate, then call a protected endpoint.

### Prerequisites

- A running YummyZoom API instance (see environment docs or `/api` Swagger UI)
- HTTP client (curl, Postman, Insomnia, or similar)

### 1) Authenticate (OTP flow)

Request an OTP (development returns the code in the response; production returns 202 and sends SMS):

```bash
curl -X POST https://localhost:5001/api/v1/users/auth/otp/request \
  -H 'Content-Type: application/json' \
  -d '{ "phoneNumber": "+15551234567" }'
```

Verify OTP to receive tokens:

```bash
curl -X POST https://localhost:5001/api/v1/users/auth/otp/verify \
  -H 'Content-Type: application/json' \
  -d '{ "phoneNumber": "+15551234567", "code": "123456" }'
```

Successful verification returns a JSON payload with:

```json
{
  "tokenType": "Bearer",
  "accessToken": "<jwt>",
  "expiresIn": 3600,
  "refreshToken": "<refresh-token>"
}
```

Save `accessToken` for subsequent requests.

### 2) Call a protected endpoint

Fetch your profile:

```bash
curl https://localhost:5001/api/v1/users/me \
  -H 'Authorization: Bearer <accessToken>'
```

You should receive your user profile JSON.

### 3) (Optional) Complete signup

If the API signals onboarding is required, complete signup:

```bash
curl -X POST https://localhost:5001/api/v1/users/auth/complete-signup \
  -H 'Authorization: Bearer <accessToken>' \
  -H 'Content-Type: application/json' \
  -d '{ "name": "Jane Doe", "email": "jane@example.com" }'
```

Check status:

```bash
curl https://localhost:5001/api/v1/users/auth/status \
  -H 'Authorization: Bearer <accessToken>'
```

### 4) Refresh tokens

When the access token expires, refresh it:

```bash
curl -X POST https://localhost:5001/api/v1/users/refresh \
  -H 'Content-Type: application/json' \
  -d '{ "refreshToken": "<refresh-token>" }'
``;

Response:

```json
{
  "tokenType": "Bearer",
  "accessToken": "<new-jwt>",
  "expiresIn": 3600,
  "refreshToken": "<new-refresh-token>"
}
```

### Next Steps

- Read `./02-Authentication.md` for full auth details.
- Review `./03-Core-Concepts.md` for common models and conventions.
- Explore feature endpoints in `./API-Reference/`.


