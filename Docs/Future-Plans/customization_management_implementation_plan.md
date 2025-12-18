# Customization Management Implementation Plan

**Date:** 2025-12-17
**Status:** COMPLETED

## 1. Overview
This document outlines the detailed design and implementation plan for the **Full Management API** (Create, Read, Update, Delete) for **Customization Groups** and their **Choices**.

The current system relies on seeded data. To enable a "Menu Editor" feature, Restaurant Staff must be able to fully manage these entities dynamically.

## 2. API Design
The API will follow the RESTful pattern and Minimal APIs standard.

### 2.1. Group Management

#### List Groups
Get all customization groups for a restaurant (e.g., for "Add Customization" dropdowns).
- **Endpoint**: `GET /api/v1/restaurants/{restaurantId}/customization-groups`
- **Authorization**: `MustBeRestaurantStaff`
- **Response**: `200 OK`
  ```json
  [
    {
        "id": "GUID",
        "name": "Size",
        "minSelections": 1,
        "maxSelections": 1,
        "choiceCount": 3
    }
  ]
  ```

#### Get Group Details
Get a specific group with all its choices.
- **Endpoint**: `GET /api/v1/restaurants/{restaurantId}/customization-groups/{groupId}`
- **Authorization**: `MustBeRestaurantStaff`
- **Response**: `200 OK`
  ```json
  {
    "id": "GUID",
    "name": "Size",
    "minSelections": 1,
    "maxSelections": 1,
    "choices": [
        { "id": "GUID", "name": "Small", "price": 0, "currency": "USD", "isDefault": true, "displayOrder": 1 }
    ]
  }
  ```

#### Create Group
- **Endpoint**: `POST /api/v1/restaurants/{restaurantId}/customization-groups`
- **Authorization**: `MustBeRestaurantStaff`
- **Request**: `{ "name": "Size", "minSelections": 1, "maxSelections": 1 }`
- **Response**: `201 Created` `{ "id": "GUID" }`

#### Update Group
Update name and selection limits.
- **Endpoint**: `PUT /api/v1/restaurants/{restaurantId}/customization-groups/{groupId}`
- **Authorization**: `MustBeRestaurantStaff`
- **Request**: `{ "name": "Size (Edited)", "minSelections": 0, "maxSelections": 2 }`
- **Response**: `204 No Content`

#### Delete Group
Soft-delete a group.
- **Endpoint**: `DELETE /api/v1/restaurants/{restaurantId}/customization-groups/{groupId}`
- **Authorization**: `MustBeRestaurantStaff`
- **Response**: `204 No Content`

### 2.2. Choice Management

#### Add Choice
- **Endpoint**: `POST /api/v1/restaurants/{restaurantId}/customization-groups/{groupId}/choices`
- **Authorization**: `MustBeRestaurantStaff`
- **Request**:
  ```json
  {
    "name": "Large",
    "priceAdjustment": 5000,
    "currency": "VND",
    "isDefault": false,
    "displayOrder": 1
  }
  ```
- **Response**: `201 Created` `{ "id": "GUID" }`

#### Update Choice
Update a choice's details.
- **Endpoint**: `PUT /api/v1/restaurants/{restaurantId}/customization-groups/{groupId}/choices/{choiceId}`
- **Authorization**: `MustBeRestaurantStaff`
- **Request**:
  ```json
  {
    "name": "Large (Updated)",
    "priceAdjustment": 6000,
    "currency": "VND",
    "isDefault": true,
    "displayOrder": 2
  }
  ```
- **Response**: `204 No Content`

#### Remove Choice
Remove a specific choice option from a group.
- **Endpoint**: `DELETE /api/v1/restaurants/{restaurantId}/customization-groups/{groupId}/choices/{choiceId}`
- **Authorization**: `MustBeRestaurantStaff`
- **Response**: `204 No Content`

## 3. Application Layer Design (CQRS)

We will use MediatR commands and queries.

### 3.1. Queries (`src/Application/CustomizationGroups/Queries/`)

*   **ListCustomizationGroupsQuery**:
    *   Returns `List<CustomizationGroupSummaryDto>`.
    *   Optimized SQL query (using Dapper or EF projection) to fetch just header info.
*   **GetCustomizationGroupDetailsQuery**:
    *   Returns `CustomizationGroupDetailsDto` (Graph with choices).
    *   Includes full child collection.

### 3.2. Commands (`src/Application/CustomizationGroups/Commands/`)

*   **CreateCustomizationGroupCommand**: (Already planned)
*   **UpdateCustomizationGroupCommand**:
    *   Target: `group.UpdateGroupDetails()`.
*   **DeleteCustomizationGroupCommand**:
    *   Target: `group.MarkAsDeleted()`.
    *   **Side Effect**: Removing a group should probably de-link it from Menu Items or handle data integrity. However, aggregate isolation suggests we just soft-delete the group. The Read Model for Menu Items needs to handle "missing/deleted" groups gracefully, or we need an Event Handler (`CustomizationGroupDeletedEventHandler`) to clean up `AppliedCustomization` references in Menu Items.
*   **AddCustomizationChoiceCommand**: (Already planned)
*   **UpdateCustomizationChoiceCommand**:
    *   Target: `group.UpdateChoice()`.
*   **RemoveCustomizationChoiceCommand**:
    *   Target: `group.RemoveChoice()`.

## 4. Domain Layer Adjustments

1.  **Return IDs**: Update `CustomizationGroup.AddChoice` to return `Result<CustomizationChoice>` or `Result<Guid>` to faciliate the `201 Created` response.
2.  **Validation**: Ensure `UpdateChoice` logic correctly validates new names against duplicates within the group.

## 5. Web Layer Design

**Location**: `src/Web/Endpoints/Restaurants.CustomizationGroups.cs`

Encapsulate all mappings in `MapCustomizationGroups`.

```csharp
private static void MapCustomizationGroups(IEndpointRouteBuilder group)
{
    // Groups
    group.MapGet("/{restaurantId:guid}/customization-groups", ...);     // List
    group.MapGet("/{restaurantId:guid}/customization-groups/{groupId:guid}", ...); // Details
    group.MapPost("/{restaurantId:guid}/customization-groups", ...);    // Create
    group.MapPut("/{restaurantId:guid}/customization-groups/{groupId:guid}", ...); // Update
    group.MapDelete("/{restaurantId:guid}/customization-groups/{groupId:guid}", ...); // Delete

    // Choices
    group.MapPost("/{restaurantId:guid}/customization-groups/{groupId:guid}/choices", ...); // Add
    group.MapPut("/{restaurantId:guid}/customization-groups/{groupId:guid}/choices/{choiceId:guid}", ...); // Update
    group.MapDelete("/{restaurantId:guid}/customization-groups/{groupId:guid}/choices/{choiceId:guid}", ...); // Remove
}
```

## 6. Implementation Checklist

1.  [x] **Domain**: Refactor `AddChoice` return type. (Implemented)
2.  [x] **Application (Read)**: Implement List/Get Queries + DTOs. (Implemented)
3.  [x] **Application (Write)**: Implement Create/Update/Delete Commands + Validators.
4.  [x] **Web**: Create new Endpoint file and map routes.
5.  [x] **Documentation**: Update API Spec.

