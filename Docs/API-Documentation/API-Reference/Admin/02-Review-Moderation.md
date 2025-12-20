# Admin Review Moderation

Base path: `/api/v1/`

Endpoints for listing, inspecting, and moderating customer reviews.

---

## GET /admin/reviews

Return a paginated list of reviews for moderation.

- Authorization: Admin

### Request

#### Query Parameters

| Parameter | Type | Description | Default |
| --- | --- | --- | --- |
| `pageNumber` | integer | Page number (1-based). | `1` |
| `pageSize` | integer | Page size. | `25` |
| `isModerated` | boolean | Filter by moderation status. | `null` |
| `isHidden` | boolean | Filter by visibility. | `null` |
| `minRating` | integer | Minimum rating (inclusive). | `null` |
| `maxRating` | integer | Maximum rating (inclusive). | `null` |
| `hasTextOnly` | boolean | When true, return reviews with non-empty comments only. | `null` |
| `restaurantId` | UUID | Filter by restaurant. | `null` |
| `fromUtc` | string (date-time) | ISO 8601 start timestamp (inclusive). | `null` |
| `toUtc` | string (date-time) | ISO 8601 end timestamp (inclusive). | `null` |
| `search` | string | Case-insensitive search across comment and restaurant name. | `null` |
| `sortBy` | string | Sort option. | `Newest` |

Sort options for `sortBy`:

- `Newest`
- `HighestRating`
- `LowestRating`

### Response

#### 200 OK

```json
{
  "items": [
    {
      "reviewId": "7c33bdfe-f6c8-4a0e-8cc2-7a49915b30a9",
      "restaurantId": "6bdb6aa7-2b76-4bb2-9c16-7a9f1ce0df9f",
      "restaurantName": "Pasta Palace",
      "customerId": "6f6f19d2-b8c1-4c7c-8b36-1b9e9274c83d",
      "rating": 4,
      "comment": "Great food.",
      "submissionTimestamp": "2024-12-18T12:03:00Z",
      "isModerated": false,
      "isHidden": false,
      "lastActionAtUtc": null
    }
  ],
  "pageNumber": 1,
  "totalPages": 20,
  "totalCount": 500,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

Field descriptions

| Field | Type | Description |
| --- | --- | --- |
| `items` | array | Page of reviews for moderation. |
| `items[].reviewId` | UUID | Review identifier. |
| `items[].restaurantId` | UUID | Restaurant identifier. |
| `items[].restaurantName` | string | Restaurant name. |
| `items[].customerId` | UUID | Customer identifier. |
| `items[].rating` | integer | Rating value. |
| `items[].comment` | string or null | Review comment text. |
| `items[].submissionTimestamp` | string | ISO 8601 submission timestamp. |
| `items[].isModerated` | boolean | Whether the review is marked as moderated. |
| `items[].isHidden` | boolean | Whether the review is hidden from public visibility. |
| `items[].lastActionAtUtc` | string or null | ISO 8601 timestamp of last moderation action, if available. |
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

## GET /admin/reviews/{reviewId}

Return detailed information about a review.

- Authorization: Admin

### Request

#### Path Parameters

| Parameter | Type | Description |
| --- | --- | --- |
| `reviewId` | UUID | Review identifier. |

### Response

#### 200 OK

```json
{
  "reviewId": "7c33bdfe-f6c8-4a0e-8cc2-7a49915b30a9",
  "restaurantId": "6bdb6aa7-2b76-4bb2-9c16-7a9f1ce0df9f",
  "restaurantName": "Pasta Palace",
  "customerId": "6f6f19d2-b8c1-4c7c-8b36-1b9e9274c83d",
  "rating": 4,
  "comment": "Great food.",
  "reply": null,
  "orderId": "cf5f6ed0-9c4b-4e98-a037-6ad1e2c5f5d0",
  "restaurantAverageRating": 4.6,
  "restaurantTotalReviews": 1830,
  "submissionTimestamp": "2024-12-18T12:03:00Z",
  "isModerated": false,
  "isHidden": false,
  "lastActionAtUtc": null
}
```

Field descriptions

| Field | Type | Description |
| --- | --- | --- |
| `reviewId` | UUID | Review identifier. |
| `restaurantId` | UUID | Restaurant identifier. |
| `restaurantName` | string | Restaurant name. |
| `customerId` | UUID | Customer identifier. |
| `rating` | integer | Rating value. |
| `comment` | string or null | Review comment text. |
| `reply` | string or null | Restaurant reply text, if present. |
| `orderId` | UUID or null | Related order identifier, if present. |
| `restaurantAverageRating` | number or null | Restaurant average rating at time of request. |
| `restaurantTotalReviews` | integer or null | Restaurant total review count at time of request. |
| `submissionTimestamp` | string | ISO 8601 submission timestamp. |
| `isModerated` | boolean | Whether the review is marked as moderated. |
| `isHidden` | boolean | Whether the review is hidden from public visibility. |
| `lastActionAtUtc` | string or null | ISO 8601 timestamp of last moderation action, if available. |

#### Error Responses

- 401 Unauthorized
- 403 Forbidden
- 404 Not Found
- 500 Internal Server Error

---

## GET /admin/reviews/{reviewId}/audit

Return the moderation audit trail for a review.

- Authorization: Admin

### Request

#### Path Parameters

| Parameter | Type | Description |
| --- | --- | --- |
| `reviewId` | UUID | Review identifier. |

### Response

#### 200 OK

```json
[
  {
    "action": "Moderated",
    "reason": "Spam",
    "actorUserId": "d2c07cd6-80ef-4893-8b9d-2d5bfb7b29ef",
    "actorDisplayName": "Admin User",
    "timestampUtc": "2024-12-18T13:10:00Z"
  }
]
```

Field descriptions

| Field | Type | Description |
| --- | --- | --- |
| `action` | string | Moderation action name. |
| `reason` | string or null | Optional moderation reason. |
| `actorUserId` | UUID | Admin user who performed the action. |
| `actorDisplayName` | string or null | Display name of the admin user, if available. |
| `timestampUtc` | string | ISO 8601 timestamp of the action. |

#### Error Responses

- 401 Unauthorized
- 403 Forbidden
- 404 Not Found
- 500 Internal Server Error

---

## POST /admin/reviews/{reviewId}/moderate

Mark a review as moderated.

- Authorization: Admin

### Request

#### Path Parameters

| Parameter | Type | Description |
| --- | --- | --- |
| `reviewId` | UUID | Review identifier. |

#### Request Body

```json
{
  "reason": "Off-topic content"
}
```

Field descriptions

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `reason` | string | No | Optional reason for moderation. Max 500 characters, cannot be whitespace-only. |

### Response

- 204 No Content

#### Error Responses

- 400 Bad Request
- 401 Unauthorized
- 403 Forbidden
- 404 Not Found
- 500 Internal Server Error

### Business Rules & Validations

- `reviewId` must reference an existing review.
- If provided, `reason` must not be whitespace and must be <= 500 characters.

---

## POST /admin/reviews/{reviewId}/hide

Hide a review from public visibility.

- Authorization: Admin

### Request

#### Path Parameters

| Parameter | Type | Description |
| --- | --- | --- |
| `reviewId` | UUID | Review identifier. |

#### Request Body

```json
{
  "reason": "Harassment"
}
```

Field descriptions

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `reason` | string | No | Optional reason for hiding. Max 500 characters, cannot be whitespace-only. |

### Response

- 204 No Content

#### Error Responses

- 400 Bad Request
- 401 Unauthorized
- 403 Forbidden
- 404 Not Found
- 500 Internal Server Error

### Business Rules & Validations

- `reviewId` must reference an existing review.
- If provided, `reason` must not be whitespace and must be <= 500 characters.

---

## POST /admin/reviews/{reviewId}/show

Show a previously hidden review.

- Authorization: Admin

### Request

#### Path Parameters

| Parameter | Type | Description |
| --- | --- | --- |
| `reviewId` | UUID | Review identifier. |

#### Request Body

```json
{
  "reason": "Appeal accepted"
}
```

Field descriptions

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `reason` | string | No | Optional reason for showing. Max 500 characters, cannot be whitespace-only. |

### Response

- 204 No Content

#### Error Responses

- 400 Bad Request
- 401 Unauthorized
- 403 Forbidden
- 404 Not Found
- 500 Internal Server Error

### Business Rules & Validations

- `reviewId` must reference an existing review.
- If provided, `reason` must not be whitespace and must be <= 500 characters.
