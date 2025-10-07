# Staff & Roles (Provider)

Base path: `/api/v1/`

This section explains how provider access is modeled and how to administer staff access to a restaurant. Access is enforced both at the endpoint layer and, critically, in the application command policies.

---

## Role Model

- RestaurantRole values
  - `Owner` — Full management privileges for the restaurant (e.g., create menus). Some commands explicitly require Owner.
  - `Staff` — Operational privileges (menu/category/item updates, order operations, coupon management, etc.).

- Authorization policies (used across provider commands)
  - `MustBeRestaurantOwner` — Caller must be Owner for the `restaurantId`.
  - `MustBeRestaurantStaff` — Caller must be Owner or Staff for the `restaurantId`.
  - Additional policies exist for user, order, and team cart scoping; provider docs primarily rely on the two above.

Examples
- Creating a menu requires Owner: `CreateMenuCommand` is annotated with `MustBeRestaurantOwner`.
- Most restaurant management (profile, hours, location), menu management, order operations, and coupon endpoints require `MustBeRestaurantStaff`.

---

## Role Assignment Administration

Role assignments are managed under the Users route. These endpoints are Administrator-only. They create or remove the mapping of a user to a specific restaurant with a specific `RestaurantRole`.

### Create Role Assignment

POST /users/role-assignments

- Authorization: `Administrator`

#### Request Body
```json
{
  "userId": "6f9b4e31-...",
  "restaurantId": "a1b2c3d4-...",
  "role": "Owner"  // or "Staff"
}
```

#### Response 200
```json
{ "roleAssignmentId": "b5a1..." }
```

#### Errors
- 404 Not Found — `RoleAssignment.UserNotFound`, `RoleAssignment.RestaurantNotFound`
- 409 Conflict — `RoleAssignment.DuplicateAssignment`
- 400 Validation — `RoleAssignment.InvalidRole`

Business rules
- The combination (userId, restaurantId, role) must be unique.
- The user must exist in the identity store.

---

### Delete Role Assignment

DELETE /users/role-assignments/{roleAssignmentId}

- Authorization: `Administrator`

#### Path Parameters
- `roleAssignmentId` (UUID)

#### Response
- 204 No Content

#### Errors
- 404 Not Found — `RoleAssignment.NotFound`

---

## How Roles Affect Provider Endpoints

- Profile & Operations: staff (Owner/Staff) can update profile, hours, location, and accepting-orders; creating menus requires Owner.
- Menu Management: staff can create/update menu items, categories, and toggle menu availability; creating a menu requires Owner.
- Orders: staff can fetch restaurant queues and perform lifecycle actions (Accept/Reject/Cancel/Preparing/Ready/Delivered) scoped to their restaurant.
- Coupons: staff can create, update, enable/disable, delete, and query details/stats for coupons scoped to their restaurant.

All provider endpoints validate `restaurantId` consistency server-side to prevent cross-tenant access.

---

## Best Practices

- Use Owner role sparingly; default to Staff for day-to-day operations.
- Automate Owner assignment during onboarding (registration approval grants Owner to submitter).
- Rotate Staff roles as employees join/leave; remove role assignments immediately when access is no longer needed.

Versioning: All routes use `/api/v1/`.