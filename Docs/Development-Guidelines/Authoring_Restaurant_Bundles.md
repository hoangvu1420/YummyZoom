# Authoring Restaurant Bundle Files

This guide explains how to create or extend a single JSON file ("bundle") that fully describes a restaurant for demos and testing. A bundle includes the restaurant profile, one menu, categories, items, optional customization groups, and optional tags.

Note: Your team or tooling will tell you where to save the file. The filename format is `<restaurant-slug>.restaurant.json`.

## Quick Start

1) Duplicate an existing bundle and rename it (e.g., `my-new-spot.restaurant.json`).
2) Set a lowercase, hyphenated `restaurantSlug` (regex: `^[a-z0-9]+(?:-[a-z0-9]+)*$`).
3) Fill in required fields: `name`, `cuisineType`, `address`, `contact`, `businessHours`, and `menu`.
4) (Optional) Set `defaultCurrency` to a 3‑letter code (e.g., `USD`, `VND`). All price amounts in the bundle are interpreted in this currency. If omitted, `USD` is assumed.
5) (Optional) Add `customizationGroups` (e.g., `Size`, `Extras`) and reference them from items.
6) Add categories and items; attach dietary tags by name (e.g., `"Vegetarian"`).
7) Validate using your team’s preview workflow, then submit.

## Bundle Structure (JSON)

```json
{
  "restaurantSlug": "my-new-restaurant",
  "name": "My New Restaurant",
  "description": "Short marketing description",
  "cuisineType": "Italian",
  "logoUrl": "https://.../logo.png",
  "backgroundImageUrl": "https://.../bg.png",
  "defaultCurrency": "USD",
  "isVerified": true,
  "isAcceptingOrders": true,
  "address": { "street": "...", "city": "...", "state": "...", "zipCode": "...", "country": "..." },
  "contact": { "phone": "+1 (555) 123-4567", "email": "info@mynewrestaurant.com" },
  "businessHours": "11:00-22:00",
  "tags": [
    { "tagName": "Vegetarian", "tagCategory": "Dietary" }
  ],
  "customizationGroups": [
    {
      "groupKey": "Size",
      "minSelections": 1,
      "maxSelections": 1,
      "choices": [
        { "name": "Small",  "priceAdjustment": 0.00, "isDefault": true,  "displayOrder": 1 },
        { "name": "Medium", "priceAdjustment": 1.00, "isDefault": false, "displayOrder": 2 },
        { "name": "Large",  "priceAdjustment": 2.00, "isDefault": false, "displayOrder": 3 }
      ]
    }
  ],
  "menu": {
    "name": "Main Menu",
    "description": "All day menu",
    "categories": [
      {
        "name": "Appetizers",
        "displayOrder": 1,
        "items": [
          {
            "name": "Bruschetta",
            "description": "Grilled bread, tomato, basil, garlic",
            "basePrice": 7.99,
            "imageUrl": "https://.../bruschetta.jpg",
            "isAvailable": true,
            "dietaryTags": ["Vegetarian"],
            "customizationGroups": ["Size"]
          }
        ]
      }
    ]
  }
}
```

## Validation Rules

Required
- `restaurantSlug`: lowercase + hyphens only.
- `name`, `cuisineType`: non-empty.
- `address`: `street`, `city`, `state`, `zipCode`, `country`.
- `contact`: `phone` and `email`.
- `businessHours`: 24h format `hh:mm-hh:mm` with start < end.
- `menu`: `name`, `description`, `categories[]` (at least one).
- `defaultCurrency` (optional): 3‑letter currency code (e.g., `USD`, `VND`). If omitted, prices are treated as USD.

Formatting & Constraints
- Phone: digits and `()+-.` allowed, min length 10.
- Email: simple `local@domain.tld`.
- Categories: `displayOrder` > 0; names unique within the menu.
- Items: names unique within their category; `description` non-empty; `basePrice` > 0. The value is in `defaultCurrency`.
- Customization groups: `groupKey` unique within file; `maxSelections >= minSelections`; at least one `choice` with unique names.
- Choice `priceAdjustment` values are in `defaultCurrency`.
- Item `customizationGroups[]` must reference a defined `groupKey` in the same file.
- `tags[]` (optional): each entry must have a valid `tagCategory` string; duplicates by (name, category) are not allowed.

Dietary Tags
- Attach to items by `dietaryTags`: array of tag names (e.g., `"Vegan"`, `"Gluten-Free"`).
- Tag names should either be common ones your team already uses (e.g., `Vegetarian`, `Vegan`, `Gluten-Free`) or be added in the bundle’s `tags[]` with `tagCategory="Dietary"`.

## Authoring Tips

- Start from a working bundle and tweak gradually.
- Keep item names stable to avoid accidental duplicates.
- Use `groupKey` as a stable reference; reference it from items via `customizationGroups`.
- Keep prices realistic. Set `defaultCurrency` once per bundle, then express all prices in that currency (items and choice adjustments). If you don’t set it, prices are treated as `USD`.
- Use image URLs even if placeholders to help frontend testing.

## Working Checklist

- File name follows `<slug>.restaurant.json` and slug is lowercase with hyphens.
- All required fields are present and non-empty.
- Business hours follow `hh:mm-hh:mm` (24h) and open time < close time.
- Categories have unique names and positive `displayOrder`.
- Items have unique names within a category and positive `basePrice`.
- Any item `customizationGroups` entries match a defined `groupKey`.
- Dietary tag names are spelled exactly as intended (e.g., `Vegetarian`, `Vegan`).
- If using a non-USD currency, confirm amounts are realistic for that market.

## Extending an Existing Bundle

- Add a new category: append to `menu.categories[]` with a unique `displayOrder`.
- Add items: ensure unique `name` within the category; add `dietaryTags` and `customizationGroups` as needed.
- Add/modify a customization group: edit `customizationGroups[]` and reference it from items by `groupKey`.
- Safe updates:
  - By default, re-seeding creates missing rows and skips existing ones.
  - To overwrite text/price fields, enable the appropriate update flags in your team’s workflow.

## Common Validation Errors and Fixes

- "restaurantSlug must be lowercase with hyphens": rename slug.
- "businessHours must be 'hh:mm-hh:mm'": fix time format and ensure start < end.
- "Duplicate category name": rename or merge categories.
- "Duplicate item name in category": rename item or remove duplicate.
- "Item references missing customization groupKey": define the group in `customizationGroups[]`.
- "Invalid defaultCurrency": use a 3‑letter code like `USD`, `VND`, `EUR`.
- "tags[].tagCategory must be one of ...": use a valid category (e.g., `Dietary`, `Cuisine`, `SpiceLevel`).

## Reviewing and Submitting

- Preview: Use your team’s preview/dry‑run workflow to validate the bundle before it affects shared environments.
- Naming: Keep display names user‑friendly; keep keys/slugs stable over time to avoid duplicates.
- Images: Prefer realistic image sizes/ratios; placeholder URLs are acceptable during development.
- Prices: Use realistic values expressed in `defaultCurrency`. For zero‑cost size changes, use `0.00`.
- Accessibility: Write clear item descriptions (what it is, key ingredients, allergens if relevant).

---

For examples, check existing `*.restaurant.json` files alongside your changes.

