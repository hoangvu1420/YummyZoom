**Order Placement + Stripe Integration**

- Scope: Single‑order online payments using Stripe Payment Intents; includes endpoints, request/response shapes, backend flow, webhooks, idempotency, and config.
- API base prefix: `/api/v1` (API versioning is applied at the group level).

**Key Endpoints**

- `POST /api/v1/orders/initiate`
  - Auth required. Accepts order details and a `PaymentMethod`. For online payment, set `PaymentMethod` to `CreditCard` (anything other than `CashOnDelivery` triggers Stripe path).
  - Optional header: `Idempotency-Key: <opaque-guid-or-token>` to prevent duplicate order/payment creation.
  - Returns: order identifiers, totals, and for online payments a Stripe `paymentIntentId` and `clientSecret` to confirm on the client.
  - Code: `src/Web/Endpoints/Orders.cs:26`, `src/Application/Orders/Commands/InitiateOrder/InitiateOrderCommandHandler.cs:285`.

- `GET /api/v1/orders/{orderId}/status`
  - Auth required. Returns current order status and version. Supports `ETag`/`If-None-Match` for efficient polling. Returns `304 Not Modified` if unchanged.
  - Code: `src/Web/Endpoints/Orders.cs:136`.

- `POST /api/v1/stripe-webhooks`
  - Anonymous. Validates Stripe signature and routes events to the appropriate handler.
  - If metadata contains `order_id` (and not `teamcart_id`) → handled by Orders webhook; otherwise routed to TeamCart handler.
  - Code: `src/Web/Endpoints/StripeWebhooks.cs:12`.

**Initiate Order: Request/Response**

- Request body (excerpt):
  - `customerId` (Guid), `restaurantId` (Guid), `items[]` (menu item + quantity), `deliveryAddress`, `paymentMethod`, optional `couponCode`, `tipAmount`.
- Response (online payment):
  - `orderId`, `orderNumber`, `totalAmount`, `paymentIntentId`, `clientSecret`.
  - Code (DTOs): `src/Application/Orders/Commands/InitiateOrder/InitiateOrderCommand.cs:33`.

**Backend Flow (Initiate Order)**

- Validation and pricing
  - Ensures caller matches `customerId`; checks restaurant active and menu items valid/available.
  - Computes subtotal, applies coupon (if any), calculates delivery fee, tip, tax, and final total.
  - Code: `src/Application/Orders/Commands/InitiateOrder/InitiateOrderCommandHandler.cs`.

- Create Stripe Payment Intent (online only)
  - Creates `PaymentIntent` with amount in minor units, currency, and metadata:
    - `source=order`, `user_id`, `restaurant_id`, `order_id` (pre‑generated for correlation).
  - Uses automatic payment methods (`AllowRedirects = "never"`) for Stripe Elements/JS.
  - Returns `paymentIntentId` and `clientSecret` to the client.
  - Code: `src/Infrastructure/Payments/Stripe/StripeService.cs:18`, `:33`, `:55`.

- Order creation
  - Online: Order is persisted with initial status `AwaitingPayment` and includes a payment transaction referencing the Stripe `paymentIntentId`.
  - COD: Order is immediately marked `Placed` with a succeeded COD transaction.
  - Code: `src/Domain/OrderAggregate/Order.cs:200`, `:315`.

**Webhook Handling (Stripe → Backend)**

- Endpoint and verification
  - `POST /api/v1/stripe-webhooks` reads raw JSON, validates `Stripe-Signature` using configured Webhook Secret, and extracts `eventId`, `type`, relevant object id (e.g., PaymentIntent ID), and metadata.
  - Code: `src/Infrastructure/Payments/Stripe/StripeService.cs:82`, `src/Web/Endpoints/StripeWebhooks.cs:12`.

- Routing and idempotency
  - If metadata contains `order_id` → `HandleStripeWebhookCommand`.
  - Stores `ProcessedWebhookEvent` records to prevent reprocessing by `eventId`.
  - Code: `src/Application/Orders/Commands/HandleStripeWebhook/HandleStripeWebhookCommandHandler.cs:60`, `src/Application/Common/Models/ProcessedWebhookEvent.cs:1`.

- Event processing for Orders
  - Looks up order by Stripe PaymentIntent ID (`GetByPaymentGatewayReferenceIdAsync`).
  - Handles:
    - `payment_intent.succeeded` → `RecordPaymentSuccess` → order status becomes `Placed` (emits `OrderPaymentSucceeded`, `OrderPlaced`).
    - `payment_intent.payment_failed` → `RecordPaymentFailure` → order status becomes `Cancelled` (emits `OrderPaymentFailed`, `OrderCancelled`).
  - Code: `src/Application/Orders/Commands/HandleStripeWebhook/HandleStripeWebhookCommandHandler.cs:95`, `:120`; domain methods `src/Domain/OrderAggregate/Order.cs:607`, `:634`.

**Client Integration Flow (Stripe.js)**

- 1) (Optional) Pricing preview
  - Call pricing preview to show final totals before initiation.
  - Code: `src/Application/Pricing/Queries/GetPricingPreview/GetPricingPreviewQueryHandler.cs`.

- 2) Initiate order (online)
  - POST `/api/v1/orders/initiate` with `paymentMethod = "CreditCard"`.
  - Include an `Idempotency-Key` header to avoid duplicates (retry‑safe for 5 minutes caching; see Idempotency below).

- 3) Confirm payment on client
  - Use Stripe publishable key and `clientSecret` from the response with Stripe Elements/JS (e.g., `stripe.confirmPayment({...})`).
  - No server `confirm` endpoint is required; PaymentIntent confirmation happens client‑side.

- 4) Webhook drives state
  - Stripe sends events to `/api/v1/stripe-webhooks`.
  - Backend updates Order status to `Placed` or `Cancelled` accordingly.

- 5) Observe status
  - Poll `GET /api/v1/orders/{orderId}/status` with `If-None-Match: "order-{orderId}-v{version}"` to leverage 304s.
  - Optionally subscribe to SignalR hubs if/when real‑time is enabled (`/hubs/customer-orders`).

**Idempotency**

- Commands that implement `IIdempotentCommand` (like InitiateOrder) are wrapped by a caching behavior.
- If `Idempotency-Key` is provided, successful responses are cached for 5 minutes per user+command+key.
- Duplicate requests with the same key return the original response without creating a new PaymentIntent or order.
- Code: `src/Application/Common/Behaviours/SimpleIdempotencyBehaviour.cs`, `src/Application/Orders/Commands/InitiateOrder/InitiateOrderCommand.cs:20`.

**Configuration**

- Stripe options (set via configuration/KeyVault):
  - Section: `Stripe` with keys `SecretKey`, `PublishableKey`, `WebhookSecret`.
  - `SecretKey` sets `StripeConfiguration.ApiKey` at startup.
  - Code: `src/Infrastructure/Payments/Stripe/StripeOptions.cs`, `src/Infrastructure/DependencyInjection.cs:224`.

- Webhook
  - Configure your Stripe endpoint to post to `/api/v1/stripe-webhooks`.
  - Set the corresponding `WebhookSecret` in configuration.

**Testing Locally**

- Use Stripe test keys; ensure the `Stripe` config is present (via appsettings.Development.json override or user secrets/KeyVault).
- Start the API, then forward webhooks:
  - `stripe listen --forward-to https://localhost:<port>/api/v1/stripe-webhooks`
- Place an order with `paymentMethod = "CreditCard"`, confirm payment using Stripe.js with the returned `clientSecret`, and observe Order status changes.

**Error Handling & Edge Cases**

- Payment intent creation failure returns a 4xx/5xx from `POST /orders/initiate` with error details; no order is created in that case.
- Webhooks without `order_id` metadata (or where order is not found) are marked processed and ignored safely.
- Duplicate Stripe events are skipped by `ProcessedWebhookEvents` idempotency store.

**Design Notes**

- Amounts are converted to Stripe minor units (`long` cents) from domain `Money`.
- Payment Intent metadata is the primary correlation mechanism; keep `order_id` intact.
- Automatic payment methods are enabled and set to not redirect, aligning with Stripe Elements UX.

**Quick Reference (Files)**

- Endpoint (initiate/status): `src/Web/Endpoints/Orders.cs`
- Command + handler: `src/Application/Orders/Commands/InitiateOrder/*`
- Stripe service: `src/Infrastructure/Payments/Stripe/StripeService.cs`
- Webhook endpoint: `src/Web/Endpoints/StripeWebhooks.cs`
- Webhook handler (orders): `src/Application/Orders/Commands/HandleStripeWebhook/*`
- Order domain payment transitions: `src/Domain/OrderAggregate/Order.cs`
- Idempotency behavior: `src/Application/Common/Behaviours/SimpleIdempotencyBehaviour.cs`
- Config wiring: `src/Infrastructure/DependencyInjection.cs`, `src/Infrastructure/Payments/Stripe/StripeOptions.cs`
