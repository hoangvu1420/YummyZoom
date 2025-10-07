# Workflow: Payments & Webhooks (Provider)

This guide explains how online payments finalize orders via Stripe webhooks, what the platform expects in incoming events, and how status changes appear to providers.

—

## Overview

- Entry point: `POST /api/v1/stripe-webhooks`
- Auth: AllowAnonymous (signature verified via `Stripe-Signature` header)
- Routing: The webhook endpoint inspects payload metadata to decide whether the event is for an Order (`order_id`) or a TeamCart (`teamcart_id`). Order events are handled by the Orders webhook processor.
- Idempotency: Every Stripe event ID is persisted and processed at most once.
- Outcomes (Orders):
  - `payment_intent.succeeded` → Order records payment success → status becomes `Placed`
  - `payment_intent.payment_failed` → Order records payment failure → status becomes `Cancelled`

—

## Webhook Endpoint

### POST /api/v1/stripe-webhooks

- Headers
  - `Stripe-Signature`: required. If missing or invalid, the request is rejected with 400.
- Body
  - Raw JSON from Stripe (the server reads the request body verbatim for signature verification).
- Behavior
  1. Verify signature (`Stripe-Signature`).
  2. Construct an internal event object with fields like `EventId`, `EventType`, `RelevantObjectId` (e.g., PaymentIntent ID), and `Metadata`.
  3. Determine routing by `Metadata`:
     - If `teamcart_id` exists and `order_id` does not → route to TeamCart webhook handler.
     - Otherwise → route to Orders webhook handler.
  4. Return:
     - 200 OK on success (even if a non-order event is safely ignored).
     - 400 Bad Request for signature/format errors.

—

## Orders Webhook Processing

This section describes how order-related events are handled once routed to the Orders processor.

Steps
1. Signature already verified by the endpoint.
2. If `order_id` metadata is absent → mark event processed and return 200 (not applicable to orders).
3. Enforce idempotency: if the `EventId` is already stored in `ProcessedWebhookEvents`, return 200.
4. Load the Order by payment gateway reference ID (`RelevantObjectId`, e.g., PaymentIntent ID). If not found → mark processed and return 200.
5. Switch on `EventType`:
   - `payment_intent.succeeded` → `order.RecordPaymentSuccess(referenceId)`
   - `payment_intent.payment_failed` → `order.RecordPaymentFailure(referenceId)`
6. Persist changes; store the processed event; return 200.

Notes
- If the Order is not in `AwaitingPayment` when a success/failure arrives, the domain returns a validation error (`Order.InvalidStatusForPaymentConfirmation`). The API surfaces a 400 in that case.
- Payment success transitions the Order to `Placed` and emits both a payment event and a lifecycle `OrderPlaced` event (see Real-time integration).

—

## Provider Impact

- New paid orders appear in the “New Orders” queue (`GET /restaurants/{id}/orders/new`).
- Real-time: after payment success or failure, provider clients can receive updates via the events hub. See Docs/API-Documentation/04-Real-time-Events-API.md.
- Typical flow with online payment
  1. Customer initiates order (status `AwaitingPayment`).
  2. Stripe confirms success → webhook sets status to `Placed`.
  3. Restaurant triages in New Orders → Accepts and proceeds through the kitchen.

—

## TeamCart Note (collaborative orders)

- The webhook router also supports TeamCart payments. If `teamcart_id` is present (and `order_id` absent), events are routed to the TeamCart webhook handler.
- After group payment and conversion, the resulting Order enters the standard provider flow and appears in the New Orders queue.

—

## Failure & Retry Semantics

- Stripe may retry delivery; the platform de-duplicates by `EventId` to ensure idempotency.
- If the event references a payment object that the platform cannot map to an Order, the event is silently acknowledged (200) after recording as processed.
- Signature failures or malformed payloads are rejected (400) without persistence.

—

## Response Reference

- Success (processed or safely ignored): `200 OK` (empty body)
- Signature missing/invalid: `400 Bad Request`
- Internal errors: `500 Internal Server Error`

—

## Security

- Webhook endpoint does not use standard authentication; it relies on Stripe’s signature validation.
- Keep the signing secret rotated and scoped to production vs. non-production environments.
- Do not expose the webhook publicly in non-secure environments without the signature check enabled.

Versioning: All routes use `/api/v1/`.