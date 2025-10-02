# Authentication and Profile Management

This guide covers all customer authentication flows and profile management capabilities in the YummyZoom API.

## Overview

YummyZoom uses a **phone-first authentication approach** with optional password enhancement:

- **Primary onboarding**: Phone OTP (required)
- **Profile completion**: Name and email setup (required after first OTP)
- **Optional enhancement**: Password setup for dual login options
- **Ongoing access**: Choice between OTP or password login

All authenticated endpoints require a Bearer token in the Authorization header:

```http
Authorization: Bearer <access_token>
```

---

## Phone OTP Authentication

### Request OTP Code

Initiates the authentication flow by sending an OTP code to the provided phone number.

**`POST /api/v1/users/auth/otp/request`**

- **Authorization:** Public
- **Content-Type:** `application/json`

#### Request Body

```json
{
  "phoneNumber": "+15551234567"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `phoneNumber` | `string` | Yes | Phone number in any format (will be normalized to E.164) |

#### Responses

**✅ 200 OK** (Development environment)
```json
{
  "code": "123456"
}
```

**✅ 202 Accepted** (Production environment)
```json
{}
```
*In production, the OTP code is sent via SMS and not returned in the response.*

**❌ 400 Bad Request**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "phoneNumber": ["Phone number is required."]
  }
}
```

---

### Verify OTP Code

Verifies the OTP code and signs in the user (create placeholder user if not exists), returning bearer tokens.

**`POST /api/v1/users/auth/otp/verify`**

- **Authorization:** Public
- **Content-Type:** `application/json`

#### Request Body

```json
{
  "phoneNumber": "+15551234567",
  "code": "123456"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `phoneNumber` | `string` | Yes | The same phone number used for OTP request |
| `code` | `string` | Yes | 4-8 digit numeric OTP code received via SMS |

#### Responses

**✅ 200 OK**
```json
{
  "tokenType": "Bearer",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600,
  "refreshToken": "CfDJ8M7..."
}
```

| Field | Type | Description |
|-------|------|-------------|
| `tokenType` | `string` | Always "Bearer" |
| `accessToken` | `string` | JWT access token for API authentication |
| `expiresIn` | `number` | Access token lifetime in seconds |
| `refreshToken` | `string` | Token for refreshing access when expired |

**❌ 400 Bad Request**
```json
{
  "type": "validation",
  "title": "Validation Error",
  "status": 400,
  "errors": {
    "code": ["Code must be numeric."]
  }
}
```

**❌ 401 Unauthorized**
```json
{
  "type": "authentication",
  "title": "Invalid OTP",
  "status": 401,
  "detail": "The provided OTP code is invalid or expired."
}
```

---

## Post-Authentication Setup

### Complete Signup

**Required step** after first OTP verification to create the user profile in the system.

**`POST /api/v1/users/auth/complete-signup`**

- **Authorization:** Authenticated (Bearer token required)
- **Content-Type:** `application/json`

#### Request Body

```json
{
  "name": "Jane Doe",
  "email": "jane@example.com"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | `string` | Yes | User's full name (max 200 characters) |
| `email` | `string` | No | User's email address (must be valid email format if provided) |

#### Responses

**✅ 200 OK**
```json
{}
```

**❌ 400 Bad Request**
```json
{
  "type": "validation",
  "title": "Validation Error",
  "status": 400,
  "errors": {
    "name": ["Name is required."],
    "email": ["Email address is not valid."]
  }
}
```

---

### Check Authentication Status

Returns the user's authentication and onboarding status.

**`GET /api/v1/users/auth/status`**

- **Authorization:** Authenticated (Bearer token required)

#### Responses

**✅ 200 OK**
```json
{
  "isNewUser": false,
  "requiresOnboarding": false
}
```

| Field | Type | Description |
|-------|------|-------------|
| `isNewUser` | `boolean` | True if this is the user's first authentication |
| `requiresOnboarding` | `boolean` | True if the user needs to complete signup |

---

## Password Management

### Set Initial Password

**Optional step** that allows OTP users to set a password for alternative login method.

**`POST /api/v1/users/auth/set-password`**

- **Authorization:** Authenticated (Bearer token required)
- **Content-Type:** `application/json`

#### Request Body

```json
{
  "newPassword": "SecurePassword123!"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `newPassword` | `string` | Yes | Password (minimum 6 characters) |

#### Responses

**✅ 204 No Content**
No response body. Password has been set successfully.

**❌ 400 Bad Request**
```json
{
  "type": "validation",
  "title": "Validation Error",
  "status": 400,
  "errors": {
    "newPassword": ["Password must be at least 6 characters long."]
  }
}
```

**❌ 409 Conflict**
```json
{
  "type": "business",
  "title": "Password Already Set",
  "status": 409,
  "detail": "User already has a password. Use change-password endpoint to update it."
}
```

---

## Alternative Login (Password-Based)

### Login with Phone and Password

**Available after password is set.** Provides an alternative to OTP authentication.

**`POST /api/v1/users/login`**

- **Authorization:** Public
- **Content-Type:** `application/json`

#### Request Body

```json
{
  "email": "+15551234567",
  "password": "SecurePassword123!"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `email` | `string` | Yes | **Phone number in E.164 format** (not actual email) |
| `password` | `string` | Yes | User's password |

> **Note:** The field is named `email` for compatibility with ASP.NET Core Identity, but contains the phone number for YummyZoom users.

#### Responses

**✅ 200 OK**
```json
{
  "tokenType": "Bearer",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600,
  "refreshToken": "CfDJ8M7..."
}
```

Response format is identical to OTP verification response.

---

## Token Management

### Refresh Access Token

Obtains a new access token when the current one expires.

**`POST /api/v1/users/refresh`**

- **Authorization:** Public
- **Content-Type:** `application/json`

#### Request Body

```json
{
  "refreshToken": "CfDJ8M7..."
}
```

#### Responses

**✅ 200 OK**
```json
{
  "tokenType": "Bearer",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600,
  "refreshToken": "CfDJ8M8..."
}
```

**❌ 401 Unauthorized**
```json
{
  "type": "authentication",
  "title": "Invalid Refresh Token",
  "status": 401,
  "detail": "The refresh token is invalid or expired."
}
```

---

## Profile Management

### Get My Profile

Retrieves the authenticated user's profile information.

**`GET /api/v1/users/me`**

- **Authorization:** Authenticated (Bearer token required)

#### Responses

**✅ 200 OK**
```json
{
  "userId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "name": "Jane Doe",
  "email": "jane@example.com",
  "phoneNumber": "+15551234567",
  "address": {
    "addressId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "street": "123 Main Street",
    "city": "San Francisco",
    "state": "CA",
    "zipCode": "94105",
    "country": "USA",
    "label": "Home",
    "deliveryInstructions": "Leave at front door"
  }
}
```

| Field | Type | Description |
|-------|------|-------------|
| `userId` | `UUID` | Unique identifier for the user |
| `name` | `string` | User's full name |
| `email` | `string` | User's email address |
| `phoneNumber` | `string` | User's phone number in E.164 format |
| `address` | `object\|null` | Primary address information (null if not set) |

#### Address Object

| Field | Type | Description |
|-------|------|-------------|
| `addressId` | `UUID` | Unique identifier for the address |
| `street` | `string` | Street address |
| `city` | `string` | City name |
| `state` | `string\|null` | State/province (optional) |
| `zipCode` | `string` | Postal/ZIP code |
| `country` | `string` | Country name |
| `label` | `string\|null` | Address label (e.g., "Home", "Work") |
| `deliveryInstructions` | `string\|null` | Special delivery instructions |

---

### Update My Profile

Updates the authenticated user's name and email.

**`PUT /api/v1/users/me/profile`**

- **Authorization:** Authenticated (Bearer token required)
- **Content-Type:** `application/json`

#### Request Body

```json
{
  "name": "Jane Smith",
  "email": "jane.smith@example.com"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | `string` | Yes | User's full name (max 200 characters) |
| `email` | `string` | No | User's email address (must be valid email format if provided) |

#### Responses

**✅ 204 No Content**
No response body. Profile updated successfully.

---

## Address Management

### Create or Update Primary Address

Creates or updates the user's primary delivery address.

**`PUT /api/v1/users/me/address`**

- **Authorization:** Authenticated (Bearer token required)
- **Content-Type:** `application/json`

#### Request Body

```json
{
  "street": "456 Oak Avenue",
  "city": "Los Angeles",
  "state": "CA",
  "zipCode": "90210",
  "country": "USA",
  "label": "Home",
  "deliveryInstructions": "Ring doorbell twice"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `street` | `string` | Yes | Street address (max 255 characters) |
| `city` | `string` | Yes | City name (max 100 characters) |
| `state` | `string` | No | State/province (max 100 characters) |
| `zipCode` | `string` | Yes | Postal/ZIP code (max 20 characters) |
| `country` | `string` | Yes | Country name (max 100 characters) |
| `label` | `string` | No | Address label (max 100 characters) |
| `deliveryInstructions` | `string` | No | Special delivery instructions (max 500 characters) |

#### Responses

**✅ 200 OK**
```json
{
  "addressId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

| Field | Type | Description |
|-------|------|-------------|
| `addressId` | `UUID` | Unique identifier for the created/updated address |

---

## Device Management

### Register Device for Notifications

Registers a device token for push notifications.

**`POST /api/v1/users/devices/register`**

- **Authorization:** Authenticated (Bearer token required)
- **Content-Type:** `application/json`

#### Request Body

```json
{
  "fcmToken": "eHVyKJ8fQzG1b...",
  "platform": "iOS",
  "deviceId": "A1B2C3D4-E5F6-7890-ABCD-EF1234567890",
  "modelName": "iPhone 14 Pro"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `fcmToken` | `string` | Yes | Firebase Cloud Messaging token |
| `platform` | `string` | Yes | Device platform (e.g., "iOS", "Android", "Web") |
| `deviceId` | `string` | No | Unique device identifier |
| `modelName` | `string` | No | Device model name |

#### Responses

**✅ 204 No Content**
No response body. Device registered successfully.

---

### Unregister Device

Removes a previously registered device token.

**`POST /api/v1/users/devices/unregister`**

- **Authorization:** Authenticated (Bearer token required)
- **Content-Type:** `application/json`

#### Request Body

```json
{
  "fcmToken": "eHVyKJ8fQzG1b..."
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `fcmToken` | `string` | Yes | The FCM token to unregister |

#### Responses

**✅ 204 No Content**
No response body. Device unregistered successfully.

---

## Business Rules & Validations

### Authentication Flow Rules
- Phone OTP is the **required** onboarding method for all new users
- Password setting is **optional** and can be done anytime after OTP signup
- Users can choose between OTP or password login after password is set
- Refresh tokens expire after 7 days and must be renewed

### Profile Management Rules
- `name` field is required and cannot be empty
- `email` must be a valid email format when provided
- Phone number cannot be changed after account creation (contact support)

### Address Management Rules
- Only one primary address per user (upsert operation)
- `street`, `city`, `zipCode`, and `country` are required fields
- `state` is optional to accommodate international addresses

### Device Management Rules
- Multiple devices can be registered per user
- FCM tokens are unique identifiers for push notification delivery
- Unregistering a token stops notifications to that device

---

## Error Handling

All endpoints follow standard HTTP status codes and return problem details for errors:

### Common Error Responses

**400 Bad Request** - Validation errors
```json
{
  "type": "validation",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "fieldName": ["Error message"]
  }
}
```

**401 Unauthorized** - Authentication required or failed
```json
{
  "type": "authentication",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Valid authentication credentials are required."
}
```

**409 Conflict** - Business rule violation
```json
{
  "type": "business",
  "title": "Conflict",
  "status": 409,
  "detail": "The requested operation conflicts with the current state."
}
```

---

## Complete Onboarding Workflow

Here's the typical flow for a new user:

1. **Request OTP:** `POST /auth/otp/request`
2. **Verify OTP:** `POST /auth/otp/verify` → Receive tokens
3. **Complete Signup:** `POST /auth/complete-signup` → Create profile
4. **Update Profile:** `PUT /me/profile` → Add/update details
5. **Add Address:** `PUT /me/address` → Set delivery address
6. **Register Device:** `POST /devices/register` → Enable notifications
7. **Optional:** `POST /auth/set-password` → Enable password login

After this flow, users can authenticate using either OTP or password and have a complete profile ready for ordering.