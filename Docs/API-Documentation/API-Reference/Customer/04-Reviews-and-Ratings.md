# Reviews & Ratings

This guide covers all APIs for customers to create, view, and manage reviews for restaurants. Public review browsing is also summarized here for completeness.

## Overview

YummyZoom supports a simple, privacy-conscious review system:

- Create a review for a delivered order at a restaurant
- View and manage your own reviews
- Browse public reviews and rating summaries per restaurant
- Aggregated rating data for restaurant listings and detail pages

Unless noted, review write actions require authentication.

---

## Create a Review

Creates a new review for a completed order at the specified restaurant.

**`POST /api/v1/restaurants/{restaurantId}/reviews`**

- **Authorization:** Required (Customer with CompletedSignup)

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `restaurantId` | `UUID` | Yes | Restaurant receiving the review |

#### Request Body

```json
{
  "orderId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "rating": 5,
  "title": "Great dinner!",
  "comment": "Delicious pasta and friendly staff."
}
```

#### Request Schema

| Field | Type | Required | Rules |
|-------|------|----------|-------|
| `orderId` | `UUID` | Yes | Must be an order owned by the authenticated user and belong to the same `restaurantId`. Order must be `Delivered`. |
| `rating` | `integer` | Yes | 1–5 inclusive. |
| `title` | `string` | No | Max 100 chars. Currently reserved; not returned in responses. |
| `comment` | `string` | No | Max 1000 chars. |

#### Response

**✓ 201 Created**
```json
{
  "reviewId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

#### Error Responses

- 400 Bad Request
  - `CreateReview.NotOrderOwner` — You do not own this order.
  - `CreateReview.RestaurantMismatch` — Order does not belong to the specified restaurant.
  - `CreateReview.InvalidOrderStatusForReview` — Order is not eligible for review.
  - Validation errors (e.g., rating out of range, field lengths).
- 404 Not Found
  - `CreateReview.OrderNotFound` — Order was not found.
- 409 Conflict
  - `CreateReview.ReviewAlreadyExists` — You have already reviewed this restaurant.

---

## Delete My Review

Deletes one of your reviews. This is a soft delete; deleted reviews are removed from public and personal listings.

**`DELETE /api/v1/restaurants/{restaurantId}/reviews/{reviewId}`**

- **Authorization:** Required (Customer; must be the review owner)

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `restaurantId` | `UUID` | Yes | Restaurant that the review belongs to |
| `reviewId` | `UUID` | Yes | Review identifier to delete |

#### Response

- **✓ 200 OK**

#### Error Responses

- 400 Bad Request
  - `DeleteReview.NotOwner` — You can only delete your own review.
- 404 Not Found
  - `DeleteReview.NotFound` — Review was not found.

---

## List My Reviews

Returns the authenticated user’s reviews, newest first.

**`GET /api/v1/users/me/reviews`**

- **Authorization:** Required (Customer)

#### Query Parameters

| Parameter | Type | Required | Description | Default |
|-----------|------|----------|-------------|---------|
| `pageNumber` | `number` | No | Page number for pagination | `1` |
| `pageSize` | `number` | No | Number of reviews per page | `10` |

#### Response

**✓ 200 OK**
```json
{
  "items": [
    {
      "reviewId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
      "authorUserId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "rating": 5,
      "title": null,
      "comment": "Amazing pasta and great service.",
      "submittedAtUtc": "2025-09-20T19:30:00Z"
    }
  ],
  "pageNumber": 1,
  "totalPages": 1,
  "totalCount": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

#### Review Object

| Field | Type | Description |
|-------|------|-------------|
| `reviewId` | `UUID` | Review identifier |
| `authorUserId` | `UUID` | ID of the user who wrote the review |
| `rating` | `number` | Rating from 1 to 5 |
| `title` | `string\|null` | Optional review title (currently null) |
| `comment` | `string\|null` | Review text content |
| `submittedAtUtc` | `string` | ISO 8601 timestamp when the review was submitted |

#### Error Responses

- 401 Unauthorized — Missing or invalid authentication.

---

## Get Order Review

Retrieves the authenticated user's review for a specific order.

**`GET /api/v1/orders/{orderId}/review`**

- **Authorization:** Required (Customer; must be the order owner)

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `orderId` | `UUID` | Yes | The unique identifier of the order. |

#### Response

**✓ 200 OK**
```json
{
  "reviewId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "orderId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "restaurantId": "b2c3d4e5-f6g7-8901-bcde-fg2345678901",
  "rating": 5,
  "title": "Great dinner!",
  "comment": "Delicious pasta and friendly staff.",
  "createdAt": "2023-10-28T10:00:00Z"
}
```

| Field | Type | Description |
|-------|------|-------------|
| `reviewId` | `UUID` | Unique identifier for the review. |
| `orderId` | `UUID` | Unique identifier for the order. |
| `restaurantId` | `UUID` | Unique identifier for the restaurant. |
| `rating` | `integer` | 1-5 star rating. |
| `title` | `string` | Review title/headline (nullable). |
| `comment` | `string` | Review body text (nullable). |
| `createdAt` | `ISO8601` | Timestamp when the review was created. |

#### Error Responses

- 404 Not Found
  - `Review.NotFound` — No review exists for this order (Client should show "Write Review" button).
- 401 Unauthorized — Missing or invalid authentication.

---

## Browse Restaurant Reviews (Public)

Public endpoints for reading reviews and rating summaries are documented in `Docs/API-Documentation/API-Reference/Customer/02-Restaurant-Discovery.md` under “Restaurant Reviews.” Summary below for quick reference.

### Get Restaurant Reviews

**`GET /api/v1/restaurants/{restaurantId}/reviews`** — Paginated public reviews. Authorization: Public.

### Get Restaurant Review Summary

**`GET /api/v1/restaurants/{restaurantId}/reviews/summary`** — Aggregated statistics including `averageRating`, `totalReviews`, distribution per star (1–5), and timestamps.

---

## Business Rules & Validations

- Create Review
  - Must be authenticated with CompletedSignup.
  - Order must be `Delivered`, owned by the caller, and belong to the same restaurant.
  - One active review per user per restaurant (conflict on duplicates).
  - `rating` is 1–5 inclusive; `title` ≤ 100 chars; `comment` ≤ 1000 chars.
  - `title` is currently reserved and not returned by read endpoints.
- Visibility & Listing
  - Public lists exclude hidden and deleted reviews; results are newest first.
  - Deleting a review removes it from public and personal listings (soft delete).
- Review Summary
  - If a summary does not exist yet, the API returns zeros with `updatedAtUtc` set to current server time.

## Pagination Rules

- Default `pageSize` is 10; page numbers start at 1.
- Suggested maximum `pageSize` is 50 for optimal performance.

## Error Handling

All endpoints return standard HTTP status codes with RFC 7807 problem details.

Example — 409 Conflict
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.8",
  "title": "CreateReview",
  "status": 409,
  "detail": "You have already reviewed this restaurant."
}
```

Example — 400 Bad Request (validation)
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Review.InvalidRating",
  "status": 400,
  "detail": "Rating must be between 1 and 5"
}
```
