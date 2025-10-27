Got it—here’s a crisp, implementation-ready spec for the **backend** to support the Phase-1 flow (**Push-first, Poll-as-needed**). It’s organized so you can hand it to the team and build.

# 1) Core objectives

* **Single source of truth:** `GET /orders/{id}/status`
* **Realtime trigger:** FCM **data** push `{ orderId, version }` on **every** state change
* **Clients pull details:** Clients always fetch `/status` before surfacing UI/notification
* **Efficient polling:** Support conditional responses (ETag / `sinceVersion`) to minimize load
* **Reliability:** Durable eventing with retries; dedupe by `version`

---

# 2) Domain model & invariants

**Order**

* `orderId: string`
* `userId: string`
* `state: enum` (`Created`, `Confirmed`, `Preparing`, `OutForDelivery`, `Delivered`, `Canceled`, `Failed`, …)
* `version: int` — **strictly monotonic**, increment on *any* user-visible change
* `updatedAt: instant`
* Optional: `eta`, `rider`, `steps[]`, `notes`, etc.

**Invariants**

* `version` increments **atomically** with `state` (same transaction).
* Terminal states (`Delivered|Canceled|Failed`) freeze `version` (no further pushes).
* Only the **owner** or authorized viewer can read the status.

---

# 3) Status API (polling endpoint)

### `GET /orders/{id}/status`

**Auth:** required (user must own the order or be explicitly authorized).

**Query params (optional):**

* `sinceVersion=<int>`: if server’s `version` ≤ provided → return **204 No Content** (or **304 Not Modified** if using validators)
* `include=meta` (optional): control payload verbosity (e.g., include `steps`, `rider`)

**Request headers (optional):**

* `If-None-Match: "<etag>"` or `If-Modified-Since: <http-date>`

**Responses:**

* **200 OK** with JSON:

  ```json
  {
    "orderId": "abc123",
    "state": "OutForDelivery",
    "version": 7,
    "updatedAt": "2025-10-27T08:41:00Z",
    "eta": "2025-10-27T09:05:00Z",
    "rider": { "name": "…" },
    "steps": [ /* timeline */ ]
  }
  ```

  Include headers:

  * `ETag: "order-abc123-v7"`
  * `Last-Modified: <updatedAt>`
  * `Cache-Control: no-cache, must-revalidate` (or `private, no-store` if stricter)
* **204 No Content** when unchanged **with `sinceVersion`**.
* **304 Not Modified** when unchanged **with ETag/If-Modified-Since**.
* **401/403** unauthorized; **404** if order not found/visible.

**Rules:**

* Compute `ETag` deterministically (e.g., `"order-<id>-v<version>"`).
* Prefer **either** `sinceVersion` **or** validators; support both but handle conflicts predictably (validators take precedence).

---

# 4) Event emission (on state change)

When an order update is **committed**:

1. Persist new `state`, increment `version`, set `updatedAt`.
2. Write an **OrderChanged** record into an **Outbox** table/stream:

   * `{ id, orderId, version, userId, occurredAt }`
3. A background **Dispatcher** pulls the outbox and sends FCM **data** message:

   * Payload: `{ "orderId": "<id>", "version": <int> }`
   * Platform config (per target token):

     * **Android:** `priority=high`, `collapse_key="order_<id>"`, `time_to_live` short (e.g., 300s)
     * **iOS/APNs:** headers `apns-push-type=background`, `content-available=1`, `apns-collapse-id=order_<id>`, short expiry
     * **Web:** standard Webpush with data payload
4. Mark outbox item **sent** (idempotently). Retries on transient failures.

**Why Outbox:** ensures push delivery attempts are **durable** and consistent with DB state; prevents “updated DB but forgot to push”.

---

# 5) Targeting & addressing

You can choose either (or both):

**A) Direct tokens**

* Maintain a table: `{ userId, deviceToken, platform, enabled, updatedAt }`
* On order change → push to **all active tokens** for `userId`.

**B) Per-order topics** (optional)

* Topic: `orders_<orderId>_<userHash>`
* Client subscribes on “start tracking”, unsubscribes on terminal
* Server publishes once; FCM distributes

**Recommendation:** Start with **direct tokens** (simpler). Add topics if fan-out or ACLs benefit.

---

# 6) Notification strategy (server side)

Phase-1 default is **data-only** push; the app will fetch and render a **local** notification.

**Optional safety net for critical states:**

* For `OutForDelivery`, `ArrivingSoon`, `Delivered`, send **hybrid**:

  * `notification` (title/body) **and** `data`
  * iOS: optionally set `mutable-content=1` to allow client NSE to refresh text
* Keep text generic & non-PII; details still come from `/status`.

**Collapse & TTL**

* Always set per-order collapse keys/ids so the newest replaces older alerts.
* Use short TTLs (e.g., 2–5 minutes) to avoid late, stale pushes.

---

# 7) Security & authorization

* **AuthN:** OAuth2/JWT (or existing session) on `/status`.
* **AuthZ:** Verify `order.userId == auth.userId` (or shared access via order share records).
* **PII:** Push payloads contain **no** PII. Only `{orderId, version}`.
* **Rate limits:** Per user and per order to protect `/status` (e.g., token bucket; generous since clients use validators).

---

# 8) Idempotency & deduplication

* **Outbox dispatcher** must be idempotent:

  * Use `outbox.id` as the unique send key; record send attempts and final result.
* **FCM payload** carries `version`; clients ignore `<= local.version`.
* **Server** does not need to dedupe incoming `/status` requests; just honor validators.

---

# 9) Performance & scalability

* **Indexes:** `(orderId)` primary; secondary index `(userId, updatedAt desc)` for audits; `(orderId, version)` unique.
* **Read path hotness:** `/status` should be **fast** and side-effect-free; consider a **read model** (materialized view) denormalized for quick fetch.
* **Conditional responses:** Expect many **304/204** hits; keep those very cheap.
* **Compression:** Enable gzip/br on JSON responses.
* **Concurrency:** If multiple updates race, `version` increments inside a **transaction** (or via DB sequence).

---

## TL;DR

* Build a rock-solid **`/status`** endpoint with conditional responses.
* Use an **Outbox** to reliably push **data-only** FCM `{orderId, version}` on each change.
* Keep **`version` monotonic** and atomic with state updates.
* Provide **short-TTL, collapsed** pushes; clients fetch details and show local notifications.
* This ships Phase-1 cleanly and sets you up to drop in SignalR later without changing the contract.
