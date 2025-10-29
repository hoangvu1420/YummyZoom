# Online Payments Integration (Client Guide)

This guide explains how client apps integrate with the backend’s online payment flow for both Individual Orders and TeamCart member payments. It focuses on when to call APIs, how to confirm payments with Stripe on the client, and how to observe status changes reliably.

## Overview

- Provider: Stripe (Payment Intents API)
- Pattern: Server creates a Payment Intent → Client confirms payment with `clientSecret` → Backend receives Stripe webhooks and updates order/cart state → Clients get updates via SignalR and/or ETag polling.
- No client-to-server “payment success” callback is required; webhooks are the source of truth.

> Note
> Use your Stripe Publishable Key in the client. Never embed the Secret Key in apps.

---

## Individual Orders (Online Payment)

### 1) Create the order and get `clientSecret`

- Endpoint: `POST /api/v1/orders/initiate`
- Body includes items, delivery address, and `paymentMethod` (`CreditCard`, `ApplePay`, `GooglePay`, `PayPal`, or `CashOnDelivery`). Currently, only `CreditCard` (backed by Stripe) and `CashOnDelivery` are supported.
- For non-COD methods, the response includes `paymentIntentId` and `clientSecret`.
- Send `Idempotency-Key: <uuid-v4>` to protect against duplicate order creation.

Successful response (fields of interest):
```json
{
  "orderId": "<uuid>",
  "paymentIntentId": "pi_...",
  "clientSecret": "pi_..._secret_..."
}
```

### 2) Confirm payment on the client

- Web: Use Stripe.js with the Payment Element or Card Element.
  - Initialize Stripe with the Publishable Key.
  - Create/mount Elements.
  - Call `stripe.confirmPayment({ elements, clientSecret })`.
  - 3‑D Secure (SCA) is handled via Stripe’s modal flow.
- iOS / Android: Use official Stripe SDKs and call the SDK’s confirm method with the `clientSecret`.

> Tip
> The backend enables Stripe Automatic Payment Methods with redirects disabled. Prefer in‑place confirmation flows.

### 3) Show a pending state, then observe order status

- Do not assume payment success from the SDK return alone. Success is finalized when the backend processes the Stripe webhook.
- Observe status via one or both:
  - SignalR hub (Customer): subscribe to the order and listen for `ReceiveOrderPaymentSucceeded` / `ReceiveOrderPaymentFailed`.
  - Polling: `GET /api/v1/orders/{orderId}/status` with strong `ETag` validators.

Expected payment statuses in the backend order lifecycle:
- `AwaitingPayment` → initial state after online order creation
- `Placed` → on `payment_intent.succeeded`
- `Cancelled` → on `payment_intent.payment_failed`

> See also
> “Get Order Status” in 03‑Individual‑Orders for ETag and caching details.

### 4) Failure and retry UX

- If payment fails, the backend cancels the order. Show a clear retry option that creates a new order (re‑initiate) rather than trying to “fix” the cancelled one.
- Keep the original order available for reference history.

---

## TeamCart Member Payments (Online)

### 1) Lock cart and fetch quoted amounts

- Follow the TeamCart workflow until the Host locks the cart for payment. The VM (`GET /api/v1/team-carts/{id}/rt`) exposes `quoteVersion` and per‑member `quotedAmount`.

### 2) Initiate a member payment

- Endpoint: `POST /api/v1/team-carts/{id}/payments/online`
- Auth: Member only. Response includes `paymentIntentId` and `clientSecret`.
- The server attaches metadata (`teamcart_id`, `member_user_id`, `quote_version`, `quoted_cents`) to the Stripe Payment Intent.

### 3) Confirm payment on the client

- Use Stripe SDKs as in Individual Orders with the provided `clientSecret`.
- Keep the cart screen in a pending state while waiting for backend confirmation.

### 4) Observe TeamCart updates

- Real‑time VM (Redis backed): `GET /api/v1/team-carts/{id}/rt` with ETag polling. The VM `version` increases on every visible change; `quoteVersion` is used for financial consistency checks.
- Members that have paid online transition their `paymentStatus` to `PaidOnline`. When all required online payments are complete, the cart enters `ReadyToConfirm` and the Host can convert it to an order.

> Note
> If the cart’s `quoteVersion` changes after a member created an intent, the webhook may be acknowledged as stale (no‑op). Prompt the user to refresh and re‑initiate.

---

## Real‑time & Polling (What to implement on the client)

- SignalR (Customer Orders):
  - Connect to the customer orders hub and call `SubscribeToOrder(orderId)` after creating the order.
  - Handle `ReceiveOrderPaymentSucceeded` and `ReceiveOrderPaymentFailed` to update the UI instantly.
- Polling with validators:
  - `GET /api/v1/orders/{orderId}/status` — send `If-None-Match` with the last `ETag`. Expect `304 Not Modified` when unchanged.
  - TeamCart VM: `GET /api/v1/team-carts/{id}/rt` with `If-None-Match` on the VM’s strong ETag.

---

## Headers, Auth, and Idempotency

- `Authorization: Bearer <access_token>`: required for all customer actions.
- `Idempotency-Key`: strongly recommended on order creation and TeamCart create/convert operations.
- Never attempt to send a client‑asserted “payment success” to the backend — webhooks are authoritative.

---

## Error Handling Guidelines

- Order/Payment initiation failures (4xx/5xx):
  - Display actionable messages and allow the user to correct input and retry.
  - Preserve user inputs (items, address) for a quick retry.
- Stripe confirmation errors:
  - Show inline card errors; allow re‑submit without re‑creating the order if the intent is still valid.
  - For hard failures (declined, requires new payment method), expect the backend to cancel on webhook; surface a new “Start over” action.

---

## Security Notes

- Publishable vs Secret Keys: only the Publishable Key belongs in apps.
- Webhooks are verified by the backend; no shared secrets in the client.
- Do not trust client‑side amounts; the server computes and validates totals.

---

## Reference Endpoints

- Create Individual Order: `POST /api/v1/orders/initiate`
- Order Status: `GET /api/v1/orders/{orderId}/status`
- TeamCart Real‑time VM: `GET /api/v1/team-carts/{id}/rt`
- Initiate TeamCart Member Online Payment: `POST /api/v1/team-carts/{id}/payments/online`
- Convert TeamCart to Order (Host): `POST /api/v1/team-carts/{id}/convert`

---

## Test Mode Tips

- Use Stripe test payment methods (e.g., `pm_card_visa`) in non‑production.
- Expect occasional 3‑D Secure challenges in test to validate SCA handling.

---

## Implementation Checklist (Client)

- Initialize Stripe SDK with Publishable Key.
- Build order screen: create order → get `clientSecret` → confirm payment.
- Subscribe to order updates (SignalR) and implement status polling fallback with ETags.
- Implement TeamCart payment initiation and VM polling.
- Handle SCA, errors, and retries gracefully.

