# Team Cart Implementation Logic & Integration Guide

## Overview
This report details the backend logic for the Team Cart payment and order conversion flows. It is intended for the Front-End team to guide the integration of client-side logic.

## 1. Member Payment Step
The payment phase begins only after the Host has **Locked** the cart and **Finalized Pricing** (e.g., added tips/coupons). The cart status must be `Finalized`.

### Prerequisites
-   **Cart Status**: Must be `Finalized`.
-   **Member Logic**: Only members with items in the cart are required to pay. Members with no items are excluded from the requirement checks.

### Payment Flow
Every participating member (including the Host) must individually "commit" to a payment method. This is tracked per user via `MemberPayment` records.

#### Option A: Cash On Delivery (COD)
-   **Action**: Client calls `CommitToCodPayment`.
-   **Backend Logic**:
    -   Validates the amount against the member's share (calculated via `QuoteLite` or item sum).
    -   Removes any prior payment record for this user (allowing switching between Online/COD).
    -   Creates a `MemberPayment` record with status `CashOnDelivery`.
    -   **Trigger**: Automatically checks if *all* members have paid. If yes, transitions cart to `ReadyToConfirm`.

#### Option B: Online Payment
-   **Action**: Client calls `InitiateMemberOnlinePayment`.
-   **Backend Logic**:
    -   **Initiation**:
        -   Calculates the exact amount the member owes.
        -   Calls the Payment Gateway (e.g., Stripe) with metadata (`teamcart_id`, `member_user_id`, `quote_version`).
        -   **Returns**: `ClientSecret` (or similar) to the client SDK.
        -   **Note**: This does **not** update the cart state yet.
    -   **Completion (Webhook)**:
        -   When the payment succeeds, the gateway calls the backend webhook.
        -   Backend calls `RecordSuccessfulOnlinePayment`.
        -   Creates a `MemberPayment` record with status `PaidOnline`.
        -   **Trigger**: Automatically checks if *all* members have paid. If yes, transitions cart to `ReadyToConfirm`.

## 2. Mixed Payment Methods
The system **fully supports mixed payment methods** within a single Team Cart.

-   **Logic**: The backend tracks payments individually per `UserId`.
-   **Completion Condition**: The `CheckAndTransitionToReadyToConfirm` process iterates through all members with items. It only requires that **each member has a valid payment record** (either `CashOnDelivery` OR `Online` with `PaidOnline` status).
-   **Example**:
    -   Member A pays via Stripe (Online).
    -   Member B selects "Pay Cash" (COD).
    -   **Result**: Valid. Once both actions are complete, the cart moves to `ReadyToConfirm`.

## 3. Merging Totals (Conversion to Order)
When the Host clicks "Place Order" (converting the cart), the backend performs the following merge logic in `TeamCartConversionService`:

1.  **Status Check**: Cart must be in `ReadyToConfirm` status (meaning all members have paid/committed).
2.  **Order Creation**: A single `Order` entity is created.
3.  **Payment Transactions**:
    -   The system does **not** merge the money into a single lump sum record.
    -   Instead, it creates a list of `PaymentTransaction` entities attached to the Order.
    -   **One Transaction Per Member**: For every `MemberPayment` in the cart, a corresponding `PaymentTransaction` is created on the Order.
        -   Tracks `PaidByUserId`.
        -   Preserves `PaymentMethod` (COD vs CreditCard).
        -   Links `PaymentGatewayReferenceId` (for online payments).
4.  **Total Verification**:
    -   The system calculates the final Order Total (Items + Tax + Delivery + Tip - Discount).
    -   It compares this against the sum of all `MemberPayment`s.
    -   **Adjustment Factor**: If there is a tiny discrepancy (e.g., due to rounding or floating-point math), the system applies a proportional `adjustmentFactor` to the recorded transaction amounts to ensure they sum up *exactly* to the Order Total.

## Integration Checklist for Frontend
1.  **Poll for Status**: Watch for the `TeamCartStatus` changing to `Finalized` to enable payment UI.
2.  **Display Member Status**:
    -   Show "Paid/Ready" indicators for members who have completed their step.
    -   Track the `ReadyToConfirm` status on the cart to enable the Host's "Place Order" button.
3.  **Handle Payment Switching**:
    -   Users can switch methods. If a user fails an online payment, allow them to retry or select COD.
    -   If a user selects COD, they can still change their mind and pay Online (which overrides the COD status).
4.  **Checkout**: Only call `ConvertTeamCartToOrder` when the cart status is strictly `ReadyToConfirm`.
