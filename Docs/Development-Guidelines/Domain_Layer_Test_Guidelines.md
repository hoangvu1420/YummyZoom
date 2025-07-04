
### Unit Testing Guidelines for the Domain Layer

**Core Philosophy:** A unit test for a domain object verifies that a **single business operation** results in the **correct state change** and/or produces the **correct outcome** (success, failure, or domain event), while correctly enforcing all business rules (invariants).

---

#### 1. Testing Aggregate Roots (e.g., `Menu`, `Order`, `RestaurantAccount`)

This is the most important area of domain testing. Tests for Aggregate Roots should be structured around their public methods, treating each method as a distinct business use case.

**What to Test for Each Public Method:**

* **The "Happy Path" (Success Case):**
  * **Goal:** Verify that when the operation is performed with valid inputs and on a valid state, it succeeds.
  * **Cases to Cover:**
    * Does the method return a `Success` result?
    * Are the aggregate's properties correctly updated to the new state? (e.g., after `menu.Disable()`, is `menu.IsEnabled` now `false`?).
    * Are items correctly added or removed from internal collections? (e.g., after adding a `MenuCategory`, does the `menu.Categories` list contain it?).
    * Is the correct `DomainEvent` raised? (e.g., does calling `Menu.Create()` add a `MenuCreated` event to the aggregate's event list?).

* **Business Rule Violations (Failure Cases):**
  * **Goal:** Verify that the method correctly prevents operations that would violate a business rule or invariant.
  * **Cases to Cover:**
    * **State-Based Rules:** Test calling the method when the aggregate is in the *wrong* state.
      * *Example (`Order`):* Can you call `order.Accept()` on an order that is already `Delivered`? It should fail.
      * *Example (`Order`):* Can a customer `Cancel()` an order that is already `ReadyForDelivery`? It should fail.
    * **Input-Based Rules:** Test calling the method with inputs that violate a rule.
      * *Example (`RestaurantAccount`):* Can you process a `PayoutSettlement` for an amount greater than the `CurrentBalance`? It should fail.
      * *Example (`Menu`):* Can you add a `MenuItem` with a negative `BasePrice`? The `MenuItem.Create` factory should fail.
    * **Consistency Rules:** Test operations that would break internal consistency.
      * *Example (`CustomizationGroup`):* Can you add a `CustomizationChoice` with a name that already exists in that group? It should fail.
  * **For all failure cases, verify:**
    * Does the method return a `Failure` result?
    * Does the returned `Error` object correctly identify the specific rule that was broken? (e.g., `Errors.Order.CannotAcceptDeliveredOrder`).
    * **Crucially, was the aggregate's state left unchanged?** A failed operation should never leave the aggregate in a partially modified, inconsistent state.
    * Was a domain event **NOT** raised?

#### 2. Testing Entities (e.g., `MenuItem`, `MenuCategory`, `AccountTransaction`)

Tests for entities are similar to aggregates but are often simpler. They are usually tested in the context of their parent aggregate.

**What to Test:**

* **Creation (Factory Methods):**
  * Does the `Create()` method correctly initialize all properties?
  * Does it fail if given invalid initial data (e.g., `MenuItem.Create()` with a negative price)?
* **Behavioral Methods:**
  * Test any public methods on the entity.
  * *Example (`MenuItem`):* After calling `menuItem.MarkAsUnavailable()`, is its `IsAvailable` property `false`? Does `MarkAsUnavailable()` on an already unavailable item do nothing?

#### 3. Testing Value Objects (e.g., `MenuId`, `Money`, `AppliedCustomization`)

Value Object tests are focused on two main areas: creation and equality.

**What to Test:**

* **Creation and Validation (Factory Methods):**
  * **Goal:** Verify that the VO can only be created with valid data.
  * **Cases to Cover:**
    * Test creating the VO with valid input. Does it succeed and hold the correct value?
    * Test creating the VO with invalid input. Does the factory method return a `Failure` result with the correct error?
      * *Example (`Money`):* Calling `Money.Create(-10)` should fail.
      * *Example (`MenuId`):* Calling `MenuId.Create("not-a-guid")` should fail.
* **Equality:**
  * **Goal:** Verify that the value-based equality works as expected.
  * **Cases to Cover:**
    * Two VOs created with the **same** values should be equal (`vo1.Equals(vo2)` and `vo1 == vo2` should be true).
    * Two VOs created with **different** values should **not** be equal.
    * A VO should not be equal to `null`.

#### 4. Testing Domain Events

Domain events are simple data carriers, so there is often little logic to test.

**What to Test:**

* **Creation:** Ensure the event's constructor correctly assigns the properties passed into it. This confirms that the event accurately captures the context of what happened.

---

### Example Test Scenarios for the `Order` Aggregate

To make this concrete, here are the test cases for just one method, `order.Accept()`:

* **Test Case 1: `Accept_ShouldSucceed_WhenOrderIsPlaced`**
  * **Arrange:** Create an `Order` with status `Placed`.
  * **Act:** Call `order.Accept()`.
  * **Assert:**
    * The result is `Success`.
    * The `order.Status` is now `Accepted`.
    * An `OrderAccepted` domain event was raised.

* **Test Case 2: `Accept_ShouldFail_WhenOrderIsAlreadyAccepted`**
  * **Arrange:** Create an `Order`, call `Accept()` on it.
  * **Act:** Call `order.Accept()` a second time.
  * **Assert:**
    * The result is `Failure`.
    * The error is `Errors.Order.AlreadyAccepted`.
    * The `order.Status` remains `Accepted`.
    * No new domain event was raised.

* **Test Case 3: `Accept_ShouldFail_WhenOrderIsDelivered`**
  * **Arrange:** Create an `Order` and move it to the `Delivered` state.
  * **Act:** Call `order.Accept()`.
  * **Assert:**
    * The result is `Failure`.
    * The error is `Errors.Order.CannotAcceptDeliveredOrder`.
    * The `order.Status` remains `Delivered`.
