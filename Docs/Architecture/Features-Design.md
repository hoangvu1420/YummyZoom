## Features

### 🧑‍🍳 **Restaurant Features**

#### 1. Restaurant Profile Management

* Set restaurant name, logo, contact info, business hours, and location.
* Define cuisine type and brief description.

#### 2. Menu Management

* Add/edit/remove menu items.
* Update item descriptions, prices, and images.
* Set availability per item (e.g., “out of stock” toggle).
* Offer customization (e.g., toppings, size, spice level).

#### 3. Order Management

* Receive real-time order notifications.
* Accept or reject orders.
* View detailed order info (items, notes, customer name).
* Update order status: *Accepted → Preparing → Ready → Delivered* (simulated delivery).
* Access order history with filters.

#### 4. Coupon Management (New)

* Create and manage coupons (percentage, fixed amount, free items, etc.).
* Apply coupons to specific items, categories, or whole orders.
* Set usage limits and validity periods.
* View coupon usage statistics.

---

### 👤 **Customer Features**

#### 1. Browse Restaurants and Menus

* Search by cuisine, location, rating, dietary preferences.
* View menus with dish details, images, and prices.

#### 2. Placing an Order

* Select dishes, customize them, and add to cart.
* Review cart, apply coupons, and checkout.

#### 3. Checkout & Payments

* Save and use payment methods (optional: tokenized Stripe/PayPal).
* Apply promo codes during checkout.
* Simple one-click checkout for returning users.

#### 4. Order Tracking (Simulated)

* Track order status in real-time: *Placed → Accepted → Preparing → Delivered*.
* View estimated delivery time (static or mock logic).

#### 5. Delivery Preferences (Simplified)

* Save multiple delivery addresses.
* Add special instructions (e.g., "leave at door").

#### 6. Order History & Reordering

* View previous orders and reorder with one click.

#### 7. Rating and Reviews (New)

* Rate restaurants after delivery (1–5 stars).
* Leave written feedback.
* View your own past reviews.

#### 8. Use Coupons (New)

* Apply valid promo codes to orders during checkout.
* See applied discount in order summary.

---

### 👥 **TeamCart (Group Order) — Customer**

#### 1. Create & Share (New)

* Host creates a shared cart with optional deadline and note.
* Share join link/token with friends to collaborate.

#### 2. Join & Contribute

* Members join via link, add items with normal customizations.
* See per-member and cart totals update in real time.

#### 3. Lock & Pay

* Host locks the cart to freeze items before payment.
* Members pay their share online or commit Cash on Delivery (COD).
* Host can apply tip and a cart-level coupon.

#### 4. Convert to Order

* When everyone has paid/committed, host converts TeamCart into a single order.
* Payments are reconciled; restaurant receives a normal order.

#### 5. Real-time Updates

* Live updates via realtime hub for joins, item changes, lock, and payment status.

### 🛠️ **Admin / Support Features**

#### 1. Admin Dashboard

* View platform metrics: total orders, active users, revenue.
* Manage customers and restaurants (add/edit/deactivate accounts).
* View all orders and update statuses manually if needed.

#### 2. Restaurant Management

* Onboard and verify restaurants (manual toggle).
* View and manage restaurant menus and coupons.

#### 3. Order Oversight

* Monitor active orders across the system.
* Manually update order statuses or simulate delivery.
* Cancel/refund orders if needed.

#### 4. Coupon and Review Monitoring

* View all coupon campaigns.
* See customer feedback and ratings.
* Moderate or delete inappropriate reviews if required.

---

### ✅ Summary of Functional Scope

| **Module**     | **Customer**                      | **Restaurant**        | **Admin**                          |
| -------------- | --------------------------------- | --------------------- | ---------------------------------- |
| Profile & Auth | ✔️                                | ✔️                    | ✔️                                 |
| Menu           | View                              | Create & Edit         | Moderate                           |
| Order          | Place, track, review              | Accept, update status | Monitor & override                 |
| TeamCart       | Create/join, add items, pay       | Receives order after conversion | Monitor & override       |
| Coupon         | Apply promo code                  | Create/manage coupons | Monitor usage                      |
| Reviews        | Submit/view reviews               | View feedback         | Moderate content                   |
| Payment        | Mock/real payments, apply coupons | View revenue          | Monitor, trigger refunds           |
| Admin Panel    | —                                 | —                     | Full control over data & workflows |

---
