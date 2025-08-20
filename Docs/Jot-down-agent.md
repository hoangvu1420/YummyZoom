Test_planning

## Checklist
- Analyze functional test guidelines (patterns, helpers)
- Examine existing order command & query test structure
- Identify comprehensive test cases for GetOrderById
- Devise optimal data setup (including items + customizations)
- Define helper additions / where to place them
- Provide step‑by‑step implementation sequence
- Call out risks (e.g., role naming, JSON parsing edge cases)
- No code changes yet (planning only)

## 1. Observed Current Patterns
- Query tests live under: tests/Application.FunctionalTests/Features/<Feature>/Queries/.
- They inherit BaseTestFixture (simple unless a cohesive base is warranted).
- Use static Testing facade: SendAsync, RunAsUserAsync, TestData access, etc.
- Order lifecycle tests already create realistic orders via InitiateOrderTestHelper (but without customizations).
- Authorization for order commands uses restaurant staff helpers (OrderLifecycleTestHelper).
- No existing query tests for orders yet → we set a precedent.

## 2. Target File & Structure
Create directory:
tests/Application.FunctionalTests/Features/Orders/Queries/
Add file:
GetOrderByIdTests.cs (single class; can future-split if more scenarios grow).

Optionally (only if complexity rises) add OrderQueryTestBase : BaseTestFixture (initially unnecessary—keep lean).

## 3. Core Test Scenarios (Updated for Built‑in Customizations Support)
1. Customer_HappyPath_ReturnsOrderDetails  
   - Default customer creates an order (no customizations) and retrieves it.  
   - Assert success + mapping basics (Status, monetary fields, item count, address, coupon null expectations, etc.).

2. RestaurantStaff_HappyPath_ReturnsOrderDetails  
   - Staff (RunAsDefaultRestaurantStaffAsync) retrieves an order from their restaurant.

3. UnauthorizedOtherCustomer_ShouldReturnNotFound  
   - Create order as default customer; run as a different plain user (no staff/owner roles) and expect Result failure with GetOrderByIdErrors.NotFound (not a thrown Forbidden exception).

4. NonexistentOrder_ShouldReturnNotFound  
   - Random Guid (not in DB) → NotFound.

5. OrderWithCustomizations_ShouldProjectParsedCustomizations  
   - Build InitiateOrderCommand with one item that includes valid customization groups & choice IDs (pattern mirrors existing InitiateOrderCustomizationTests).  
   - Send command, then query GetOrderById.  
   - Assert Items[index].Customizations count, names, and price adjustments match snapshot (GroupName, ChoiceName, optional PriceAdjustment).  
   - No direct DB mutation—customization snapshots are produced by the command handler.

6. OrderWithCouponAndTip_ShouldReturnFinancialBreakdown  
   - Create order via command providing CouponCode and TipAmount.  
   - Verify DiscountAmount > 0, TipAmount equals provided, AppliedCouponId not null (if persisted), and that TotalAmount, SubtotalAmount, and Discount/Tip/Tax/DeliveryFee are non‑negative with TotalAmount > 0. (Optionally assert subtotal + fees - discount ≈ total with tolerance.)

7. MalformedCustomizationJson_ShouldFallbackToEmptyList (Optional)  
   - Since the command now always serializes well‑formed customization snapshot JSON, this state can only be produced by legacy data or manual corruption.  
   - Optional approach: AFTER creating an order with customizations, manually UPDATE the underlying OrderItems.SelectedCustomizations column to an invalid string, then execute GetOrderById and assert Customizations == empty.  
   - Keep optional; do not introduce helper unless needed (single targeted SQL/EF context mutation inside test is acceptable if team tolerates minimal white‑box setup).  
   - Mark as [Explicit] or [Ignore] if deemed out of scope initially.

8. RestaurantOwner_Authorized (Optional)  
   - Owner retrieving order (RunAsDefaultRestaurantOwnerAsync). Validates parity with staff roles.

9. Administrator_AuthorizedOrPotentialBug (Exploratory)  
   - Run as Administrator (role string constant) and retrieve order.  
   - Exposes potential mismatch if handler checks "Admin" vs constants defining "Administrator". Document result; mark TODO if failing.

(If test suite time is a concern, implement 1–6 first; 7–9 optional.)

## 4. Data Setup Strategy (Updated)
We now rely exclusively on the enhanced InitiateOrderCommand to create orders with customizations; no post‑creation mutation required.

Capabilities leveraged:
- `InitiateOrderTestHelper.BuildValidCommand(...)` to get a base command.
- Inject customizations per item by constructing `OrderItemDto` with a `List<OrderItemCustomizationRequestDto>` referencing known test customization groups & choice IDs (see existing `InitiateOrderCustomizationTests` for patterns).

Approach per scenario:
- Scenarios 1–4: Use base command (possibly adjusting menu item IDs) with no customizations.
- Scenario 5: Build one item with multiple customization groups/choices (e.g., Burger Add‑Ons: Extra Cheese + Bacon). Assign to command Items before sending.
- Scenario 6: Same as scenario 1 plus coupon code and tip values (helper may already support a coupon variant; otherwise override properties in the record `with { CouponCode = ..., TipAmount = ... }`).
- Scenario 7 (optional malformed): Only scenario performing manual data alteration (see notes below); keep isolated.

No new helper class is needed for customizations. Keep plan lean. If future queries require repeated complex arrangement (e.g., multi‑item, multi‑group combinations), consider extracting small builder methods inside the test class (private methods) instead of a shared helper file.

Malformed JSON Optional Handling:
- If implemented, perform a targeted EF context update or raw SQL UPDATE inside that single test after order creation to set `SelectedCustomizations` to 'INVALID_JSON'. This avoids introducing a global helper whose only purpose is simulating legacy corruption.

## 5. Assertions Checklist per Scenario (Adjusted)
Common baseline assertions (for success cases):
- result.ShouldBeSuccessful()
- result.Value.Order.OrderId == createdOrderId
- Status is expected (Placed initially; or as per scenario)
- Items.Count == expected (default 2 from helper)
- Monetary fields: TotalAmount > 0; currency codes == "USD" (or whatever TestData seeds).
- Address fields not null (from InitiateOrderTestHelper.DefaultDeliveryAddress)
- AppliedCouponId null vs not null depending on scenario.

Customizations test (Scenario 5):
- For customized item: Customizations.Count == number of choices passed in command.
- Each customization fields match snapshot (GroupName, ChoiceName, PriceAdjustmentAmount/Currency as seeded by test data factory).
- LineItemTotalAmount reflects base + sum(choice price adjustments).

Malformed JSON test (optional):
- Customizations.Count == 0 (parser fallback, no exception).

Authorization negative:
- result.IsFailure + result.Error == GetOrderByIdErrors.NotFound (confirm code).

Nonexistent:
- Same NotFound error code.

(Optional admin test):
- If fails due to "Admin" vs "Administrator" mismatch, document as potential bug (decide to adjust handler or skip). The plan flags this early.

## 6. Helper Additions Summary (Revised)
No new helper required for standard scenarios. Reuse:
- `InitiateOrderTestHelper` for baseline command creation.
- Inline private builder methods inside `GetOrderByIdTests` for readability, e.g. `BuildBurgerWithAddOns(params Guid[] choiceIds)` mirroring existing customization command tests.

Optional (only if malformed JSON test implemented): a tiny private method `CorruptFirstItemCustomizationsAsync(Guid orderId)` inside the test class performing a direct update. Omit unless that optional test is enabled.

## 7. Risks & Mitigations
- Role Name Mismatch: Handler uses role checks (`RestaurantStaff:{restaurantId}` and `Roles.Administrator`). If Administrator constant differs from any hard‑coded string (`Admin`), admin exploratory test may surface a mismatch. Add TODO if failing.
- Flaky Time Assertions: Avoid asserting exact timestamps; only assert presence/non-null or ordering if stable.
- Monetary Rounding: Don’t tightly recompute totals; basic consistency or positive values only. If performing arithmetic, allow small tolerance (e.g., +/- 0.01m) for rounding.
- Customizations: Since now generated via command, snapshot integrity is already validated by existing InitiateOrderCustomizationTests; here only assert correct projection in query DTO.
- Malformed JSON Scenario: Optional; ensure corruption happens after order creation and only in that test to avoid polluting shared state.
- Parallelism: Maintain isolation; each test creates its own order. Avoid static caching beyond existing test infrastructure patterns.

## 8. Step-by-Step Implementation Plan (Revised)
1. Create directory `tests/Application.FunctionalTests/Features/Orders/Queries` if missing.
2. Add `GetOrderByIdTests.cs` inheriting `BaseTestFixture` (no dedicated test base initially).
3. Implement scenarios 1–6 sequentially:
   - Use `InitiateOrderTestHelper.BuildValidCommand(...)` for order creation.
   - For scenario 5 build a customized `OrderItemDto` replicating pattern from `InitiateOrderCustomizationTests` (copy the private builder approach).
4. (Optional) Add scenario 7 with inline corruption logic (only if agreed it adds value now).
5. Add scenarios 8–9 (optional) focusing on role variations & potential admin naming issue.
6. Consolidate common assertions into small local methods if duplication grows (e.g., `AssertBasicOrderFields(resultOrder, createdOrderId, expectedItemCount)`).
7. Run functional tests; adjust assertions to actual persisted field names / values.
8. Add TODO comments for any observed role mismatch or design clarifications.
9. Submit PR; no new helper file introduced (keeps change surface minimal).

## 9. Future Extensions (Not in initial implementation)
- Add pagination/list query tests once those handlers exist.
- Introduce a reusable CustomizationBuilder DSL if many tests need varied customizations.
- Add snapshot integrity test (ensuring item names & prices are persisted even after menu changes—when such behavior is implemented).
- Add performance safeguard test (ensuring only two SQL queries executed—requires interception instrumentation).

## 10. Acceptance Criteria Mapping (Updated)
- Functional coverage for GetOrderById success, authorization, not found, customizations, coupon/tip → implemented tests (scenarios 1–6).
- Customizations verified via command-driven creation (no raw JSON edits) → black-box integrity maintained.
- Optional malformed JSON projection behavior validated only if scenario 7 enabled.
- No unnecessary test helpers introduced; reuse existing command test infrastructure.
- Risks identified (role naming, rounding, corruption) with mitigations documented.

Let me know if you’d like me to proceed with implementing the helper + tests or adjust the scenario set first.