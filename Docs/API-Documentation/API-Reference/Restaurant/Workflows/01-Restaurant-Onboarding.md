# Workflow: Restaurant Onboarding

End-to-end provider onboarding: submitting a restaurant registration, admin review, and provisioning a verified restaurant with owner access.

—

## Overview

- States: `Pending` → `Approved` or `Rejected`.
- On approval: a Restaurant is created and Verified; Not Accepting Orders by default; submitter is granted `Owner` role for the restaurant.
- Actors: Applicant (authenticated user), Administrator.

—

## 1) Submit Registration (Applicant)

POST /api/v1/restaurant-registrations

- Authorization: CompletedSignup (authenticated domain user)

Request Body
```json
{
  "name": "Pasta Palace",
  "description": "Fresh handmade pasta",
  "cuisineType": "Italian",
  "street": "123 Market St",
  "city": "San Francisco",
  "state": "CA",
  "zipCode": "94105",
  "country": "USA",
  "phoneNumber": "+1 (415) 555-2121",
  "email": "owner@pastapalace.example",
  "businessHours": "09:00-17:30",
  "logoUrl": "https://cdn.example.com/logo.png",
  "latitude": 37.7936,
  "longitude": -122.3965
}
```

Business Rules & Validations
- Name ≤ 100, Description ≤ 500, CuisineType ≤ 50.
- Street ≤ 200; City/State ≤ 100; ZipCode ≤ 20; Country ≤ 100.
- Phone ≤ 30; Email must be valid; BusinessHours ≤ 200 and must be `HH:mm-HH:mm`.
- Optional LogoUrl must be a valid absolute URL.
- If provided: −90 ≤ Latitude ≤ 90; −180 ≤ Longitude ≤ 180.

Response
- 201 Created
```json
{ "registrationId": "5f2c1a9e-..." }
```

Error Responses
- 400 Validation — field length, email format, hours format, geo ranges.
- 401 Unauthorized — missing/invalid auth or signup incomplete.

—

## 2) List My Registrations (Applicant)

GET /api/v1/restaurant-registrations/mine

- Authorization: Authenticated

Response 200
```json
[
  {
    "registrationId": "5f2c1a9e-...",
    "name": "Pasta Palace",
    "city": "San Francisco",
    "status": "Pending",
    "submittedAtUtc": "2025-09-14T10:00:00Z",
    "reviewedAtUtc": null,
    "reviewNote": null,
    "submitterUserId": "90b3..."
  }
]
```

Error Responses
- 401 Unauthorized

—

## 3) List Pending (Admin)

GET /api/v1/restaurant-registrations/admin/pending?pageNumber=1&pageSize=10

- Authorization: Administrator
- Query: pageNumber (default 1), pageSize (default 10)

Response 200
- Paginated list of registration summaries (same shape as above items), newest first.

Error Responses
- 401 Unauthorized, 403 Forbidden.

—

## 4) Approve or Reject (Admin)

Approve Registration

POST /api/v1/restaurant-registrations/admin/{registrationId}/approve

- Authorization: Administrator
- Path: registrationId (UUID)
- Body (optional)
```json
{ "note": "Looks good" }
```

Behavior
- Provisions a Restaurant (Verified) using submitted data.
- Grants the submitter `Owner` role for the new restaurant.

Response 200
```json
{ "restaurantId": "e3d6d9a1-..." }
```

Error Responses
- 404 Not Found — registration not found.
- 401/403 — unauthorized/forbidden.

Reject Registration

POST /api/v1/restaurant-registrations/admin/{registrationId}/reject

- Authorization: Administrator
- Body
```json
{ "reason": "Insufficient information" }
```

Response
- 204 No Content

Error Responses
- 400 Validation — reason required.
- 404 Not Found — registration not found.
- 401/403 — unauthorized/forbidden.

—

## 5) Post-Approval Provider Setup (Owner/Staff)

After approval, the owner can complete setup using Provider APIs:
- Profile & Operations: `PUT /restaurants/{id}/profile`, `/business-hours`, `/location`, `/accepting-orders`.
- Menu Management: create menus/categories/items; set availability and pricing.
- Coupons: create/update/enable/disable; details/stats.
- Orders: operate lifecycle (accept/reject/cancel/preparing/ready/delivered).

See:
- 01-Restaurant-Profile-and-Operations.md
- 02-Menu-Management.md
- 03-Order-Operations.md
- 04-Coupon-Management.md

—

## Security & Consistency Notes

- Admin endpoints require `Administrator` role.
- Approval is transactional: registration approval, restaurant provisioning (Verified), and owner role assignment succeed/fail together.
- Provider endpoints enforce restaurant tenancy on every request.

Versioning: All examples use `/api/v1/`.