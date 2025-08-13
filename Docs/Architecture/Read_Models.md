# Identity & Access

| Read Model / Lookup Table   | Description                                                                                               | Triggering Domain Events                                              | Purpose & Use Case                                                                       |
| --------------------------- | --------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------- | ---------------------------------------------------------------------------------------- |
| **`RoleAssignmentsView`**   | Flattened view of a user’s roles across restaurants: `(UserID, RestaurantID, Role)` plus restaurant name. | `RoleAssignmentCreated`, `RoleAssignmentRemoved`, `RestaurantUpdated` | Permission checks on every API call without joins; render “Your Restaurants” dashboards. |
| **`RestaurantStaffRoster`** | Per-restaurant roster of owners/staff with minimal contact info.                                          | same as above                                                         | Show/manage staff lists in owner UI; drive notification fan-out.                         |

# Restaurant Catalog & Ops

| Read Model / Lookup Table           | Description                                                                                                                 | Triggering Domain Events                                                                                          | Purpose & Use Case                                                                      |
| ----------------------------------- | --------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------- |
| **`RestaurantOperationalSnapshot`** | Per restaurant: `IsAcceptingOrders`, `OpenNow`, `NextOpenAt`, `TodayBusinessHours` (denorm).                                | `RestaurantUpdated`, `BusinessHoursUpdated`, `IsAcceptingOrdersToggled` (+ periodic recompute cron for “OpenNow”) | Ultra-fast filter “only show places open now / taking orders”.                          |
| **`MenuItemAvailabilityIndex`**     | `(MenuItemID, RestaurantID, IsAvailable, BasePrice, Name, MenuCategoryName)`                                                | `MenuItemCreated/Updated`, `MenuItemAvailabilityChanged`, `MenuCategoryUpdated`                                   | Power search & category pages that need stock/price without pulling the full menu JSON. |
| **`CustomizationOptionLookup`**     | For each `MenuItemID`, a flattened list of active customization groups/choices with price adjustments and selection bounds. | `CustomizationGroupUpdated`, `MenuItemUpdated`                                                                    | Render item detail modals quickly; validate client selections cheaply.                  |
| **`MenuItemSearchIndex`**           | Search doc for dishes: item name, synonyms, tags, cuisine, restaurant, popularity signals.                                  | `MenuItemCreated/Updated`, `TagUpdated`, `RestaurantUpdated`, `OrderPlaced` (popularity)                          | Dish-level search (“pad thai near me”, “gluten-free pizza”).                            |
| **`TagIndex`**                      | Reverse index: `TagID/TagName → [MenuItemIDs]` + counts.                                                                    | `TagUpdated`, `MenuItemUpdated`                                                                                   | One-click dietary filters with counts (faceting).                                       |

# Coupons & Pricing

| Read Model / Lookup Table       | Description                                                                                   | Triggering Domain Events                                                    | Purpose & Use Case                                                    |
| ------------------------------- | --------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------- | --------------------------------------------------------------------- |
| **`ActiveCouponsByRestaurant`** | Active promos with basic applicability: code, type, value, scope, min order, validity window. | `CouponCreated/Updated/Disabled`                                            | Show promo banners and code pickers without touching the write model. |
| **`ItemCouponMatrix`**          | Precomputed applicability: `(CouponID → [MenuItemID/CategoryID])`.                            | same as above, `MenuItemUpdated`, `MenuCategoryUpdated`                     | Rapid coupon validation and “what does this coupon apply to?” UX.     |
| **`UserCouponEligibility`**     | `(UserID, CouponID, IsEligible, RemainingUses)` merged from global state + per-user usage.    | `CouponCreated/Updated`, `OrderPlacedWithCoupon`, `CouponUsageLimitChanged` | Instant feedback in checkout (“you’re not eligible / 1 use left”).    |

# Cart & TeamCart

| Read Model / Lookup Table     | Description                                                                 | Triggering Domain Events                                | Purpose & Use Case                                             |
| ----------------------------- | --------------------------------------------------------------------------- | ------------------------------------------------------- | -------------------------------------------------------------- |
| **`TeamCartSummary`**         | `(TeamCartID, Restaurant, HostUser, Status, Deadline, Totals, MemberCount)` | `TeamCartCreated/Updated`, `TeamCartExpired`            | List/landing for team carts; decide which actions are enabled. |
| **`TeamCartItemView`**        | Denorm items with snapshot names/prices/customizations per cart.            | `TeamCartItemAdded/Updated/Removed`                     | Render cart contents without touching aggregates.              |
| **`TeamCartInviteLookup`**    | `ShareToken → TeamCartID + minimal cart meta`                               | `TeamCartCreated`, `ShareTokenRotated`                  | Resolve invite links quickly and safely.                       |
| **`MemberPaymentStatusView`** | Per member: pledged/paid amounts, method, status, timestamps.               | `MemberPaymentCommitted/Failed/Paid`, `TeamCartUpdated` | Gate transitions to `ReadyToConfirm`; show who still owes.     |

# Orders & Fulfillment

| Read Model / Lookup Table         | Description                                                                                    | Triggering Domain Events                                     | Purpose & Use Case                                             |
| --------------------------------- | ---------------------------------------------------------------------------------------------- | ------------------------------------------------------------ | -------------------------------------------------------------- |
| **`ActiveOrdersByRestaurant`**    | KDS-friendly view grouped by status with item lines & notes.                                   | `OrderPlaced`, `OrderStatusChanged`, `OrderItemNotesUpdated` | Kitchen screens / owner tablet—zero joins, ordered by SLA.     |
| **`OrderStatusTimeline`**         | Append-only per Order: `(Status, Timestamp, Actor)`                                            | status changes, payment events                               | Power the customer tracking UI and support audits.             |
| **`CustomerOpenOrders`**          | Current orders for a user with compact fields; excludes history.                               | `OrderPlaced`, `OrderStatusChanged`                          | Show “Track your order” tile on home/profile in O(1).          |
| **`CustomerOrderHistorySummary`** | Denorm list for a user: `(OrderID, Restaurant, ItemsPreview, Total, PlacedAt, ReorderPayload)` | `OrderPlaced`, `OrderCompleted/Cancelled`                    | Fast history & one-click re-order.                             |
| **`OrderPaymentSummary`**         | Per order: payment intent/reference, captured/refunded amounts, last failure reason.           | `PaymentRecorded`, `RefundIssued`, `PaymentFailed`           | Robust payment state for CS/admin UIs without hitting gateway. |
| **`ReviewEligibility`**           | Orders delivered & not yet reviewed; `(UserID, OrderID, EligibleFrom)`                         | `OrderDelivered`, `ReviewSubmitted`                          | Nudge users to review; disable review UI when not eligible.    |

# Payments, Revenue & Payouts

| Read Model / Lookup Table              | Description                                                                                              | Triggering Domain Events                                     | Purpose & Use Case                                           |
| -------------------------------------- | -------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| **`RestaurantAccountBalanceSnapshot`** | `(RestaurantID, CurrentBalance, LastUpdatedAt)`                                                          | `RevenueRecorded`, `PayoutSettled`, `ManualAdjustmentPosted` | Owner dashboards and payout availability checks.             |
| **`AccountTransactionFeed`**           | Time-ordered immutable feed (already your EN) but projected with human labels and related order numbers. | `AccountTransactionCreated`                                  | Admin & owner statements without expanding events each time. |
| **`RestaurantDailyRevenueSummary`**    | `(RestaurantID, Date, Gross, Discounts, Fees, Net)`                                                      | `OrderCompleted`, `AccountTransactionCreated`                | Charts, trendlines, tax exports.                             |
| **`PayoutQueue`**                      | Pending payouts with amount, destination summary, earliest execution time.                               | `PayoutRequested`, `PayoutSettled`, `PayoutFailed`           | Operate payouts and show ETA/status to owners.               |

# Reviews & Reputation

| Read Model / Lookup Table       | Description                                        | Triggering Domain Events                                         | Purpose & Use Case                              |
| ------------------------------- | -------------------------------------------------- | ---------------------------------------------------------------- | ----------------------------------------------- |
| **`RecentReviewsByRestaurant`** | Last N reviews with stars and comment snippet.     | `ReviewSubmitted`, `ReviewModerated/Hidden`                      | Restaurant profile UI.                          |
| **`ReviewModerationQueue`**     | Reviews needing attention: flags, risk score, age. | `ReviewSubmitted`, `ReviewFlagged`, `ReviewAutoModerationTagged` | Support workflow to approve/hide/reply quickly. |

# Support & Governance

| Read Model / Lookup Table     | Description                                                        | Triggering Domain Events                                            | Purpose & Use Case                                            |
| ----------------------------- | ------------------------------------------------------------------ | ------------------------------------------------------------------- | ------------------------------------------------------------- |
| **`SupportTicketThreadView`** | Flattened messages per ticket with author display names and roles. | `SupportTicketCreated`, `TicketMessageAdded`, `TicketStatusChanged` | Single query to render the ticket thread.                     |
| **`TicketContextIndex`**      | Reverse links: `(EntityType, EntityID) → [TicketIDs]`              | same as above                                                       | From any order/user/restaurant page, jump to related tickets. |
| **`OpenTicketsByQueue`**      | Buckets by type/priority/assignee with counts & SLAs.              | same                                                                | Team workload boards and alerts.                              |

# Discovery & Personalization

| Read Model / Lookup Table  | Description                                                                    | Triggering Domain Events                                   | Purpose & Use Case                           |
| -------------------------- | ------------------------------------------------------------------------------ | ---------------------------------------------------------- | -------------------------------------------- |
| **`RestaurantSLAMetrics`** | Rolling medians for prep time & acceptance latency per restaurant/time-of-day. | `OrderAccepted`, `OrderReadyForDelivery`, `OrderDelivered` | Better ETA predictions and ranking.          |
| **`MenuItemPopularity`**   | Rolling counts per item/category/restaurant (1d/7d/30d).                       | `OrderPlaced`                                              | “Popular near you” and sort by best-sellers. |

---

## Notes on implementation details

* **Shape**: Most of these can be narrow relational tables. For complex UI payloads (e.g., `ActiveOrdersByRestaurant`, `TeamCartItemView`) use a **document column** (JSON) to store stable, denormalized line items/customizations.
* **Projections**: Build them with idempotent event handlers. For time-based snapshots like `RestaurantOperationalSnapshot`, supplement event triggers with a **small scheduler** (e.g., recompute `OpenNow` every 5 minutes).
* **Invalidation**: When write-side invariants touch many items (e.g., coupon scope changes), fan out “rebuild” jobs for the affected restaurant to refresh `ItemCouponMatrix` and `FullMenuView` atomically.
* **Security**: Keep PII out of search indices. Use `RoleAssignmentsView` to guard reads for owner/staff dashboards.

If you want, I can turn these into a seed SQL schema (with indexes) or draw a small projection topology diagram showing which events feed which read models.
