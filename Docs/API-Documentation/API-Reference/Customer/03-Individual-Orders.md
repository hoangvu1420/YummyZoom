# Individual Orders

This guide covers all APIs for placing, tracking, and managing individual orders. Individual orders are direct customer orders placed immediately without cart collaboration.

## Overview

Individual orders in YummyZoom support the complete order lifecycle:

- **Order Placement**: Create orders with menu items, customizations, and payment methods
- **Payment Processing**: Handle online payments via Stripe or cash-on-delivery options
- **Order Tracking**: Real-time status updates from placement to delivery
- **Order History**: Access past orders and detailed receipts
- **Financial Breakdown**: Transparent pricing with subtotal, taxes, fees, and discounts

All order endpoints require **authentication** except where noted.

---

## Order Placement

### Initiate Order

Creates a new individual order with specified items, delivery details, and payment method. For online payments, returns payment credentials for client-side completion.

**`POST /api/v1/orders/initiate`**

- **Authorization:** Required (Customer)
- **Idempotency:** Supported via `Idempotency-Key` header

#### Request Headers

| Header | Type | Required | Description |
|--------|------|----------|-------------|
| `Authorization` | `string` | Yes | Bearer token for customer authentication |
| `Idempotency-Key` | `UUID` | No | Unique identifier to prevent duplicate order creation. Must be a valid UUID v4 format. |

#### Request Body

```json
{
  "customerId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "restaurantId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "items": [
    {
      "menuItemId": "b2c3d4e5-f6g7-8901-bcde-fg2345678901",
      "quantity": 2,
      "customizations": [
        {
          "customizationGroupId": "c3d4e5f6-g7h8-9012-cdef-gh3456789012",
          "choiceIds": [
            "d4e5f6g7-h8i9-0123-defg-hi4567890123"
          ]
        }
      ]
    },
    {
      "menuItemId": "e5f6g7h8-i9j0-1234-efgh-ij5678901234",
      "quantity": 1
    }
  ],
  "deliveryAddress": {
    "street": "123 Market Street, Apt 4B",
    "city": "San Francisco",
    "state": "CA",
    "zipCode": "94105",
    "country": "US"
  },
  "paymentMethod": "CreditCard",
  "specialInstructions": "Please ring doorbell twice",
  "couponCode": "WELCOME10",
  "tipAmount": 5.00,
  "teamCartId": null
}
```

#### Request Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `customerId` | `UUID` | Yes | Customer placing the order (must match authenticated user) |
| `restaurantId` | `UUID` | Yes | Target restaurant for the order |
| `items` | `array` | Yes | Array of menu items with quantities and customizations |
| `deliveryAddress` | `object` | Yes | Complete delivery address information |
| `paymentMethod` | `string` | Yes | Payment method: "CreditCard", "PayPal", "ApplePay", "GooglePay", "CashOnDelivery" |
| `specialInstructions` | `string` | No | Additional instructions for the restaurant |
| `couponCode` | `string` | No | Discount coupon code to apply |
| `tipAmount` | `number` | No | Tip amount in restaurant's currency |
| `teamCartId` | `UUID` | No | Source team cart ID (for converted team orders) |

#### Order Item Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `menuItemId` | `UUID` | Yes | Menu item identifier |
| `quantity` | `number` | Yes | Quantity to order (must be positive) |
| `customizations` | `array` | No | Array of customization selections |

#### Customization Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `customizationGroupId` | `UUID` | Yes | Customization group identifier |
| `choiceIds` | `UUID[]` | Yes | Array of selected choice IDs within the group |

#### Delivery Address Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `street` | `string` | Yes | Street address including apartment/unit number |
| `city` | `string` | Yes | City name |
| `state` | `string` | Yes | State or province |
| `zipCode` | `string` | Yes | Postal/ZIP code |
| `country` | `string` | Yes | Country code (ISO 3166-1 alpha-2) |

#### Response

**✅ 201 Created**
```json
{
  "orderId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "orderNumber": "ORD-2023-001234",
  "totalAmount": {
    "amount": 34.56,
    "currency": "USD"
  },
  "paymentIntentId": "pi_3N8mX2LkdIwHu7ix0G1XYZ3A",
  "clientSecret": "pi_3N8mX2LkdIwHu7ix0G1XYZ3A_secret_abcdef123456"
}
```

For **Cash on Delivery** orders:
```json
{
  "orderId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "orderNumber": "ORD-2023-001234",
  "totalAmount": {
    "amount": 34.56,
    "currency": "USD"
  },
  "paymentIntentId": null,
  "clientSecret": null
}
```

#### Response Schema

| Field | Type | Description |
|-------|------|-------------|
| `orderId` | `UUID` | Unique order identifier |
| `orderNumber` | `string` | Human-readable order number |
| `totalAmount` | `Money` | Final total including all fees and taxes |
| `paymentIntentId` | `string\|null` | Stripe payment intent ID (null for COD) |
| `clientSecret` | `string\|null` | Client secret for payment completion (null for COD) |

---

## Order Tracking

### Get Order Details

Retrieves complete order information including items, pricing breakdown, and current status.

**`GET /api/v1/orders/{orderId}`**

- **Authorization:** Required (Order Customer or Restaurant Staff)

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `orderId` | `UUID` | Yes | Unique order identifier |

#### Response

**✅ 200 OK**
```json
{
  "orderDetails": {
    "orderId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
    "orderNumber": "ORD-2023-001234",
    "customerId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "restaurantId": "b2c3d4e5-f6g7-8901-bcde-fg2345678901",
    "status": "Preparing",
    "placementTimestamp": "2023-10-27T14:30:00Z",
    "lastUpdateTimestamp": "2023-10-27T14:45:00Z",
    "estimatedDeliveryTime": "2023-10-27T15:30:00Z",
    "actualDeliveryTime": null,
    "subtotalAmount": 26.98,
    "subtotalCurrency": "USD",
    "discountAmount": 2.70,
    "discountCurrency": "USD",
    "deliveryFeeAmount": 2.99,
    "deliveryFeeCurrency": "USD",
    "tipAmount": 5.00,
    "tipCurrency": "USD",
    "taxAmount": 2.16,
    "taxCurrency": "USD",
    "totalAmount": 34.43,
    "totalCurrency": "USD",
    "appliedCouponId": "c3d4e5f6-g7h8-9012-cdef-gh3456789012",
    "sourceTeamCartId": null,
    "deliveryAddress_Street": "123 Market Street, Apt 4B",
    "deliveryAddress_City": "San Francisco",
    "deliveryAddress_State": "CA",
    "deliveryAddress_Country": "US",
    "deliveryAddress_PostalCode": "94105",
    "items": [
      {
        "orderItemId": "d4e5f6g7-h8i9-0123-defg-hi4567890123",
        "menuItemId": "b2c3d4e5-f6g7-8901-bcde-fg2345678901",
        "name": "Classic Burger",
        "quantity": 2,
        "unitPriceAmount": 12.99,
        "unitPriceCurrency": "USD",
        "lineItemTotalAmount": 25.98,
        "lineItemTotalCurrency": "USD",
        "customizations": [
          {
            "groupName": "Cheese",
            "choiceName": "Swiss",
            "priceAdjustmentAmount": 1.00,
            "priceAdjustmentCurrency": "USD"
          }
        ]
      }
    ]
  }
}
```

---

### Get Order Status

Retrieves lightweight order status information optimized for polling and real-time updates.

**`GET /api/v1/orders/{orderId}/status`**

- **Authorization:** Required (Order Customer or Restaurant Staff)

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `orderId` | `UUID` | Yes | Unique order identifier |

#### Response

**✅ 200 OK**
```json
{
  "orderId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "status": "Preparing",
  "lastUpdateTimestamp": "2023-10-27T14:45:00Z",
  "estimatedDeliveryTime": "2023-10-27T15:30:00Z"
}
```

#### Order Status Values

| Status | Description |
|--------|-------------|
| `AwaitingPayment` | Order created, waiting for payment confirmation |
| `Placed` | Payment confirmed, order sent to restaurant |
| `Accepted` | Restaurant accepted the order |
| `Preparing` | Restaurant is preparing the order |
| `ReadyForDelivery` | Order is ready for pickup/delivery |
| `Delivered` | Order has been delivered to customer |
| `Cancelled` | Order was cancelled |
| `Rejected` | Restaurant rejected the order |

---

### Get My Orders

Retrieves paginated list of orders for the authenticated customer.

**`GET /api/v1/orders/my`**

- **Authorization:** Required (Customer)

#### Query Parameters

| Parameter | Type | Required | Description | Default |
|-----------|------|----------|-------------|---------|
| `pageNumber` | `number` | No | Page number for pagination | `1` |
| `pageSize` | `number` | No | Number of orders per page | `10` |

#### Response

**✅ 200 OK**
```json
{
  "items": [
    {
      "orderId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
      "orderNumber": "ORD-2023-001234",
      "status": "Delivered",
      "placementTimestamp": "2023-10-27T14:30:00Z",
      "restaurantId": "b2c3d4e5-f6g7-8901-bcde-fg2345678901",
      "customerId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "totalAmount": 34.43,
      "totalCurrency": "USD",
      "itemCount": 3
    },
    {
      "orderId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "orderNumber": "ORD-2023-001233",
      "status": "Cancelled",
      "placementTimestamp": "2023-10-26T19:15:00Z",
      "restaurantId": "c3d4e5f6-g7h8-9012-cdef-gh3456789012",
      "customerId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "totalAmount": 18.75,
      "totalCurrency": "USD",
      "itemCount": 2
    }
  ],
  "pageNumber": 1,
  "totalPages": 5,
  "totalCount": 47,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

---

## Order Management

### Cancel Order

Cancels an order if it's in a cancellable state. Customers can cancel orders before restaurant acceptance.

**`POST /api/v1/orders/{orderId}/cancel`**

- **Authorization:** Required (Order Customer or Restaurant Staff)

#### Path Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `orderId` | `UUID` | Yes | Unique order identifier |

#### Request Body

```json
{
  "restaurantId": "b2c3d4e5-f6g7-8901-bcde-fg2345678901",
  "actingUserId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "reason": "Customer changed mind"
}
```

#### Request Schema

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `restaurantId` | `UUID` | Yes | Restaurant ID for authorization |
| `actingUserId` | `UUID` | No | ID of user performing cancellation (optional for customer self-cancellation) |
| `reason` | `string` | No | Reason for cancellation |

#### Response

**✅ 200 OK**
```json
{
  "orderId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
  "newStatus": "Cancelled",
  "message": "Order has been successfully cancelled",
  "timestamp": "2023-10-27T14:35:00Z"
}
```

---

## Business Rules & Order Flow

### Order Placement Rules

1. **Customer Authorization**: Only authenticated customers can place orders for themselves
2. **Restaurant Validation**: Restaurant must be active and accepting orders
3. **Menu Item Validation**: 
   - All items must exist and be available
   - All items must belong to the specified restaurant
   - Customizations must be valid for each menu item
4. **Address Validation**: Complete delivery address is required
5. **Financial Calculation**: 
   - Subtotal = sum of (item price + customizations) × quantity
   - Tax = subtotal × 8% (configurable rate)
   - Delivery fee = $2.99 (configurable)
   - Total = subtotal - discount + delivery fee + tip + tax
6. **Idempotency Protection**: 
   - Duplicate requests with the same `Idempotency-Key` return cached response
   - Prevents accidental duplicate orders from network retries or client errors
   - Idempotency keys are cached for 5 minutes after successful order creation

### Payment Processing

#### Cash on Delivery (COD)
- Order status immediately transitions to `Placed`
- Payment transaction is created and marked as `Succeeded`
- No external payment gateway interaction required

#### Online Payments (Credit Card, PayPal, etc.)
- Order status starts as `AwaitingPayment`
- Stripe payment intent created with order metadata
- Customer completes payment on client side
- Webhook confirms payment success/failure
- Status transitions to `Placed` on success or `Cancelled` on failure

### Coupon Application

1. **Validation**: Coupon must exist, be active, and belong to the restaurant
2. **Eligibility**: Minimum order value and applicable items checked
3. **Usage Tracking**: Per-user and total usage limits enforced atomically
4. **Financial Impact**: Discount applied before tax calculation

### Order Status Lifecycle

```
AwaitingPayment (online) ──► Placed ──► Accepted ──► Preparing ──► ReadyForDelivery ──► Delivered
      │                        │           │
      ▼                        ▼           ▼
   Cancelled               Rejected    Cancelled
```

**Status Transition Rules:**
- `AwaitingPayment`: Only for online payments; automatically transitions on payment confirmation
- `Placed`: Restaurant can accept/reject; customer can cancel before acceptance
- `Accepted`: Restaurant can cancel with reason; customer cannot cancel
- `Preparing`: Restaurant controls progression; delivery tracking begins
- `ReadyForDelivery`: Awaiting delivery completion
- `Delivered`: Final success state
- `Cancelled`: Terminal state (payment refunded if applicable)
- `Rejected`: Terminal state (payment refunded automatically)

### Authorization Rules

**Order Access:**
- Customers can only view/manage their own orders
- Restaurant staff can view orders for their restaurant
- Administrators can view all orders

**Order Operations:**
- Customer cancellation: Allowed until restaurant acceptance
- Restaurant operations: Require restaurant staff authorization
- Status updates: Only restaurant staff can progress order status

### Real-time Updates

Orders generate domain events for status changes:
- `OrderPlaced`: Notifies restaurant of new order
- `OrderAccepted`: Notifies customer of acceptance
- `OrderPreparing`: Updates customer on preparation status
- `OrderReadyForDelivery`: Delivery preparation notification
- `OrderDelivered`: Final confirmation to customer
- `OrderCancelled`: Cancellation notification to relevant parties

---

## Error Handling

### Validation Errors

**400 Bad Request**
```json
{
  "type": "validation",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "paymentMethod": ["The specified payment method is not valid."],
    "items": ["At least one item is required."]
  }
}
```

### Business Logic Errors

**422 Unprocessable Entity**
```json
{
  "type": "https://tools.ietf.org/html/rfc4918#section-11.2",
  "title": "Business rule violation",
  "status": 422,
  "detail": "Menu item 'Classic Burger' is currently not available."
}
```

### Authorization Errors

**403 Forbidden**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403,
  "detail": "You are not authorized to access this order."
}
```

### Not Found Errors

**404 Not Found**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Order not found."
}
```

---

## Performance Considerations

### Order Creation Optimization
- Menu items validated in batch queries to minimize database roundtrips
- Customization groups loaded once per unique group across all items
- Financial calculations performed in-memory after data loading
- Payment intent creation happens before order persistence

### Status Polling
- Only fall back to polling if real-time updates are not available
- Use lightweight `/status` endpoint for frequent updates
- ETag caching planned for conditional responses
- Full order details endpoint optimized for detailed views

### Pagination Best Practices
- Default page size of 10 orders balances performance and UX
- Maximum page size of 50 enforced for large datasets
- Use cursor-based pagination for very active customers (future enhancement)

---

## Security Considerations

### Payment Security
- Payment credentials (client secrets) are one-time use
- Order metadata prevents payment intent reuse across orders
- Webhook signature validation ensures payment confirmation authenticity

### Data Protection
- Order details include sensitive delivery addresses
- Customer PII access restricted to order participants
- Payment information stored separately with PCI compliance

### Authorization Model
- JWT-based authentication with claim-based authorization
- Order access validated on every request
- Restaurant staff permissions scoped to specific restaurants

---

## Integration Notes

### Team Cart Conversion
Individual orders can originate from team cart conversions:
- `teamCartId` field links to source team cart
- Payment transactions represent individual member contributions
- Order totals may differ from team cart due to delivery fee redistribution

### Delivery Integration
Future integration planned for:
- Real-time delivery tracking
- Delivery partner assignment
- GPS-based status updates
- Estimated delivery time adjustments

### Notification System
Orders integrate with notification services for:
- Order confirmation emails/SMS
- Status update push notifications
- Restaurant order management alerts
- Delivery coordination messages