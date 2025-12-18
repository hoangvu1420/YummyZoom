# Customization Management (Provider)

Base path: `/api/v1/restaurants/{restaurantId}/customization-groups`

These endpoints allow authorized restaurant staff to manage customization groups and their choices. Customization groups (e.g., "Size", "Toppings") can be reused across multiple menu items.

Authorization: All endpoints require the caller to satisfy the policy `MustBeRestaurantStaff` for the `restaurantId`.

---

## Customization Groups

### GET /restaurants/{restaurantId}/customization-groups

List all customization groups for a restaurant. This is used to populate selection lists when adding customizations to menu items.

- Authorization: MustBeRestaurantStaff

#### Path Parameters

- `restaurantId` (UUID) — Restaurant scope.

#### Response 200

Returns a list of `CustomizationGroupSummaryDto`.

```json
[
  {
    "id": "b1b2...",
    "name": "Size",
    "minSelections": 1,
    "maxSelections": 1,
    "choiceCount": 3
  },
  {
    "id": "c3c4...",
    "name": "Toppings",
    "minSelections": 0,
    "maxSelections": 5,
    "choiceCount": 10
  }
]
```

---

### GET /restaurants/{restaurantId}/customization-groups/{groupId}

Get full details of a customization group, including its choices.

- Authorization: MustBeRestaurantStaff

#### Path Parameters

- `restaurantId` (UUID)
- `groupId` (UUID)

#### Response 200

Returns `CustomizationGroupDetailsDto`.

```json
{
  "id": "b1b2...",
  "name": "Size",
  "minSelections": 1,
  "maxSelections": 1,
  "choices": [
    {
      "id": "e5e6...",
      "name": "Small",
      "priceAmount": 0,
      "priceCurrency": "USD",
      "isDefault": true,
      "displayOrder": 1
    },
    {
      "id": "f7f8...",
      "name": "Large",
      "priceAmount": 1.50,
      "priceCurrency": "USD",
      "isDefault": false,
      "displayOrder": 2
    }
  ]
}
```

#### Errors

- 404 Not Found — `CustomizationGroupErrors.NotFound`

---

### POST /restaurants/{restaurantId}/customization-groups

Create a new customization group.

- Authorization: MustBeRestaurantStaff

#### Request Body

```json
{
  "name": "Spiciness Level",
  "minSelections": 1,
  "maxSelections": 1
}
```

- `name` (string, required, max 200 chars)
- `minSelections` (int, >= 0)
- `maxSelections` (int, >= 0, >= minSelections)

#### Response 201

```json
{
  "id": "g9h0..."
}
```

#### Errors

- 400 Validation — `CustomizationGroup.InvalidName`, mixed min/max rules.

---

### PUT /restaurants/{restaurantId}/customization-groups/{groupId}

Update a customization group's details (name and selection limits).

- Authorization: MustBeRestaurantStaff

#### Request Body

```json
{
  "name": "Spiciness Level (Revised)",
  "minSelections": 0,
  "maxSelections": 1
}
```

#### Response

- 204 No Content

#### Errors

- 404 Not Found — `CustomizationGroupErrors.NotFound`
- 400 Validation — Invalid input.

---

### DELETE /restaurants/{restaurantId}/customization-groups/{groupId}

Soft-delete a customization group.

- Authorization: MustBeRestaurantStaff

#### Response

- 204 No Content

#### Errors

- 404 Not Found — `CustomizationGroupErrors.NotFound`

---

## Customization Choices

### POST /restaurants/{restaurantId}/customization-groups/{groupId}/choices

Add a new choice option to a customization group.

- Authorization: MustBeRestaurantStaff

#### Request Body

```json
{
  "name": "Mild",
  "priceAdjustment": 0,
  "currency": "USD",
  "isDefault": true,
  "displayOrder": 1
}
```

- `name` (string, required, max 100)
- `priceAdjustment` (decimal, >= 0)
- `currency` (string, required, 3 chars)
- `isDefault` (bool)
- `displayOrder` (int, optional)

#### Response 201

```json
{
  "id": "i1j2..."
}
```

#### Errors

- 404 Not Found — `CustomizationGroupErrors.NotFound`
- 400 Validation — Duplicate name within group, invalid price.

---

### PUT /restaurants/{restaurantId}/customization-groups/{groupId}/choices/{choiceId}

Update an existing choice option.

- Authorization: MustBeRestaurantStaff

#### Path Parameters

- `restaurantId` (UUID)
- `groupId` (UUID)
- `choiceId` (UUID)

#### Request Body

```json
{
  "name": "Extra Spicy",
  "priceAdjustment": 0.50,
  "currency": "USD",
  "isDefault": false,
  "displayOrder": 3
}
```

#### Response

- 204 No Content

#### Errors

- 404 Not Found — `CustomizationGroupErrors.NotFound` or Choice not found.

---

### DELETE /restaurants/{restaurantId}/customization-groups/{groupId}/choices/{choiceId}

Remove a choice option from a customization group.

- Authorization: MustBeRestaurantStaff

#### Response

- 204 No Content

#### Errors

- 404 Not Found — `CustomizationGroupErrors.NotFound` or Choice not found.

---

### POST /restaurants/{restaurantId}/customization-groups/{groupId}/choices/reorder

Batch reorder multiple choice options within a customization group.

- Authorization: MustBeRestaurantStaff

#### Request Body

```json
{
  "choiceOrders": [
    {
      "choiceId": "e5e6...",
      "displayOrder": 1
    },
    {
      "choiceId": "f7f8...",
      "displayOrder": 2
    }
  ]
}
```

- `choiceOrders`: List of updates. Each entry must have a valid `choiceId` belonging to the group and a non-negative `displayOrder`.

#### Response

- 204 No Content

#### Errors

- 404 Not Found — `CustomizationGroupErrors.NotFound`
- 400 Validation — `CustomizationGroupErrors.ChoiceNotFoundForReordering`, `CustomizationGroupErrors.DuplicateDisplayOrder`

---

## Business Rules

- **Groups**: A customization group defines a set of options (choices) and rules for how many can/must be selected (min/max).
- **Choices**: Each choice has a name and an optional price adjustment.
- **Reuse**: Groups are distinct entities designed to be linked to multiple menu items (linkage is handled via Menu Item endpoints).
- **Soft Delete**: Deleting a group marks it as deleted but does not physically remove it, preserving historical data for orders.
- **Validation**:
    - `MaxSelections` must be greater than or equal to `MinSelections`.
    - Choice names must be unique within a group.
    - Prices cannot be negative.

## Errors & Responses

Standard platform responses apply:
- 200 OK + Body (GET)
- 201 Created + ID (POST)
- 204 No Content (PUT, DELETE)
- 400 Bad Request (Validation failures)
- 401 Unauthorized (Missing/invalid token)
- 403 Forbidden (User not authorized for this restaurant)
- 404 Not Found (Resource does not exist)
