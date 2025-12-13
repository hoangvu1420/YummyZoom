# Pricing Preview

This guide covers the pricing preview API that provides authoritative, server-side pricing calculations before order submission. This endpoint allows customers to see accurate pricing breakdowns including subtotals, taxes, delivery fees, discounts, and tips without creating an actual order.

## Overview

The pricing preview feature provides:

- **Real-time Pricing**: Accurate calculations based on current menu prices and availability
- **Coupon Validation**: Real-time coupon validation and discount calculations
- **Customization Support**: Pricing includes all selected customizations and their costs
- **Tax Calculations**: Proper tax calculations based on restaurant-specific policies
- **Delivery Fees**: Accurate delivery fee calculations
- **Performance Optimized**: Cached responses for improved performance
- **Error Handling**: Comprehensive validation and error reporting

All pricing preview endpoints require **authentication**.

---

## Get Pricing Preview

Calculates the complete pricing breakdown for a set of menu items, including subtotal, taxes, delivery fees, discounts, and tips. This endpoint provides authoritative pricing that matches the final order total.

**`POST /api/v1/pricing/preview`**

- **Authorization:** Required (Customer)
- **Performance:** Optimized for <500ms response times with caching
- **Caching:** Responses are cached for 2 minutes to improve performance

### Request Headers

| Header | Type | Required | Description |
|--------|------|----------|-------------|
| `Authorization` | `string` | Yes | Bearer token for customer authentication |
| `Content-Type` | `string` | Yes | Must be `application/json` |

### Request Body

```json
{
  "restaurantId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "items": [
    {
      "menuItemId": "b2c3d4e5-f6g7-8901-bcde-fg2345678901",
      "quantity": 2,
      "customizations": [
        {
          "customizationGroupId": "c3d4e5f6-g7h8-9012-cdef-gh3456789012",
          "choiceIds": [
            "d4e5f6g7-h8i9-0123-defg-hi4567890123",
            "e5f6g7h8-i9j0-1234-efgh-ij5678901234"
          ]
        }
      ]
    }
  ],
  "couponCode": "SUMMER15",
  "tipAmount": 5.00,
  "includeCouponSuggestions": true
}
```

### Request Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `restaurantId` | `UUID` | Yes | Restaurant identifier |
| `items` | `PricingPreviewItem[]` | Yes | Array of menu items for pricing calculation |
| `couponCode` | `string` | No | Coupon code to apply (max 50 characters) |
| `tipAmount` | `decimal` | No | Tip amount (≥0, default: 0) |
| `includeCouponSuggestions` | `bool` | No | When `true`, response includes the same `couponSuggestions` object used by TeamCart (cart summary, `bestDeal`, `suggestions[]`). Adds a few milliseconds and bypasses cache. |

### Pricing Preview Item Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `menuItemId` | `UUID` | Yes | Menu item identifier |
| `quantity` | `integer` | Yes | Item quantity (1-99) |
| `customizations` | `PricingPreviewCustomization[]` | No | Array of selected customizations |

### Pricing Preview Customization Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `customizationGroupId` | `UUID` | Yes | Customization group identifier |
| `choiceIds` | `UUID[]` | Yes | Array of selected choice identifiers (min 1) |

### Response

**✅ 200 OK**

```json
{
  "subtotal": {
    "amount": 31.98,
    "currency": "USD"
  },
  "discountAmount": {
    "amount": 4.80,
    "currency": "USD"
  },
  "deliveryFee": {
    "amount": 2.99,
    "currency": "USD"
  },
  "tipAmount": {
    "amount": 5.00,
    "currency": "USD"
  },
  "taxAmount": {
    "amount": 2.81,
    "currency": "USD"
  },
  "totalAmount": {
    "amount": 37.98,
    "currency": "USD"
  },
  "currency": "USD",
  "notes": [
    {
      "type": "info",
      "code": "COUPON_APPLIED",
      "message": "Coupon 'SUMMER15' applied successfully",
      "metadata": {
        "discountPercentage": 15.0
      }
    }
  ],
  "calculatedAt": "2023-10-27T14:35:00Z",
  "couponSuggestions": {
    "cartSummary": {
      "subtotal": 31.98,
      "currency": "USD",
      "itemCount": 3
    },
    "bestDeal": {
      "code": "FIRSTORDER",
      "label": "15% off",
      "savings": 4.8,
      "isEligible": true,
      "eligibilityReason": null,
      "minOrderGap": 0,
      "expiresOn": "2023-11-01T00:00:00Z",
      "scope": "WholeOrder",
      "urgency": "ExpiresWithin7Days"
    },
    "suggestions": [
      {
        "code": "FIRSTORDER",
        "label": "15% off",
        "savings": 4.8,
        "isEligible": true,
        "eligibilityReason": null,
        "minOrderGap": 0,
        "expiresOn": "2023-11-01T00:00:00Z",
        "scope": "WholeOrder",
        "urgency": "ExpiresWithin7Days"
      },
      {
        "code": "FREEDESSERT",
        "label": "Free dessert",
        "savings": 0,
        "isEligible": false,
        "eligibilityReason": "MinAmountNotMet",
        "minOrderGap": 8.02,
        "expiresOn": "2023-11-10T00:00:00Z",
        "scope": "SpecificItems",
        "urgency": "None"
      }
    ]
  }
}
```

### Response Schema

| Field | Type | Description |
|-------|------|-------------|
| `subtotal` | `Money` | Sum of all item prices including customizations |
| `discountAmount` | `Money?` | Applied discount amount (null if no coupon) |
| `deliveryFee` | `Money` | Delivery fee for the restaurant |
| `tipAmount` | `Money` | Tip amount (always present, 0 if not specified) |
| `taxAmount` | `Money` | Calculated tax amount |
| `totalAmount` | `Money` | Final total amount |
| `currency` | `string` | Currency code for all amounts |
| `notes` | `PricingPreviewNote[]` | Array of informational, warning, or error notes |
| `calculatedAt` | `string` | ISO 8601 timestamp of calculation |
| `couponSuggestions` | `CouponSuggestionsResponse?` | Present when `includeCouponSuggestions=true`. Matches the TeamCart coupon suggestions shape (`cartSummary`, `bestDeal`, `suggestions`). |

### Pricing Preview Note Schema

| Field | Type | Description |
|-------|------|-------------|
| `type` | `string` | Note type: `"info"`, `"warning"`, or `"error"` |
| `code` | `string` | Machine-readable note code |
| `message` | `string` | Human-readable note message |
| `metadata` | `object?` | Additional context data (optional) |

### Common Note Codes

| Code | Type | Description |
|------|------|-------------|
| `COUPON_APPLIED` | `info` | Coupon was successfully applied |
| `COUPON_NOT_FOUND` | `warning` | Coupon code was not found |
| `COUPON_INVALID` | `warning` | Coupon is expired, inactive, or doesn't meet requirements |
| `MENU_ITEM_NOT_FOUND` | `error` | Menu item does not exist |
| `MENU_ITEM_UNAVAILABLE` | `warning` | Menu item is currently unavailable |
| `CUSTOMIZATION_INVALID` | `error` | Invalid customization selection |
| `COUPON_SUGGESTIONS_UNAVAILABLE` | `warning` | Coupon suggestions could not be fetched or user not signed in |

### Error Responses

**❌ 400 Bad Request** - Validation Error
```json
{
  "status": 400,
  "title": "Validation Error",
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "errors": {
    "Items": ["At least one item is required"],
    "Items[0].Quantity": ["Quantity must be between 1 and 99"],
    "TipAmount": ["Tip amount cannot be negative"]
  }
}
```

**❌ 401 Unauthorized** - Authentication Required
```json
{
  "status": 401,
  "title": "Unauthorized",
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1"
}
```

**❌ 404 Not Found** - Restaurant Not Found
```json
{
  "status": 404,
  "title": "Restaurant not found or inactive",
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "detail": "The specified restaurant does not exist or is not currently active"
}
```

**❌ 422 Unprocessable Entity** - No Valid Items
```json
{
  "status": 422,
  "title": "No valid items found for pricing calculation",
  "type": "https://tools.ietf.org/html/rfc4918#section-11.2",
  "detail": "All provided menu items are invalid, unavailable, or have invalid customizations"
}
```

---

## Business Rules & Pricing Logic

### Pricing Calculation Rules

1. **Subtotal Calculation**:
   - Base item price × quantity
   - Plus customization costs × quantity
   - Sum of all items

2. **Discount Application**:
   - Coupon validation against restaurant and menu items
   - Minimum order amount requirements
   - Percentage or fixed amount discounts
   - Applied to subtotal before taxes

3. **Tax Calculation**:
   - Based on restaurant-specific tax policy
   - Tax base includes subtotal, delivery fee, and tip (configurable)
   - Tax rate applied to tax base

4. **Delivery Fee**:
   - Restaurant-specific delivery fee
   - Applied consistently across all orders

5. **Final Total**:
   - Subtotal - Discount + Delivery Fee + Tip + Tax

### Validation Rules

1. **Restaurant Validation**:
   - Restaurant must exist and be active
   - Restaurant must be accepting orders

2. **Menu Item Validation**:
   - All items must exist and be available
   - All items must belong to the specified restaurant
   - Quantities must be between 1 and 99

3. **Customization Validation**:
   - Customization groups must be applicable to the menu item
   - Choice IDs must be valid for the customization group
   - Minimum and maximum selection requirements enforced

4. **Coupon Validation**:
   - Coupon must exist and be active
   - Coupon must be valid for the restaurant
   - Minimum order amount requirements
   - Usage limits and expiration dates

### Performance Considerations

1. **Caching Strategy**:
   - Responses cached for 2 minutes
   - Cache key includes restaurant ID and item hash
   - Cache invalidation on menu changes

2. **Response Time**:
   - Target response time: <500ms
   - Optimized database queries
   - Efficient pricing calculations

3. **Rate Limiting**:
   - Standard rate limiting applies
   - No special restrictions for pricing preview

---

## Integration Examples

### Basic Pricing Preview

```javascript
// Get pricing for a simple order
const response = await fetch('/api/v1/pricing/preview', {
  method: 'POST',
  headers: {
    'Authorization': 'Bearer ' + token,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    restaurantId: 'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
    items: [
      {
        menuItemId: 'b2c3d4e5-f6g7-8901-bcde-fg2345678901',
        quantity: 2
      }
    ],
    tipAmount: 3.00
  })
});

const pricing = await response.json();
console.log(`Total: $${pricing.totalAmount.amount}`);
```

### Pricing with Customizations and Coupon

```javascript
// Get pricing with customizations and coupon
const response = await fetch('/api/v1/pricing/preview', {
  method: 'POST',
  headers: {
    'Authorization': 'Bearer ' + token,
    'Content-Type': 'application/json'
  },
  body: JSON.stringify({
    restaurantId: 'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
    items: [
      {
        menuItemId: 'b2c3d4e5-f6g7-8901-bcde-fg2345678901',
        quantity: 1,
        customizations: [
          {
            customizationGroupId: 'c3d4e5f6-g7h8-9012-cdef-gh3456789012',
            choiceIds: ['d4e5f6g7-h8i9-0123-defg-hi4567890123']
          }
        ]
      }
    ],
    couponCode: 'SAVE10',
    tipAmount: 5.00
  })
});

const pricing = await response.json();

// Check for notes
pricing.notes.forEach(note => {
  if (note.type === 'warning') {
    console.warn(note.message);
  }
});
```

### Error Handling

```javascript
try {
  const response = await fetch('/api/v1/pricing/preview', {
    method: 'POST',
    headers: {
      'Authorization': 'Bearer ' + token,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(requestBody)
  });

  if (!response.ok) {
    const error = await response.json();
    
    if (response.status === 400) {
      // Handle validation errors
      console.error('Validation errors:', error.errors);
    } else if (response.status === 404) {
      // Handle restaurant not found
      console.error('Restaurant not found');
    } else if (response.status === 422) {
      // Handle no valid items
      console.error('No valid items for pricing');
    }
    return;
  }

  const pricing = await response.json();
  // Process successful response
  
} catch (error) {
  console.error('Network error:', error);
}
```

---

## Cross-References

- Authentication: `./01-Authentication-and-Profile.md`
- Order Placement: `./03-Individual-Orders.md`
- Core Concepts: `../../03-Core-Concepts.md`
- Error Codes: `../../Appendices/01-Error-Codes.md`
