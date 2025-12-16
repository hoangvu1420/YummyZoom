# Menu Management (Provider)

Base path: `/api/v1/`

These endpoints allow authorized restaurant staff to manage menus, categories, items, and item customizations for a restaurant.

Authorization: Unless stated otherwise, all endpoints require the caller to satisfy policy `MustBeRestaurantStaff` for the `restaurantId`. Creating menus requires `MustBeRestaurantOwner`.

---

## Menus

### GET /restaurants/{restaurantId}/menus

List menus for management with basic counts.

- Authorization: MustBeRestaurantStaff

#### Path Parameters

- `restaurantId` (UUID) — Restaurant scope.

#### Response 200

```json
[
  {
    "menuId": "bb1b1a8b-...",
    "name": "Main Menu",
    "description": "Everyday items",
    "isEnabled": true,
    "lastModified": "2025-09-14T10:22:10Z",
    "categoryCount": 6,
    "itemCount": 42
  }
]
```

---

### POST /restaurants/{restaurantId}/menus

Create a new menu.

- Authorization: MustBeRestaurantOwner

#### Path Parameters

- `restaurantId` (UUID)

#### Request Body

```json
{ "name": "Lunch", "description": "Weekday lunch", "isEnabled": true }
```

- `name` (string, required, not empty)
- `description` (string, required, not empty)
- `isEnabled` (bool, default true)

#### Response 201

```json
{ "menuId": "7d2b1a62-..." }
```

#### Errors

- 400 Validation — `Menu.InvalidMenuName`, `Menu.InvalidMenuDescription`
- 404 Not Found — invalid restaurant scope
- 409 Conflict — domain conflicts

---

### PUT /restaurants/{restaurantId}/menus/{menuId}

Update menu name/description.

- Authorization: MustBeRestaurantStaff

#### Path Parameters

- `restaurantId` (UUID)
- `menuId` (UUID)

#### Request Body

```json
{ "name": "Main Menu", "description": "Updated description" }
```

#### Response

- 204 No Content

#### Errors

- 400 Validation — `Menu.InvalidMenuName`, `Menu.InvalidMenuDescription`
- 404 Not Found — `Menu.InvalidMenuId`

---

### PUT /restaurants/{restaurantId}/menus/{menuId}/availability

Enable or disable a menu.

- Authorization: MustBeRestaurantStaff

#### Request Body

```json
{ "isEnabled": false }
```

#### Response

- 204 No Content

#### Errors

- 404 Not Found — `Menu.InvalidMenuId`

---

## Categories

### GET /restaurants/{restaurantId}/menus/{menuId}/categories

List categories in a menu for management (sidebar/backoffice views).

- Authorization: MustBeRestaurantStaff

#### Notes

- Returns only non-deleted categories for the menu.
- `itemCount` excludes soft-deleted menu items.
- Sorted by `displayOrder` ascending, then by `categoryId` for stability. Empty array when none exist.

#### Path Parameters

- `restaurantId` (UUID)
- `menuId` (UUID)

#### Response 200

```json
[
  {
    "categoryId": "c1",
    "name": "Appetizers",
    "displayOrder": 1,
    "itemCount": 5
  },
  {
    "categoryId": "c2",
    "name": "Mains",
    "displayOrder": 2,
    "itemCount": 12
  }
]
```

#### Errors

- 400 Validation — missing/invalid `restaurantId` or `menuId`
- 404 Not Found — `Menu.InvalidMenuId`

---

### GET /restaurants/{restaurantId}/categories/{categoryId}

Get menu category details.

- Authorization: MustBeRestaurantStaff

#### Response 200

```json
{
  "menuId": "a1b2...",
  "menuName": "Main Menu",
  "categoryId": "c1d2...",
  "name": "Burgers",
  "displayOrder": 3,
  "itemCount": 12,
  "lastModified": "2025-09-14T10:22:10Z"
}
```

#### Errors

- 404 Not Found — `Management.GetMenuCategoryDetails.NotFound`

---

### GET /restaurants/{restaurantId}/categories/{categoryId}/items?pageNumber=1&pageSize=10&isAvailable=true&q=cheese

List items in a category (paginated, with optional filters).

- Authorization: MustBeRestaurantStaff

#### Response 200

Paginated list of `MenuItemSummaryDto` (id, name, price, currency, isAvailable, imageUrl, lastModified).

#### Errors

- 404 Not Found — `Management.GetMenuItemsByCategory.NotFound`

---

### POST /restaurants/{restaurantId}/menus/{menuId}/categories

Add a category to a menu.

- Authorization: MustBeRestaurantStaff

#### Request Body

```json
{ "name": "Desserts" }
```

- Name must be non-empty. Display order is auto-assigned (last + 1).

#### Response 201

```json
{ "menuCategoryId": "ef23..." }
```

#### Errors

- 400 Validation — `Menu.InvalidCategoryName`, `Menu.InvalidDisplayOrder`
- 404 Not Found — `Menu.InvalidMenuId`
- 409 Conflict — `Menu.DuplicateCategoryName`

---

### PUT /restaurants/{restaurantId}/categories/{categoryId}

Update category name and/or display order.

- Authorization: MustBeRestaurantStaff

#### Request Body

```json
{ "name": "Mains", "displayOrder": 2 }
```

- `name` non-empty, `displayOrder` > 0

#### Response

- 204 No Content

#### Errors

- 400 Validation — `Menu.InvalidCategoryName`, `Menu.InvalidDisplayOrder`
- 404 Not Found — `Menu.CategoryNotFound`

---

### PUT /restaurants/{restaurantId}/categories/reorder

Bulk-update category display orders (e.g., after drag-and-drop).

- Authorization: MustBeRestaurantStaff

#### Request Body

```json
{
  "categoryOrders": [
    { "categoryId": "c1", "displayOrder": 1 },
    { "categoryId": "c2", "displayOrder": 2 }
  ]
}
```

Rules

- `categoryOrders` required and non-empty; must include every category in the restaurant (no omissions).
- `categoryId` must belong to `restaurantId` and not be deleted; all provided IDs are validated before any update is applied.
- `displayOrder` must be a unique, gapless sequence starting at 1 (i.e., 1..N for N categories); categoryIds and displayOrders must be unique within the request.

#### Response

- 204 No Content

#### Errors

- 400 Validation — missing/duplicate IDs, duplicate displayOrder, invalid displayOrder
- 404 Not Found — `Menu.Reorder.CategoryNotFound` (any provided category not in the restaurant)
- 400 Validation — `Menu.Reorder.IncompleteCategoryList`, `Menu.Reorder.InvalidDisplayOrderRange`

---

### DELETE /restaurants/{restaurantId}/categories/{categoryId}

Remove (soft-delete) a category.

- Authorization: MustBeRestaurantStaff

#### Response

- 204 No Content

#### Errors

- 404 Not Found — `Menu.CategoryNotFound`

---

## Items

### GET /restaurants/{restaurantId}/menu-items/{itemId}/management

Get full item details (management).

- Authorization: MustBeRestaurantStaff

#### Response 200

```json
{
  "itemId": "9a8b...",
  "categoryId": "c1d2...",
  "name": "Cheeseburger",
  "description": "100% beef, cheddar",
  "priceAmount": 9.99,
  "priceCurrency": "USD",
  "isAvailable": true,
  "imageUrl": "https://cdn.example.com/items/cheeseburger.png",
  "dietaryTagIds": ["da1..."],
  "appliedCustomizations": [
    { "groupId": "g1...", "displayTitle": "Cheese Choice", "displayOrder": 1 }
  ],
  "lastModified": "2025-09-14T10:22:10Z"
}
```

#### Errors

- 404 Not Found — `Management.GetMenuItemDetails.NotFound`

> Note
>
> The management details route has a distinct path (`/management`) to avoid ambiguity with the public menu item details endpoint used by customers.

---

### GET /restaurants/{restaurantId}/menu-items/search?q=pho&categoryId=&isAvailable=true&pageNumber=1&pageSize=20

Search menu items across all categories for a restaurant (paginated).

- Authorization: MustBeRestaurantStaff

Query parameters

- `q` (string, optional) — case-insensitive name contains search
- `categoryId` (UUID, optional) — restrict to a specific category; must belong to `restaurantId`
- `isAvailable` (bool, optional) — filter by availability flag
- `pageNumber` (int, optional, default 1, >= 1)
- `pageSize` (int, optional, default 20, >= 1)

#### Response 200

Paginated list of `MenuItemSearchResultDto`:

```json
{
  "items": [
    {
      "itemId": "9a8b...",
      "menuCategoryId": "c1d2...",
      "categoryName": "Beverages",
      "name": "Iced Coffee",
      "priceAmount": 39000,
      "priceCurrency": "VND",
      "isAvailable": true,
      "imageUrl": "https://...",
      "lastModified": "2025-09-14T10:22:10Z"
    }
  ],
  "totalCount": 12,
  "pageNumber": 1,
  "pageSize": 20
}
```

Sorting: Name ASC then ItemId ASC (deterministic). When `categoryId` is provided and the category is missing/soft-deleted or belongs to another restaurant, returns 404.

#### Errors

- 404 Not Found — `Management.SearchMenuItems.CategoryNotFound`

---

### POST /restaurants/{restaurantId}/menu-items

Create a new item in a category.

- Authorization: MustBeRestaurantStaff

#### Request Body

```json
{
  "menuCategoryId": "c1d2...",
  "name": "Cheeseburger",
  "description": "100% beef, cheddar",
  "price": 9.99,
  "currency": "USD",
  "imageUrl": "https://...",
  "isAvailable": true,
  "dietaryTagIds": ["da1..."]
}
```

Field rules

- `name` required, non-empty
- `description` required, non-empty
- `price` > 0 with `currency`
- `menuCategoryId` must exist and belong to the same restaurant
- `dietaryTagIds` optional; empty clears

#### Response 201

```json
{ "menuItemId": "9a8b..." }
```

#### Errors

- 404 Not Found — `MenuItem.CategoryNotFound`
- 400 Validation — `MenuItem.CategoryNotBelongsToRestaurant`, `MenuItem.NegativePrice`, `MenuItem.InvalidName`, `MenuItem.InvalidDescription`

---

### PUT /restaurants/{restaurantId}/menu-items/{itemId}

Update item core details (name, description, price, currency, imageUrl).

- Authorization: MustBeRestaurantStaff

#### Request Body

```json
{
  "name": "Cheeseburger Deluxe",
  "description": "Beef, cheddar, tomato",
  "price": 10.49,
  "currency": "USD",
  "imageUrl": "https://.../deluxe.png"
}
```

#### Response

- 204 No Content

#### Errors

- 404 Not Found — `MenuItem.MenuItemNotFound`
- 400 Validation — `MenuItem.InvalidName`, `MenuItem.InvalidDescription`, `MenuItem.NegativePrice`
- 403 Forbidden — item not in `restaurantId`

---

### PUT /restaurants/{restaurantId}/menu-items/{itemId}/price

Change item price only.

- Authorization: MustBeRestaurantStaff

#### Request Body

```json
{ "price": 10.49, "currency": "USD" }
```

#### Response

- 204 No Content

#### Errors

- 404 Not Found — `MenuItem.MenuItemNotFound`
- 400 Validation — `MenuItem.NegativePrice`
- 403 Forbidden — item not in `restaurantId`

---

### POST /restaurants/{restaurantId}/menu-items/batch-update

Bulk update availability or price for multiple items in one request. Executes best-effort: continues after individual failures and reports them in the response.

- Authorization: MustBeRestaurantStaff

#### Request Body

```json
{
  "operations": [
    { "itemId": "9a8b...", "field": "isAvailable", "value": false },
    { "itemId": "c0c1...", "field": "price", "value": 199000 }
  ]
}
```

Rules

- Up to 50 operations per call; `operations` is required and non-empty.
- `field` must be `isAvailable` (expects boolean) or `price` (expects numeric in the item’s currency).
- Each `itemId` must belong to `restaurantId`; cross-restaurant items return 403.
- Processing continues after per-item failures; failures are reported in the response.

#### Response 200

```json
{
  "successCount": 1,
  "failedCount": 2,
  "errors": [
    { "itemId": "deadbeef-...", "field": "isAvailable", "message": "Menu item 'deadbeef-...' was not found." },
    { "itemId": "9a8b...", "field": "price", "message": "MenuItem.InvalidPriceValue" }
  ]
}
```

- `errors` entries contain human-readable `message` values (domain/validation descriptions). Successful operations are applied transactionally per item.

#### Errors

- 400 Validation — invalid payload (missing operations, unsupported field, wrong value type)
- 403 Forbidden — item does not belong to `restaurantId`

---

### PUT /restaurants/{restaurantId}/menu-items/{itemId}/availability

Toggle availability.

- Authorization: MustBeRestaurantStaff

#### Request Body

```json
{ "isAvailable": false }
```

#### Response

- 204 No Content (idempotent if unchanged)

#### Errors

- 404 Not Found — `MenuItem.MenuItemNotFound`
- 403 Forbidden — item not in `restaurantId`

---

### PUT /restaurants/{restaurantId}/menu-items/{itemId}/category

Move item to a different category.

- Authorization: MustBeRestaurantStaff

#### Request Body

```json
{ "newCategoryId": "c2e3" }
```

Rules: target category must exist and belong to same restaurant.

#### Response

- 204 No Content

#### Errors

- 404 Not Found — `MenuItem.MenuItemNotFound`, `MenuItem.CategoryNotFound`
- 400 Validation — `MenuItem.CategoryNotBelongsToRestaurant`
- 403 Forbidden — item not in `restaurantId`

---

### PUT /restaurants/{restaurantId}/menu-items/{itemId}/dietary-tags

Replace dietary tag set.

- Authorization: MustBeRestaurantStaff

#### Request Body

```json
{ "dietaryTagIds": ["da1...", "db2..."] }
```

- Null or empty list clears all tags.

#### Response

- 204 No Content

#### Errors

- 404 Not Found — `MenuItem.MenuItemNotFound`
- 403 Forbidden — item not in `restaurantId`

---

### POST /restaurants/{restaurantId}/menu-items/{itemId}/customizations

Assign a customization group to an item.

- Authorization: MustBeRestaurantStaff

#### Request Body

```json
{ "groupId": "g1...", "displayTitle": "Cheese Choice", "displayOrder": 1 }
```

Rules

- Group must exist and belong to the same restaurant.
- If `displayOrder` is omitted, it is set to last + 1.
- Cannot assign the same group twice to the same item.

#### Response

- 204 No Content

#### Errors

- 404 Not Found — `MenuItem.MenuItemNotFound`, `CustomizationGroup.NotFound`
- 400 Validation — `CustomizationGroup.NotBelongsToRestaurant`, `MenuItem.CustomizationAlreadyAssigned`
- 403 Forbidden — item not in `restaurantId`

---

### DELETE /restaurants/{restaurantId}/menu-items/{itemId}/customizations/{groupId}

Remove a customization group from an item.

- Authorization: MustBeRestaurantStaff

#### Response

- 204 No Content

#### Errors

- 404 Not Found — `MenuItem.MenuItemNotFound`, `MenuItem.CustomizationNotFound`
- 403 Forbidden — item not in `restaurantId`

---

### DELETE /restaurants/{restaurantId}/menu-items/{itemId}

Soft-delete an item.

- Authorization: MustBeRestaurantStaff

#### Response

- 204 No Content

#### Errors

- 404 Not Found — `MenuItem.MenuItemNotFound`
- 403 Forbidden — item not in `restaurantId`

---

## Business Rules & Side Effects

- All mutating operations are scoped to `restaurantId`; handlers enforce ownership before state changes.
- Menu/category/item changes emit domain events (e.g., `MenuCreated`, `MenuCategoryAdded`, `MenuItemCreated`, `MenuItemAvailabilityChanged`, `MenuItemPriceChanged`) used for read-model/search updates.
- Item availability toggles are idempotent; an event is emitted only when the state changes.
- Deletions are soft-deletes at the domain level and emit corresponding events; records may remain in audit trails.

## Errors & Responses

- Standard responses follow platform helpers:
  - Reads: 200 with body (`WithStandardResults<T>`)
  - Mutations: 204 No Content (`WithStandardResults`)
  - Creations: 201 Created with id (`WithStandardCreationResults<T>`)
- Common error codes are listed inline above. See the Error Codes appendix when available for object schema.

Versioning: All routes are under `/api/v1/`.

Security: Bearer token required; caller must be Owner/Staff in the restaurant’s scope.
