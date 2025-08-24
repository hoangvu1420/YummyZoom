## Broadcast Matrix

Phase 1 (Now):
1. OrderPaymentSucceeded
   - Payload: OrderId, RestaurantId, OrderNumber, Status=Placed (or Paid), Total/GrossAmount, PlacementTimestamp, LastUpdateTimestamp, EstimatedDeliveryTime (if any)
   - Channel: RestaurantOrdersHub -> group: restaurant:{RestaurantId}
   - Recipients: Authorized restaurant staff (kitchen, manager dashboard)
2. OrderPaymentFailed
   - Payload: OrderId, RestaurantId, Status=PaymentFailed, FailureTimestamp
   - Channel: (Same hub) restaurant:{RestaurantId}
   - Recipients: Restaurant staff (so they don’t act on it); optional future customer notification
3. OrderAccepted / OrderPreparing / OrderReadyForDelivery / OrderDelivered / OrderRejected / OrderCancelled
   - Payload (uniform OrderStatusBroadcastDto): OrderId, RestaurantId, OrderNumber, NewStatus, LastUpdateTimestamp, EstimatedDeliveryTime, ActualDeliveryTime (when Delivered), OccurredOnUtc
   - Channel: RestaurantOrdersHub -> restaurant:{RestaurantId}
   - Recipients: Restaurant dashboard (real-time workflow board)

(If customer-facing hub deferred, these events are still available for polling fallback.)

Deferred / Future (Planned):
1. Customer Order Status Updates
   - Events reused from above
   - Channel: CustomerOrderStatusHub -> order:{OrderId} (or customer:{CustomerId} if batching)
   - Recipients: The ordering customer (mobile/web client)
2. CouponUsed (if UI needs live coupon availability)
   - Payload: CouponId, PreviousUsageCount, NewUsageCount, UsedAt
   - Channel: Marketing/Admin hub -> coupon:{CouponId} or admin global group
   - Recipients: Admin dashboards / promotional tools
3. RevenueRecorded / PlatformFeeRecorded
   - Payload: RestaurantAccountId, RestaurantId, OrderId, Amount, NewBalance
   - Channel: Finance hub -> restaurant:{RestaurantId}
   - Recipients: Finance dashboard / accounting microservice
4. Integration / Analytics Events
   - Emitted to message bus (e.g., Kafka/Rabbit) via separate Outbox → ExternalPublisher
   - Consumers: Data warehouse, analytics, notification service
5. Order SLA / Delay Alerts
   - Derived events (not domain) triggered by timers
   - Channels: RestaurantOrdersHub (alerts), CustomerOrderStatusHub (delay notice)

## How They Will Be Broadcast (Mechanism)

Layered approach:
1. Application event handlers invoke `IOrderRealtimeNotifier` (abstraction).
2. Infrastructure implementation uses SignalR:
   - Hubs:
     - RestaurantOrdersHub (Phase 1)
     - CustomerOrderStatusHub (future)
   - Grouping strategy:
     - restaurant:{RestaurantId}
     - order:{OrderId} (customer tracking)
3. Authorization:
   - On hub connection: validate JWT; add to groups permitted (staff with RoleAssignment; customer owns OrderId).
4. Payload contracts:
   - Compact DTOs, versionable (add `SchemaVersion` if needed later).
5. Failure handling:
   - Broadcast is best-effort; swallow/log failures so revenue booking & other side-effects proceed.

## Recipient Summary

| Event Type | Primary Recipient Now | Future Additional Recipients |
|------------|-----------------------|------------------------------|
| PaymentSucceeded | Restaurant staff | Customer, Finance |
| PaymentFailed | Restaurant staff | Customer |
| Status Changes | Restaurant staff | Customer |
| CouponUsed | (Deferred) | Admin/Marketing |
| RevenueRecorded | (Deferred) | Finance dashboards |
| PlatformFeeRecorded | (Deferred) | Finance/Platform analytics |

## Prioritization (Phase 1 MVP)

Implement only:
- PaymentSucceeded
- PaymentFailed
- Accepted / Preparing / ReadyForDelivery / Delivered / Rejected / Cancelled

Everything else explicitly deferred with extension points left in `IOrderRealtimeNotifier`.

## Key Considerations

- Uniform DTO simplifies hub client subscription.
- Keep payload minimal to avoid leaking sensitive info (no customer PII in restaurant broadcast).
- Add OccurredOnUtc for ordering and reconciliation.
- Idempotent handlers ensure single broadcast per event even with retries.

That covers what, how, and who for broadcasts now and later.