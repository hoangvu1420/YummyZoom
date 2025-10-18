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
| `entityTypes` | `string[]` | Restrict results to specific entity types. Allowed values (case-insensitive): `restaurant`, `menu_item`, `tag`. Repeat the parameter to pass multiple values (e.g., `?entityTypes=menu_item&entityTypes=restaurant`). | `null` |
| `sort` | `string` | `relevance` (default). Allowed: `relevance`, `distance` (needs lat/lon), `rating`, `priceBand`, `popularity` | `relevance` |
| `bbox` | `string` | Viewport filter `minLon,minLat,maxLon,maxLat` (WGS84). Distance sort still requires lat/lon. | `null` |
| `includeFacets` | `boolean` | Include facet counts for filtering options | `false` |
| `pageNumber` | `number` | Page number for pagination | `1` |
| `pageSize` | `number` | Number of results per page | `10` |

> Note
>
> Use the underscore form `menu_item` for the `entityTypes` filter (not `MenuItem` or `menuitem`). The API matches canonical tokens stored in the search index: `restaurant`, `menu_item`, `tag`.

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
| `restaurantId` | `UUID|  `itemId` | `UUID` | Menu item identifier | |null` | Restaurant ID (null for Restaurant type results) |
| `name` | `string` | Name of the entity |
| `descriptionSnippet` | `string|  `itemId` | `UUID` | Menu item identifier | |null` | Brief description or excerpt |
| `cuisine` | `string|  `itemId` | `UUID` | Menu item identifier | |null` | Cuisine type |
| `score` | `number` | Relevance score (0.0 to 1.0) |
| `distanceKm` | `number|  `itemId` | `UUID` | Menu item identifier | |null` | Distance in kilometers (when location provided) |
| `badges` | `array` | Visual indicators for status, rating, price, etc. |
| `reason` | `string|  `itemId` | `UUID` | Menu item identifier | |null` | Explanation of why this result was included |

#### Badge Object

| Field | Type | Description |
|-------|------|-------------|
| `code` | `string` | Badge code: "open_now", "rating", "near_you", "price_band", etc. |
| `label` | `string` | Display text for the badge |
| `data` | `object|  `itemId` | `UUID` | Menu item identifier | |null` | Optional additional data (e.g., rating details, distance info) |

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
| `limit` | `number` | No | Max suggestions to return. Default 10. Range 1–50. |
| `types` | `string[]` | No | Optional filter by entity type(s). Allowed values (case-insensitive): `restaurant`, `menu_item`, `tag`. Repeat the parameter for multiple values. |

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

### Top Tags

Returns the most-used tags across menu items in verified restaurants. Useful for discovery surfaces like “Trending” or tag carousels.

**`GET /api/v1/tags/top`**

- **Authorization:** Public

#### Query Parameters

| Parameter | Type | Description | Default |
|-----------|------|-------------|---------|
| `categories` | `string` | Optional comma-separated list of tag categories to filter by. Allowed values (case-insensitive): `Dietary`, `Cuisine`, `SpiceLevel`, `Allergen`, `Preparation`, `Temperature`, `CookingMethod`, `Course`, `Beverage`, `PortionSize`, `Popularity`. Examples: `Dietary`, `Dietary,Cuisine`, `Cuisine,SpiceLevel,Allergen`. | `null` (all categories) |
| `limit` | `number` | Max number of tags to return. Range 1–100. | `10` |

> Note
>
> Counts are computed from tag assignments on active, non-deleted menu items belonging to verified restaurants. Tags are ordered by usage count (descending), then by name (ascending) for deterministic results.

#### Usage Examples

- **Get all top tags**: `GET /api/v1/tags/top?limit=20`
- **Get only dietary tags**: `GET /api/v1/tags/top?categories=Dietary&limit=10`
- **Get dietary and cuisine tags**: `GET /api/v1/tags/top?categories=Dietary,Cuisine&limit=15`
- **Get multiple categories**: `GET /api/v1/tags/top?categories=Cuisine,SpiceLevel,Allergen&limit=25`

#### Response

**• 200 OK**
```json
[
  {
    "tagId": "1c9b0b7c-49b7-4e8f-9b9a-7dbf2c3b6b4a",
    "tagName": "Vegetarian",
    "tagCategory": "Dietary",
    "usageCount": 42
  },
  {
    "tagId": "5ea7a9c7-9426-40ae-8e7f-dc6c906d4d87",
    "tagName": "Italian",
    "tagCategory": "Cuisine",
    "usageCount": 38
  },
  {
    "tagId": "2a7e3c1d-5f4b-4a8a-9c1e-8f2b6d3c4a5b",
    "tagName": "Vegan",
    "tagCategory": "Dietary",
    "usageCount": 35
  },
  {
    "tagId": "9c700a6d-be30-48c4-bfaf-532547f6c1db",
    "tagName": "Vietnamese",
    "tagCategory": "Cuisine",
    "usageCount": 24
  }
]
```

**• 400 Bad Request** (Invalid category specified)
```json
{
  "title": "Tags.Top.InvalidCategory",
  "detail": "Unknown category 'InvalidCategory'. Valid values: Dietary, Cuisine, SpiceLevel, Allergen, Preparation, Temperature.",
  "status": 400
}
```

#### Top Tag Object

| Field | Type | Description |
|-------|------|-------------|
| `tagId` | `UUID` | Tag identifier |
| `tagName` | `string` | Human-readable tag name |
| `tagCategory` | `string` | One of: `Dietary`, `Cuisine`, `SpiceLevel`, `Allergen`, `Preparation`, `Temperature` |
| `usageCount` | `number` | Number of menu-item assignments contributing to this tag |

---

## Menu Items Feed

### Browse Featured Items

Curated menu items feed for the Home screen. Returns a paginated list of popular items.

**`GET /api/v1/menu-items/feed`**

- **Authorization:** Public

#### Query Parameters

| Parameter | Type | Description | Default |
|-----------|------|-------------|---------|
| `tab` | `string` | Feed variant. Allowed values: `popular`. | `popular` |
| `pageNumber` | `number` | Page number for pagination | `1` |
| `pageSize` | `number` | Number of results per page (1–50) | `20` |

#### Response

**✅ 200 OK**
```json
{
  "items": [
    {
      "itemId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "name": "Margherita Pizza",
      "priceAmount": 12.99,
      "priceCurrency": "USD",
      "imageUrl": "https://cdn.example.com/img/pizza.jpg",
      "rating": 4.6,
      "restaurantName": "Mario's Italian Bistro",
      "restaurantId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
      "lifetimeSoldCount": 342
    }
  ],
  "pageNumber": 1,
  "totalPages": 10,
  "totalCount": 200,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

#### Feed Item Object

| Field | Type | Description |
|-------|------|-------------|
| `itemId` | `UUID` | Menu item identifier |
| `name` | `string` | Menu item name |
| `priceAmount` | `number` | Price amount |
| `priceCurrency` | `string` | ISO 4217 currency code |
| `imageUrl` | `string \ null` | Image URL if available |
| `rating` | `number \ null` | Restaurant average rating |
| `restaurantName` | `string` | Owning restaurant name |
| `restaurantId` | `UUID` | Owning restaurant identifier |
| `lifetimeSoldCount` | `number` | Total lifetime quantity sold (delivered orders only) |

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
| `tags` | `string[]` | Filter by tag name(s). Tag names are unique. Repeat the parameter to pass multiple values (e.g., `?tags=Vegetarian&tags=Vegan`). Matches restaurants having at least one menu item with any of the specified tags. | `null` |
| `tagIds` | `UUID[]` | Filter by tag ID(s). Repeat the parameter to pass multiple values (e.g., `?tagIds=...&tagIds=...`). Matches restaurants having at least one menu item with any of the specified tag IDs. | `null` |
| `lat` | `number` | Latitude for distance computation | `null` |
| `lng` | `number` | Longitude for distance computation | `null` |
| `radiusKm` | `number` | Reserved for future map viewport/radius (not supported in MVP) | `null` |
| `minRating` | `number` | Minimum average rating (1.0-5.0) | `null` |
| `pageNumber` | `number` | Page number for pagination | `1` |
| `pageSize` | `number` | Number of results per page | `10` |
| `sort` | `string` | Sort order: `rating`, `distance` (requires `lat`/`lng`), or `popularity`. | `null` |

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
      "city": "San Francisco",
      "distanceKm": 1.4,
      "latitude": 37.7749,
      "longitude": -122.4194
    },
    {
      "restaurantId": "b2c3d4e5-f6g7-8901-bcde-fg2345678901",
      "name": "Pasta Palace",
      "logoUrl": null,
      "cuisineTags": ["Italian", "Pasta"],
      "avgRating": 4.2,
      "ratingCount": 89,
      "city": "San Francisco",
      "distanceKm": 2.8,
      "latitude": 37.7812,
      "longitude": -122.4115
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
| `logoUrl` | `string|  `itemId` | `UUID` | Menu item identifier | |null` | URL to restaurant logo image |
| `cuisineTags` | `string[]` | Array of cuisine types |
| `avgRating` | `number|  `itemId` | `UUID` | Menu item identifier | |null` | Average customer rating (1.0-5.0) |
| `ratingCount` | `number|  `itemId` | `UUID` | Menu item identifier | |null` | Total number of ratings |
| `city` | `string|  `itemId` | `UUID` | Menu item identifier | |null` | City where restaurant is located |
| `distanceKm` | `number|  `itemId` | `UUID` | Menu item identifier | |null` | Distance in kilometers when `lat`/`lng` are provided; otherwise null |
| `latitude` | `number|  `itemId` | `UUID` | Menu item identifier | |null` | Latitude when available; otherwise null |
| `longitude` | `number|  `itemId` | `UUID` | Menu item identifier | |null` | Longitude when available; otherwise null |

---

#### Tag Filtering Behavior

- Tag names are globally unique; filtering by `tags` uses an exact, case-insensitive match on tag names.
- `tags` and `tagIds` are combined with OR logic: if both are provided, a restaurant matches when it has at least one menu item with any listed tag name or ID.
- Matching considers active, non-deleted menu items from verified restaurants only.


### Get Restaurant Information

Retrieves comprehensive public information about a specific restaurant including contact details, operating hours, address, and cuisine tags derived from menu items.

**`GET /api/v1/restaurants/{restaurantId}/info`**

- **Authorization:** Public

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `restaurantId` | `UUID` | Yes | Unique identifier of the restaurant |

#### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `lat` | `number` | No | Latitude for distance calculation. Must be between -90 and 90. Both `lat` and `lng` must be provided together. |
| `lng` | `number` | No | Longitude for distance calculation. Must be between -180 and 180. Both `lat` and `lng` must be provided together. |

> **Note**  
> When both `lat` and `lng` are provided, the response includes the calculated distance in kilometers from the provided location to the restaurant. If only one coordinate is provided, or if values are out of range, a 400 Bad Request error is returned.

#### Response Examples

**✅ 200 OK**
```json
{
  "restaurantId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "name": "Mario's Italian Bistro",
  "logoUrl": "https://cdn.yummyzoom.com/logos/marios.jpg",
  "backgroundImageUrl": "https://cdn.yummyzoom.com/backgrounds/marios-bg.jpg",
  "description": "Authentic Italian cuisine in the heart of San Francisco",
  "cuisineType": "Italian",
  "cuisineTags": ["Italian", "Mediterranean"],
  "isAcceptingOrders": true,
  "isVerified": true,
  "address": {
    "street": "123 Market Street",
    "city": "San Francisco",
    "state": "CA",
    "zipCode": "94102",
    "country": "USA"
  },
  "contactInfo": {
    "phoneNumber": "+1-415-555-0123",
    "email": "info@mariosbistro.com"
  },
  "businessHours": "11:00-22:00",
  "establishedDate": "2020-06-15T10:30:00Z",
  "avgRating": 4.5,
  "ratingCount": 127,
  "distanceKm": 2.3
}
```

#### Restaurant Info Object

| Field | Type | Description |
|-------|------|-------------|
| `restaurantId` | `UUID` | Unique restaurant identifier |
| `name` | `string` | Restaurant name |
| `logoUrl` | `string|null` | URL to restaurant logo image |
| `backgroundImageUrl` | `string|null` | URL to restaurant background/banner image |
| `description` | `string` | Detailed description of the restaurant |
| `cuisineType` | `string` | Primary cuisine classification |
| `cuisineTags` | `string[]` | Array of cuisine tags from menu items (Cuisine category only, alphabetically sorted) |
| `isAcceptingOrders` | `boolean` | Whether the restaurant is currently accepting orders |
| `isVerified` | `boolean` | Partner verification status |
| `address` | `object` | Full restaurant address |
| `address.street` | `string` | Street address |
| `address.city` | `string` | City name |
| `address.state` | `string` | State/province |
| `address.zipCode` | `string` | Postal/ZIP code |
| `address.country` | `string` | Country name |
| `contactInfo` | `object` | Contact information |
| `contactInfo.phoneNumber` | `string` | Phone number |
| `contactInfo.email` | `string` | Email address |
| `businessHours` | `string` | Operating hours (format: `HH:MM-HH:MM`) |
| `establishedDate` | `string` | ISO 8601 timestamp when restaurant was added to platform |
| `avgRating` | `number|null` | Average rating (1-5), if available |
| `ratingCount` | `number|null` | Total number of ratings, if available |
| `distanceKm` | `number|null` | Distance in kilometers from provided location (only when lat/lng supplied and restaurant has coordinates) |

> **Note on CuisineTags**  
> The `cuisineTags` array is dynamically populated from tags assigned to the restaurant's menu items. Only tags with category "Cuisine" are included. Tags are unique, sorted alphabetically, and exclude soft-deleted menu items or tags.

#### Error Responses

**❌ 400 Bad Request (validation error)**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "": ["Both Lat and Lng must be provided together, or both omitted."]
  }
}
```

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
  "version": 2,
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
        ],
        "sold": {
          "lifetime": 1284,
          "rolling7": 86,
          "rolling30": 412,
          "lastSoldAt": "2025-10-12T18:41:52Z",
          "lastUpdatedAt": "2025-10-12T18:42:05Z"
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

> **New in version 2**: Each `items.byId` entry now includes a `sold` object containing lifetime and rolling sales counts. These values update whenever delivered order totals change, and ETag/Last-Modified headers advance accordingly. Items with no sales return zero counts and `null` timestamps.

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
| `description` | `string|  `itemId` | `UUID` | Menu item identifier | |null` | Category description |
| `displayOrder` | `number` | Sort order for display |
| `items` | `array` | Array of menu items in this category |

#### Menu Item Object

| Field | Type | Description |
|-------|------|-------------|
| `itemId` | `UUID` | Item identifier |
| `name` | `string` | Item name |
| `description` | `string|  `itemId` | `UUID` | Menu item identifier | |null` | Item description |
| `basePrice` | `Money` | Base price before customizations |
| `isAvailable` | `boolean` | Whether item is currently available |
| `dietaryTags` | `string[]` | Dietary information (e.g., "Vegetarian", "Gluten-Free") |
| `customizationGroups` | `array` | Available customization options |
| `sold` | `SoldMetrics` | Sales popularity counters (defaults to zeros/null when no sales) |

#### Sold Metrics Object

| Field | Type | Description |
|-------|------|-------------|
| `lifetime` | `number` | Total delivered quantity accumulated for the item |
| `rolling7` | `number` | Quantity delivered in the trailing 7 days |
| `rolling30` | `number` | Quantity delivered in the trailing 30 days |
| `lastSoldAt` | `string|null` | ISO 8601 timestamp of the most recent delivered order (or `null` if never sold) |
| `lastUpdatedAt` | `string|null` | ISO 8601 timestamp when the counters were last refreshed |

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
| `title` | `string|  `itemId` | `UUID` | Menu item identifier | |null` | Optional review title |
| `comment` | `string|  `itemId` | `UUID` | Menu item identifier | |null` | Review text content |
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
| `lastReviewAtUtc` | `string|  `itemId` | `UUID` | Menu item identifier | |null` | ISO 8601 timestamp of most recent review |
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




