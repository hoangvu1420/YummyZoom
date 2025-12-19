# Order Simulator (Dev/Test Only)

Dev helper to generate synthetic restaurant orders for the Live Orders dashboard and optionally auto-drive them through the restaurant flow.

## Prerequisites
- Environment: `Development` or `Test`.
- Feature flag: `Features:OrderFlowSimulation=true` (see `src/Web/appsettings.Development.json` for reference).
- Seed data: at least one active `DomainUsers` row, one restaurant, and available menu items for that restaurant.
- No authentication on the dev endpoint; guarded by env + feature flag.

## Endpoint
`POST /dev/order-simulator/start`

### Request body
```json
{
  "restaurantId": "7b87f9f1-...",      // optional; if null picks first restaurant
  "count": 5,                          // 1..50
  "scenario": "incomingOnly",          // incomingOnly | autoFlow | fastAutoFlow
  "interOrderDelayMs": 5000,           // delay between creating each order; default 5000ms
  "delaysMs": {                        // optional, only used when autoFlow/fastAutoFlow
    "placedToAcceptedMs": 5000,
    "acceptedToPreparingMs": 10000,
    "preparingToReadyMs": 10000,
    "readyToDeliveredMs": 15000
  },
  "notePool": ["Giao trước cổng", "Ít cay"], // optional order-level notes
  "useRandomMenuItems": true,
  "maxItemsPerOrder": 3,               // 1..5
  "maxQuantityPerItem": 2              // 1..5
}
```

### Scenarios
- `incomingOnly`: create orders and leave them at `Placed` (for “Đơn mới” lane).
- `autoFlow`: create orders then auto-run Accept → Preparing → Ready → Delivered with provided delays (defaults ~5–45s).
- `fastAutoFlow`: same as autoFlow but very short defaults (~1s each).

### Response 202
```json
{
  "restaurantId": "7b87f9f1-...",
  "orders": [
    { "orderId": "f47ac10b-...", "scenario": "autoflow", "status": "Placed", "flowStatus": "Starting flow" }
  ]
}
```

### Example

```json
{
  "restaurantId": "7b87f9f1-...",
  "count": 5,
  "scenario": "incomingOnly",
  "interOrderDelayMs": 5000,
  "delaysMs": {
    "placedToAcceptedMs": 5000,
    "acceptedToPreparingMs": 10000,
    "preparingToReadyMs": 10000,
    "readyToDeliveredMs": 15000
  },
  "notePool": ["Giao trước cổng", "Ít cay"],
  "useRandomMenuItems": true,
  "maxItemsPerOrder": 3,
  "maxQuantityPerItem": 2
}
```

## Notes & Limitations
- Payment is always `CashOnDelivery`; no external gateway.
- No driver/shipper fields (not supported in system).
- Per-item notes are not supported; use customizations to mimic “không hành/ít cay”.
- Menu items are selected from available items for the restaurant (up to 50).
- `interOrderDelayMs` defaults to 5000ms to spread out order creation; set to 0 to create orders back-to-back.
- Concurrency: orders are created sequentially in one request; flow automation runs in background.

## Quick cURL example (Dev)
```bash
curl -X POST http://localhost:5155/dev/order-simulator/start \
  -H "Content-Type: application/json" \
  -d '{
    "count": 3,
    "scenario": "autoFlow",
    "delaysMs": { "placedToAcceptedMs": 500, "acceptedToPreparingMs": 800, "preparingToReadyMs": 800, "readyToDeliveredMs": 800 }
  }'
```

Monitor Live Orders UI or SignalR logs to see status transitions.***
