# Admin Restaurant Registrations

Base path: `/api/v1/`

Endpoints for reviewing and approving or rejecting restaurant registration requests.

---

## POST /restaurant-registrations

Submit a restaurant registration for admin review.

- Authorization: Authenticated User (policy `CompletedSignup`)

### Request

#### Request Body

```json
{
  "name": "Pasta Palace",
  "description": "Fresh handmade pasta.",
  "cuisineType": "Italian",
  "street": "123 Market St",
  "city": "San Francisco",
  "state": "CA",
  "zipCode": "94105",
  "country": "USA",
  "phoneNumber": "+1 (415) 555-2121",
  "email": "contact@pastapalace.example",
  "businessHours": "09:00-17:30",
  "logoUrl": "https://cdn.example.com/pasta/logo.png",
  "latitude": 37.7936,
  "longitude": -122.3965
}
```

Field descriptions

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `name` | string | Yes | Restaurant name. Max 100 characters. |
| `description` | string | Yes | Restaurant description. Max 500 characters. |
| `cuisineType` | string | Yes | Cuisine type. Max 50 characters. |
| `street` | string | Yes | Street address. Max 200 characters. |
| `city` | string | Yes | City. Max 100 characters. |
| `state` | string | Yes | State/region. Max 100 characters. |
| `zipCode` | string | Yes | Postal/ZIP code. Max 20 characters. |
| `country` | string | Yes | Country. Max 100 characters. |
| `phoneNumber` | string | Yes | Contact phone number. Max 30 characters. |
| `email` | string | Yes | Contact email address (must be valid email format). |
| `businessHours` | string | Yes | Business hours string. Max 200 characters. |
| `logoUrl` | string | No | Optional logo URL (must be absolute URL if provided). |
| `latitude` | number | No | Optional latitude in range [-90, 90]. |
| `longitude` | number | No | Optional longitude in range [-180, 180]. |

### Response

#### 201 Created

```json
{
  "registrationId": "b64f47b6-61a6-4490-89c7-2cc5bfdb8c3c"
}
```

Field descriptions

| Field | Type | Description |
| --- | --- | --- |
| `registrationId` | UUID | Newly created registration identifier. |

#### Error Responses

- 400 Bad Request
- 401 Unauthorized
- 403 Forbidden
- 409 Conflict
- 500 Internal Server Error

### Business Rules & Validations

- All required fields must be provided and non-empty.
- `logoUrl` must be a valid absolute URL if supplied.
- `latitude` and `longitude` must be within valid ranges if supplied.

---

## GET /restaurant-registrations/admin/pending

Return a paginated list of pending restaurant registrations.

- Authorization: Admin

### Request

#### Query Parameters

| Parameter | Type | Description | Default |
| --- | --- | --- | --- |
| `pageNumber` | integer | Page number (1-based). | `1` |
| `pageSize` | integer | Page size. | `10` |

### Response

#### 200 OK

```json
{
  "items": [
    {
      "registrationId": "b64f47b6-61a6-4490-89c7-2cc5bfdb8c3c",
      "name": "Pasta Palace",
      "city": "San Francisco",
      "status": "Pending",
      "submittedAtUtc": "2024-12-18T10:02:00Z",
      "reviewedAtUtc": null,
      "reviewNote": null,
      "submitterUserId": "4f7edb91-7710-4c3a-b737-60e3a8a6a7af"
    }
  ],
  "pageNumber": 1,
  "totalPages": 3,
  "totalCount": 25,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

Field descriptions

| Field | Type | Description |
| --- | --- | --- |
| `items` | array | Page of pending registrations. |
| `items[].registrationId` | UUID | Registration identifier. |
| `items[].name` | string | Submitted restaurant name. |
| `items[].city` | string | Submitted city. |
| `items[].status` | string | Registration status: `Pending`, `Approved`, or `Rejected`. |
| `items[].submittedAtUtc` | string | ISO 8601 timestamp when submitted. |
| `items[].reviewedAtUtc` | string or null | ISO 8601 timestamp when reviewed, if any. |
| `items[].reviewNote` | string or null | Review note entered by admin, if any. |
| `items[].submitterUserId` | UUID | User who submitted the registration. |
| `pageNumber` | integer | Current page number. |
| `totalPages` | integer | Total number of pages. |
| `totalCount` | integer | Total record count. |
| `hasPreviousPage` | boolean | Whether a previous page exists. |
| `hasNextPage` | boolean | Whether a next page exists. |

#### Error Responses

- 400 Bad Request
- 401 Unauthorized
- 403 Forbidden
- 500 Internal Server Error

---

## GET /restaurant-registrations/admin/{registrationId}

Return the full details of a pending restaurant registration.

- Authorization: Admin

### Request

#### Path Parameters

| Parameter | Type | Description |
| --- | --- | --- |
| `registrationId` | UUID | Registration identifier. |

### Response

#### 200 OK

```json
{
  "registrationId": "b64f47b6-61a6-4490-89c7-2cc5bfdb8c3c",
  "name": "Pasta Palace",
  "description": "Fresh handmade pasta.",
  "cuisineType": "Italian",
  "street": "123 Market St",
  "city": "San Francisco",
  "state": "CA",
  "zipCode": "94105",
  "country": "USA",
  "phoneNumber": "+1 (415) 555-2121",
  "email": "contact@pastapalace.example",
  "businessHours": "09:00-17:30",
  "logoUrl": "https://cdn.example.com/pasta/logo.png",
  "latitude": 37.7936,
  "longitude": -122.3965,
  "status": "Pending",
  "submittedAtUtc": "2024-12-18T10:02:00Z",
  "reviewedAtUtc": null,
  "reviewNote": null,
  "submitterUserId": "4f7edb91-7710-4c3a-b737-60e3a8a6a7af",
  "submitterName": "Jane Nguyen",
  "reviewedByUserId": null
}
```

Field descriptions

| Field | Type | Description |
| --- | --- | --- |
| `registrationId` | UUID | Registration identifier. |
| `name` | string | Submitted restaurant name. |
| `description` | string | Submitted description. |
| `cuisineType` | string | Submitted cuisine type. |
| `street` | string | Submitted street address. |
| `city` | string | Submitted city. |
| `state` | string | Submitted state/region. |
| `zipCode` | string | Submitted postal/ZIP code. |
| `country` | string | Submitted country. |
| `phoneNumber` | string | Submitted contact phone number. |
| `email` | string | Submitted contact email address. |
| `businessHours` | string | Submitted business hours. |
| `logoUrl` | string or null | Submitted logo URL if provided. |
| `latitude` | number or null | Submitted latitude if provided. |
| `longitude` | number or null | Submitted longitude if provided. |
| `status` | string | Registration status (always `Pending`). |
| `submittedAtUtc` | string | ISO 8601 timestamp when submitted. |
| `reviewedAtUtc` | string or null | ISO 8601 timestamp when reviewed, if any. |
| `reviewNote` | string or null | Review note, if any. |
| `submitterUserId` | UUID | User who submitted the registration. |
| `submitterName` | string | Name of the user who submitted the registration. |
| `reviewedByUserId` | UUID or null | Admin user who reviewed the registration, if any. |

#### Error Responses

- 401 Unauthorized
- 403 Forbidden
- 404 Not Found
- 409 Conflict — registration exists but is not pending.
- 500 Internal Server Error

### Business Rules & Validations

- Only pending registrations can be retrieved. Non-pending registrations return a conflict.

---

## POST /restaurant-registrations/admin/{registrationId}/approve

Approve a restaurant registration and provision the restaurant account.

- Authorization: Admin

### Request

#### Path Parameters

| Parameter | Type | Description |
| --- | --- | --- |
| `registrationId` | UUID | Registration identifier. |

#### Request Body

```json
{
  "note": "Verified business license."
}
```

Field descriptions

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `note` | string | No | Optional review note. Max 500 characters. |

### Response

#### 200 OK

```json
{
  "restaurantId": "6bdb6aa7-2b76-4bb2-9c16-7a9f1ce0df9f"
}
```

Field descriptions

| Field | Type | Description |
| --- | --- | --- |
| `restaurantId` | UUID | Newly provisioned restaurant identifier. |

#### Error Responses

- 400 Bad Request
- 401 Unauthorized
- 403 Forbidden
- 404 Not Found
- 409 Conflict
- 500 Internal Server Error

### Business Rules & Validations

- `registrationId` must reference a pending registration.
- If provided, `note` must be <= 500 characters.

---

## POST /restaurant-registrations/admin/{registrationId}/reject

Reject a restaurant registration with a reason.

- Authorization: Admin

### Request

#### Path Parameters

| Parameter | Type | Description |
| --- | --- | --- |
| `registrationId` | UUID | Registration identifier. |

#### Request Body

```json
{
  "reason": "Missing required documentation."
}
```

Field descriptions

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `reason` | string | Yes | Rejection reason. Max 500 characters. |

### Response

- 204 No Content

#### Error Responses

- 400 Bad Request
- 401 Unauthorized
- 403 Forbidden
- 404 Not Found
- 409 Conflict
- 500 Internal Server Error

### Business Rules & Validations

- `registrationId` must reference a pending registration.
- `reason` is required and must be <= 500 characters.
