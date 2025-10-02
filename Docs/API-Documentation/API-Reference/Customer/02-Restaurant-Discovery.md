# Restaurant Discovery

This guide covers all APIs for discovering restaurants, browsing menus, and viewing reviews. These endpoints help customers find and evaluate restaurants before placing orders.

## Overview

Restaurant discovery in YummyZoom provides multiple ways to find restaurants:

- **Universal Search**: Cross-entity search across restaurants, menu items, and tags
- **Autocomplete**: Quick suggestions for search terms
- **Restaurant Search**: Dedicated restaurant search with location and rating filters
- **Menu Browsing**: View complete restaurant menus with categories and items
- **Review System**: Access restaurant ratings and customer reviews

All endpoints in this section are **public** and require no authentication.

---

## Universal Search

### Search Everything

The primary search endpoint that searches across restaurants, menu items, and tags with comprehensive filtering options.

**`GET /api/v1/search`**

- **Authorization:** Public

#### Query Parameters

| Parameter | Type | Description | Default |
|-----------|------|-------------|---------|
| `term` | `string` | Search term to match against names and descriptions | `null` |
| `lat` | `number` | Latitude for location-based filtering | `null` |
| `lon` | `number` | Longitude for location-based filtering | `null` |
| `openNow` | `boolean` | Filter to only restaurants currently accepting orders | `null` |
| `cuisines` | `string[]` | Array of cuisine types to filter by | `null` |
| `tags` | `string[]` | Array of tags to filter by | `null` |
| `priceBands` | `number[]` | Array of price bands (1-4) to filter by | `null` |
| `includeFacets` | `boolean` | Include facet counts for filtering options | `false` |
| `pageNumber` | `number` | Page number for pagination | `1` |
| `pageSize` | `number` | Number of results per page | `10` |

#### Response

**✅ 200 OK**
```json
{
  "page": {
    "items": [
      {
        "id": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
        "type": "Restaurant",
        "restaurantId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
        "name": "Mario's Italian Bistro",
        "descriptionSnippet": "Authentic Italian cuisine with fresh ingredients...",
        "cuisine": "Italian",
        "score": 0.95,
        "distanceKm": 2.3,
        "badges": [
          {
            "code": "open_now",
            "label": "Open Now"
          },
          {
            "code": "rating",
            "label": "4.5 ⭐",
            "data": {
              "rating": 4.5,
              "reviewCount": 127
            }
          }
        ],
        "reason": "High relevance match for 'italian'"
      },
      {
        "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
        "type": "MenuItem",
        "restaurantId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
        "name": "Margherita Pizza",
        "descriptionSnippet": "Classic pizza with fresh mozzarella and basil",
        "cuisine": "Italian",
        "score": 0.87,
        "distanceKm": 2.3,
        "badges": [
          {
            "code": "price_band",
            "label": "$$"
          }
        ],
        "reason": "Menu item match"
      }
    ],
    "pageNumber": 1,
    "totalPages": 3,
    "totalCount": 25,
    "hasPreviousPage": false,
    "hasNextPage": true
  },
  "facets": {
    "cuisines": [
      { "value": "Italian", "count": 12 },
      { "value": "Mexican", "count": 8 },
      { "value": "Thai", "count": 5 }
    ],
    "tags": [
      { "value": "Vegetarian", "count": 15 },
      { "value": "Gluten-Free", "count": 7 },
      { "value": "Spicy", "count": 9 }
    ],
    "priceBands": [
      { "value": 1, "count": 3 },
      { "value": 2, "count": 12 },
      { "value": 3, "count": 8 },
      { "value": 4, "count": 2 }
    ],
    "openNowCount": 18
  }
}
```

#### Search Result Object

| Field | Type | Description |
|-------|------|-------------|
| `id` | `UUID` | Unique identifier of the result |
| `type` | `string` | Type of result: "Restaurant", "MenuItem", or "Tag" |
| `restaurantId` | `UUID\|null` | Restaurant ID (null for Restaurant type results) |
| `name` | `string` | Name of the entity |
| `descriptionSnippet` | `string\|null` | Brief description or excerpt |
| `cuisine` | `string\|null` | Cuisine type |
| `score` | `number` | Relevance score (0.0 to 1.0) |
| `distanceKm` | `number\|null` | Distance in kilometers (when location provided) |
| `badges` | `array` | Visual indicators for status, rating, price, etc. |
| `reason` | `string\|null` | Explanation of why this result was included |

#### Badge Object

| Field | Type | Description |
|-------|------|-------------|
| `code` | `string` | Badge code: "open_now", "rating", "near_you", "price_band", etc. |
| `label` | `string` | Display text for the badge |
| `data` | `object\|null` | Optional additional data (e.g., rating details, distance info) |

#### Facets Object

| Field | Type | Description |
|-------|------|-------------|
| `cuisines` | `array` | Available cuisine types with counts |
| `tags` | `array` | Available tags with counts |
| `priceBands` | `array` | Available price bands (1-4) with counts |
| `openNowCount` | `number` | Number of restaurants currently accepting orders |

---

### Search Autocomplete

Provides quick suggestions for search terms as users type.

**`GET /api/v1/search/autocomplete`**

- **Authorization:** Public

#### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `term` | `string` | Yes | Partial search term (1-64 characters) |

#### Response

**✅ 200 OK**
```json
[
  {
    "id": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
    "type": "Restaurant",
    "name": "Mario's Italian Bistro"
  },
  {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "type": "MenuItem",
    "name": "Margherita Pizza"
  },
  {
    "id": "b2c3d4e5-f6g7-8901-bcde-fg2345678901",
    "type": "Tag",
    "name": "Italian"
  }
]
```

#### Suggestion Object

| Field | Type | Description |
|-------|------|-------------|
| `id` | `UUID` | Unique identifier |
| `type` | `string` | "Restaurant", "MenuItem", or "Tag" |
| `name` | `string` | Display name for the suggestion |

#### Business Rules
- Returns up to 10 suggestions
- Results are ranked by similarity and relevance
- Includes prefix matching and fuzzy matching
- Excludes soft-deleted entities

---

## Restaurant Discovery

### Search Restaurants

Dedicated endpoint for searching restaurants with location and rating filters.

**`GET /api/v1/restaurants/search`**

- **Authorization:** Public

#### Query Parameters

| Parameter | Type | Description | Default |
|-----------|------|-------------|---------|
| `q` | `string` | Search term for restaurant name | `null` |
| `cuisine` | `string` | Cuisine type filter | `null` |
| `lat` | `number` | Latitude for location-based search | `null` |
| `lng` | `number` | Longitude for location-based search | `null` |
| `radiusKm` | `number` | Search radius in kilometers | `null` |
| `minRating` | `number` | Minimum average rating (1.0-5.0) | `null` |
| `pageNumber` | `number` | Page number for pagination | `1` |
| `pageSize` | `number` | Number of results per page | `10` |

#### Response

**✅ 200 OK**
```json
{
  "items": [
    {
      "restaurantId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
      "name": "Mario's Italian Bistro",
      "logoUrl": "https://cdn.yummyzoom.com/logos/marios.jpg",
      "cuisineTags": ["Italian", "Pizza", "Pasta"],
      "avgRating": 4.5,
      "ratingCount": 127,
      "city": "San Francisco"
    },
    {
      "restaurantId": "b2c3d4e5-f6g7-8901-bcde-fg2345678901",
      "name": "Pasta Palace",
      "logoUrl": null,
      "cuisineTags": ["Italian", "Pasta"],
      "avgRating": 4.2,
      "ratingCount": 89,
      "city": "San Francisco"
    }
  ],
  "pageNumber": 1,
  "totalPages": 2,
  "totalCount": 15,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

#### Restaurant Search Result Object

| Field | Type | Description |
|-------|------|-------------|
| `restaurantId` | `UUID` | Unique restaurant identifier |
| `name` | `string` | Restaurant name |
| `logoUrl` | `string\|null` | URL to restaurant logo image |
| `cuisineTags` | `string[]` | Array of cuisine types |
| `avgRating` | `number\|null` | Average customer rating (1.0-5.0) |
| `ratingCount` | `number\|null` | Total number of ratings |
| `city` | `string\|null` | City where restaurant is located |

---

### Get Restaurant Information

Retrieves basic public information about a specific restaurant.

**`GET /api/v1/restaurants/{restaurantId}/info`**

- **Authorization:** Public

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `restaurantId` | `UUID` | Yes | Unique identifier of the restaurant |

#### Response

**✅ 200 OK**
```json
{
  "restaurantId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "name": "Mario's Italian Bistro",
  "logoUrl": "https://cdn.yummyzoom.com/logos/marios.jpg",
  "cuisineTags": ["Italian", "Pizza", "Pasta"],
  "isAcceptingOrders": true,
  "city": "San Francisco"
}
```

#### Restaurant Info Object

| Field | Type | Description |
|-------|------|-------------|
| `restaurantId` | `UUID` | Unique restaurant identifier |
| `name` | `string` | Restaurant name |
| `logoUrl` | `string\|null` | URL to restaurant logo image |
| `cuisineTags` | `string[]` | Array of cuisine types |
| `isAcceptingOrders` | `boolean` | Whether the restaurant is currently accepting orders |
| `city` | `string\|null` | City where restaurant is located |

#### Error Responses

**❌ 404 Not Found**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Restaurant info was not found."
}
```

---

## Menu Browsing

### Get Restaurant Menu

Returns the complete menu for a restaurant in a normalized, frontend-optimized structure. The response uses a "byId" pattern where items are stored in dictionaries keyed by their IDs, with separate arrays for ordering. This structure minimizes data duplication and provides efficient lookups for client applications.

The response includes:
- **Menu metadata**: Basic information about the menu itself
- **Categories**: Organized in display order with references to their menu items
- **Items**: Complete item details with pricing, descriptions, and customization options
- **Customization Groups**: Available modifications for menu items
- **Tag Legend**: Dietary and other classification tags

All monetary values include currency information, and the structure is optimized for caching and real-time updates.

**`GET /api/v1/restaurants/{restaurantId}/menu`**

- **Authorization:** Public
- **Caching:** Supports HTTP caching with ETag and Last-Modified headers

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `restaurantId` | `UUID` | Yes | Unique identifier of the restaurant |

#### Response Headers

| Header | Description |
|--------|-------------|
| `ETag` | Entity tag for cache validation |
| `Last-Modified` | Last modification timestamp |
| `Cache-Control` | Caching directives (`public, max-age=300`) |

#### Response

**✅ 200 OK**
```json
{
  "version": 1,
  "restaurantId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "menuId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "menuName": "Main Menu",
  "menuDescription": "Our delicious selection of authentic Italian dishes",
  "menuEnabled": true,
  "lastRebuiltAt": "2023-10-27T10:30:00Z",
  "currency": "USD",
  "categories": {
    "order": [
      "b2c3d4e5-f6g7-8901-bcde-fg2345678901",
      "c3d4e5f6-g7h8-9012-cdef-gh3456789012"
    ],
    "byId": {
      "b2c3d4e5-f6g7-8901-bcde-fg2345678901": {
        "id": "b2c3d4e5-f6g7-8901-bcde-fg2345678901",
        "name": "Appetizers",
        "displayOrder": 1,
        "itemOrder": [
          "d4e5f6g7-h8i9-0123-defg-hi4567890123",
          "e5f6g7h8-i9j0-1234-efgh-ij5678901234"
        ]
      },
      "c3d4e5f6-g7h8-9012-cdef-gh3456789012": {
        "id": "c3d4e5f6-g7h8-9012-cdef-gh3456789012",
        "name": "Main Courses",
        "displayOrder": 2,
        "itemOrder": [
          "f6g7h8i9-j0k1-2345-fghi-jk6789012345"
        ]
      }
    }
  },
  "items": {
    "byId": {
      "d4e5f6g7-h8i9-0123-defg-hi4567890123": {
        "id": "d4e5f6g7-h8i9-0123-defg-hi4567890123",
        "categoryId": "b2c3d4e5-f6g7-8901-bcde-fg2345678901",
        "name": "Bruschetta",
        "description": "Grilled bread topped with fresh tomatoes, garlic, and basil",
        "price": {
          "amount": 8.99,
          "currency": "USD"
        },
        "imageUrl": "https://cdn.yummyzoom.com/items/bruschetta.jpg",
        "isAvailable": true,
        "dietaryTagIds": [
          "g7h8i9j0-k1l2-3456-ghij-kl7890123456"
        ],
        "customizationGroups": [
          {
            "groupId": "h8i9j0k1-l2m3-4567-hijk-lm8901234567",
            "displayTitle": "Bread Type",
            "displayOrder": 1
          }
        ]
      }
    }
  },
  "customizationGroups": {
    "byId": {
      "h8i9j0k1-l2m3-4567-hijk-lm8901234567": {
        "id": "h8i9j0k1-l2m3-4567-hijk-lm8901234567",
        "name": "Bread Type",
        "min": 1,
        "max": 1,
        "options": [
          {
            "id": "i9j0k1l2-m3n4-5678-ijkl-mn9012345678",
            "name": "Sourdough",
            "priceDelta": {
              "amount": 0.00,
              "currency": "USD"
            },
            "isDefault": true,
            "displayOrder": 1
          },
          {
            "id": "j0k1l2m3-n4o5-6789-jklm-no0123456789",
            "name": "Whole Wheat",
            "priceDelta": {
              "amount": 1.00,
              "currency": "USD"
            },
            "isDefault": false,
            "displayOrder": 2
          }
        ]
      }
    }
  },
  "tagLegend": {
    "byId": {
      "g7h8i9j0-k1l2-3456-ghij-kl7890123456": {
        "name": "Vegetarian",
        "category": "Dietary"
      }
    }
  }
}
```

#### Menu Structure

| Field | Type | Description |
|-------|------|-------------|
| `restaurantId` | `UUID` | Restaurant identifier |
| `name` | `string` | Restaurant name |
| `isAcceptingOrders` | `boolean` | Whether restaurant is accepting orders |
| `lastUpdated` | `string` | ISO 8601 timestamp of last menu update |
| `categories` | `array` | Array of menu categories |

#### Category Object

| Field | Type | Description |
|-------|------|-------------|
| `categoryId` | `UUID` | Category identifier |
| `name` | `string` | Category name |
| `description` | `string\|null` | Category description |
| `displayOrder` | `number` | Sort order for display |
| `items` | `array` | Array of menu items in this category |

#### Menu Item Object

| Field | Type | Description |
|-------|------|-------------|
| `itemId` | `UUID` | Item identifier |
| `name` | `string` | Item name |
| `description` | `string\|null` | Item description |
| `basePrice` | `Money` | Base price before customizations |
| `isAvailable` | `boolean` | Whether item is currently available |
| `dietaryTags` | `string[]` | Dietary information (e.g., "Vegetarian", "Gluten-Free") |
| `customizationGroups` | `array` | Available customization options |

#### Customization Group Object

| Field | Type | Description |
|-------|------|-------------|
| `groupId` | `UUID` | Customization group identifier |
| `name` | `string` | Group name (e.g., "Size", "Toppings") |
| `isRequired` | `boolean` | Whether selection is required |
| `maxSelections` | `number` | Maximum number of options that can be selected |
| `options` | `array` | Available customization options |

#### Customization Option Object

| Field | Type | Description |
|-------|------|-------------|
| `optionId` | `UUID` | Option identifier |
| `name` | `string` | Option name |
| `priceAdjustment` | `Money` | Price adjustment for this option |

#### Money Object

| Field | Type | Description |
|-------|------|-------------|
| `amount` | `number` | Monetary amount |
| `currency` | `string` | Currency code (e.g., "USD") |

#### Caching Behavior

- **Cache Duration:** 5 minutes (`max-age=300`)
- **ETag Support:** Returns weak ETag for cache validation
- **304 Not Modified:** Returned when content hasn't changed
- **Last-Modified:** Timestamp of last menu rebuild

#### Error Responses

**❌ 304 Not Modified**
No response body. Content hasn't changed since last request.

**❌ 404 Not Found**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Menu view for the restaurant was not found."
}
```

---

## Restaurant Reviews

### Get Restaurant Reviews

Retrieves paginated customer reviews for a restaurant.

**`GET /api/v1/restaurants/{restaurantId}/reviews`**

- **Authorization:** Public

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `restaurantId` | `UUID` | Yes | Unique identifier of the restaurant |

#### Query Parameters

| Parameter | Type | Required | Description | Default |
|-----------|------|----------|-------------|---------|
| `pageNumber` | `number` | No | Page number for pagination | `1` |
| `pageSize` | `number` | No | Number of reviews per page | `10` |

#### Response

**✅ 200 OK**
```json
{
  "items": [
    {
      "reviewId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
      "authorUserId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "rating": 5,
      "title": "Excellent Italian food!",
      "comment": "Amazing pasta and great service. The atmosphere is cozy and perfect for a date night.",
      "submittedAtUtc": "2023-10-25T19:30:00Z"
    },
    {
      "reviewId": "b2c3d4e5-f6g7-8901-bcde-fg2345678901",
      "authorUserId": "c3d4e5f6-g7h8-9012-cdef-gh3456789012",
      "rating": 4,
      "title": null,
      "comment": "Good food, but service was a bit slow during peak hours.",
      "submittedAtUtc": "2023-10-24T20:15:00Z"
    }
  ],
  "pageNumber": 1,
  "totalPages": 5,
  "totalCount": 45,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

#### Review Object

| Field | Type | Description |
|-------|------|-------------|
| `reviewId` | `UUID` | Review identifier |
| `authorUserId` | `UUID` | ID of the user who wrote the review |
| `rating` | `number` | Rating from 1 to 5 stars |
| `title` | `string\|null` | Optional review title |
| `comment` | `string\|null` | Review text content |
| `submittedAtUtc` | `string` | ISO 8601 timestamp when review was submitted |

---

### Get Restaurant Review Summary

Retrieves aggregated review statistics for a restaurant.

**`GET /api/v1/restaurants/{restaurantId}/reviews/summary`**

- **Authorization:** Public

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `restaurantId` | `UUID` | Yes | Unique identifier of the restaurant |

#### Response

**✅ 200 OK**
```json
{
  "averageRating": 4.3,
  "totalReviews": 127,
  "ratings1": 2,
  "ratings2": 5,
  "ratings3": 15,
  "ratings4": 45,
  "ratings5": 60,
  "totalWithText": 89,
  "lastReviewAtUtc": "2023-10-27T08:45:00Z",
  "updatedAtUtc": "2023-10-27T09:00:00Z"
}
```

#### Review Summary Object

| Field | Type | Description |
|-------|------|-------------|
| `averageRating` | `number` | Average rating (1.0-5.0) |
| `totalReviews` | `number` | Total number of reviews |
| `ratings1` | `number` | Number of 1-star reviews |
| `ratings2` | `number` | Number of 2-star reviews |
| `ratings3` | `number` | Number of 3-star reviews |
| `ratings4` | `number` | Number of 4-star reviews |
| `ratings5` | `number` | Number of 5-star reviews |
| `totalWithText` | `number` | Number of reviews with text comments |
| `lastReviewAtUtc` | `string\|null` | ISO 8601 timestamp of most recent review |
| `updatedAtUtc` | `string` | ISO 8601 timestamp when summary was last updated |

---

## Business Rules & Validations

### Search Rules
- Universal search supports fuzzy matching and prefix matching
- Location-based search requires both `lat` and `lon` parameters
- Maximum search term length is 64 characters for autocomplete
- Search results are ranked by relevance score and distance (when location provided)
- Facets are only returned when `includeFacets=true`

### Menu Access Rules
- Menu data is cached for 5 minutes for performance
- Only active, non-deleted menu items are returned
- Customization options include price adjustments relative to base price
- Menu availability reflects real-time restaurant status

### Review Display Rules
- Reviews are ordered by submission date (newest first)
- Only approved, non-moderated reviews are shown
- Review summary statistics are updated in near real-time
- User information is limited to user ID for privacy

### Pagination Rules
- Default page size is 10 items
- Maximum page size is 50 items
- Page numbers start at 1
- Empty results return valid pagination metadata with zero counts

---

## Error Handling

All endpoints return standard HTTP status codes and problem details:

**400 Bad Request** - Invalid query parameters
```json
{
  "type": "validation",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "pageSize": ["Page size must be between 1 and 50."]
  }
}
```

**404 Not Found** - Restaurant or resource not found
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Restaurant info was not found."
}
```

**500 Internal Server Error** - Server error
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "An error occurred while processing your request.",
  "status": 500
}
```

---

## Performance Considerations

### Caching Strategy
- **Menu data**: Cached for 5 minutes with ETag support
- **Restaurant info**: Cached for 2 minutes
- **Search results**: Not cached due to dynamic nature
- **Review summaries**: Cached with invalidation on new reviews

### Location-Based Search
- Provide latitude/longitude for distance-based results
- Use appropriate radius values (typically 1-50 km)
- Results are sorted by relevance score, then distance

### Pagination Best Practices
- Use reasonable page sizes (10-20 items) for optimal performance
- Implement infinite scroll or "Load More" patterns for mobile
- Cache pagination state on client side for smooth navigation

---

## Complete Discovery Workflow

Here's a typical restaurant discovery flow:

1. **Start with Search**: Use autocomplete for query suggestions
2. **Broad Search**: Universal search to find restaurants and menu items
3. **Refine Results**: Apply location, cuisine, and rating filters
4. **Restaurant Details**: Get specific restaurant information
5. **Browse Menu**: View complete menu with prices and options
6. **Check Reviews**: Read customer feedback and ratings
7. **Ready to Order**: Proceed to order placement with selected restaurant