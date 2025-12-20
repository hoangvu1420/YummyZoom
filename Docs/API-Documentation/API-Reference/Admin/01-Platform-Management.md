# Admin Platform Management

Base path: `/api/v1/`

Admin endpoints for platform metrics, trends, restaurant health, and notification delivery.

---

## GET /admin/dashboard/summary

Return the latest platform KPI snapshot for admin dashboards.

- Authorization: Admin

### Request

No parameters.

### Response

#### 200 OK

```json
{
  "totalOrders": 125430,
  "activeOrders": 412,
  "deliveredOrders": 123800,
  "grossMerchandiseVolume": 2530042.75,
  "totalRefunds": 12450.25,
  "activeRestaurants": 438,
  "activeCustomers": 52490,
  "openSupportTickets": 32,
  "totalReviews": 98201,
  "lastOrderAtUtc": "2024-12-18T14:22:11Z",
  "updatedAtUtc": "2024-12-18T14:25:00Z"
}
```

Field descriptions

| Field | Type | Description |
| --- | --- | --- |
| `totalOrders` | integer | Total orders on the platform. |
| `activeOrders` | integer | Orders currently in an active lifecycle state. |
| `deliveredOrders` | integer | Total delivered orders. |
| `grossMerchandiseVolume` | number | Total GMV across all orders. |
| `totalRefunds` | number | Total refunds issued. |
| `activeRestaurants` | integer | Restaurants currently active on the platform. |
| `activeCustomers` | integer | Customers currently active on the platform. |
| `openSupportTickets` | integer | Count of open support tickets. |
| `totalReviews` | integer | Total reviews recorded. |
| `lastOrderAtUtc` | string or null | ISO 8601 timestamp of the most recent order, or null if none. |
| `updatedAtUtc` | string | ISO 8601 timestamp when the snapshot was computed. |

#### Error Responses

- 401 Unauthorized
- 403 Forbidden
- 404 Not Found
- 500 Internal Server Error

---

## GET /admin/dashboard/trends

Return daily performance metrics for a date range.

- Authorization: Admin

### Request

#### Query Parameters

| Parameter | Type | Description | Default |
| --- | --- | --- | --- |
| `startDate` | string (date) | Inclusive start date in `YYYY-MM-DD`. | `null` |
| `endDate` | string (date) | Inclusive end date in `YYYY-MM-DD`. | `null` |

### Response

#### 200 OK

```json
[
  {
    "bucketDate": "2024-12-10",
    "totalOrders": 412,
    "deliveredOrders": 395,
    "grossMerchandiseVolume": 12034.55,
    "totalRefunds": 62.50,
    "newCustomers": 48,
    "newRestaurants": 3,
    "updatedAtUtc": "2024-12-10T23:59:59Z"
  }
]
```

Field descriptions

| Field | Type | Description |
| --- | --- | --- |
| `bucketDate` | string (date) | Daily bucket in `YYYY-MM-DD`. |
| `totalOrders` | integer | Orders placed on the day. |
| `deliveredOrders` | integer | Orders delivered on the day. |
| `grossMerchandiseVolume` | number | GMV for the day. |
| `totalRefunds` | number | Refunds issued on the day. |
| `newCustomers` | integer | New customer signups. |
| `newRestaurants` | integer | New restaurant signups. |
| `updatedAtUtc` | string | ISO 8601 timestamp for the data snapshot. |

#### Error Responses

- 400 Bad Request
- 401 Unauthorized
- 403 Forbidden
- 500 Internal Server Error

---

## GET /admin/dashboard/restaurants

Return a paginated list of restaurant health summaries for admins.

- Authorization: Admin

### Request

#### Query Parameters

| Parameter | Type | Description | Default |
| --- | --- | --- | --- |
| `pageNumber` | integer | Page number (1-based). | `1` |
| `pageSize` | integer | Page size. | `25` |
| `isVerified` | boolean | Filter by verification status. | `null` |
| `isAcceptingOrders` | boolean | Filter by accepting orders. | `null` |
| `minAverageRating` | number | Minimum average rating filter. | `null` |
| `minOrdersLast30Days` | integer | Minimum orders in the last 30 days. | `null` |
| `maxOutstandingBalance` | number | Maximum outstanding balance filter. | `null` |
| `search` | string | Case-insensitive search by restaurant name. | `null` |
| `sortBy` | string | Sort option. | `RevenueDescending` |

Sort options for `sortBy`:

- `RevenueDescending`
- `OrdersDescending`
- `RatingDescending`
- `OutstandingBalanceDescending`
- `OutstandingBalanceAscending`
- `LastOrderDescending`
- `LastOrderAscending`

### Response

#### 200 OK

```json
{
  "items": [
    {
      "restaurantId": "6bdb6aa7-2b76-4bb2-9c16-7a9f1ce0df9f",
      "restaurantName": "Pasta Palace",
      "isVerified": true,
      "isAcceptingOrders": true,
      "ordersLast7Days": 120,
      "ordersLast30Days": 480,
      "revenueLast30Days": 45210.75,
      "averageRating": 4.6,
      "totalReviews": 1830,
      "couponRedemptionsLast30Days": 86,
      "outstandingBalance": 0.0,
      "lastOrderAtUtc": "2024-12-18T14:22:11Z",
      "updatedAtUtc": "2024-12-18T14:25:00Z"
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
| `items` | array | Page of restaurant health summaries. |
| `items[].restaurantId` | UUID | Restaurant identifier. |
| `items[].restaurantName` | string | Restaurant name. |
| `items[].isVerified` | boolean | Whether the restaurant is verified. |
| `items[].isAcceptingOrders` | boolean | Whether the restaurant is accepting orders. |
| `items[].ordersLast7Days` | integer | Orders in the last 7 days. |
| `items[].ordersLast30Days` | integer | Orders in the last 30 days. |
| `items[].revenueLast30Days` | number | Revenue in the last 30 days. |
| `items[].averageRating` | number | Average rating. |
| `items[].totalReviews` | integer | Total review count. |
| `items[].couponRedemptionsLast30Days` | integer | Coupon redemptions in the last 30 days. |
| `items[].outstandingBalance` | number | Outstanding balance amount. |
| `items[].lastOrderAtUtc` | string or null | ISO 8601 timestamp of last order, or null. |
| `items[].updatedAtUtc` | string | ISO 8601 timestamp for the snapshot. |
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

## POST /notifications/send-to-user

Send a push notification to a specific user.

- Authorization: Admin

### Request

#### Request Body

```json
{
  "userId": "c2a4c0cd-0dc9-4c3f-bc9b-c5a6fb1dd3ee",
  "title": "Account Update",
  "body": "Your account settings were updated.",
  "dataPayload": {
    "category": "account",
    "action": "settings-update"
  }
}
```

Field descriptions

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `userId` | UUID | Yes | Target user identifier. |
| `title` | string | Yes | Notification title. |
| `body` | string | Yes | Notification body text. |
| `dataPayload` | object | No | Optional key/value metadata for client handling. |

### Response

- 204 No Content

#### Error Responses

- 400 Bad Request
- 401 Unauthorized
- 403 Forbidden
- 500 Internal Server Error

---

## POST /notifications/send-broadcast

Send a push notification to all active users.

- Authorization: Admin

### Request

#### Request Body

```json
{
  "title": "Service Update",
  "body": "We will be undergoing maintenance at 02:00 UTC.",
  "dataPayload": {
    "category": "maintenance",
    "priority": "high"
  }
}
```

Field descriptions

| Field | Type | Required | Description |
| --- | --- | --- | --- |
| `title` | string | Yes | Notification title. |
| `body` | string | Yes | Notification body text. |
| `dataPayload` | object | No | Optional key/value metadata for client handling. |

### Response

- 204 No Content

#### Error Responses

- 400 Bad Request
- 401 Unauthorized
- 403 Forbidden
- 500 Internal Server Error
