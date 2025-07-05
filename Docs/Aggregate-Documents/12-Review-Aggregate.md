# Aggregate Documentation: `Review`

* **Version:** 1.0
* **Last Updated:** 2025-01-05
* **Source File:** `src/Domain/ReviewAggregate/Review.cs`

## 1. Overview

**Description:**
The `Review` aggregate captures authentic customer feedback and ratings for restaurants, anchored to completed orders. It manages the complete lifecycle of customer reviews from submission through moderation, visibility control, and restaurant responses. The aggregate ensures review authenticity by requiring a valid order ID and enforces business rules around rating validity and reply management.

**Core Responsibilities:**

* Manages the complete lifecycle of customer reviews from creation to deletion.
* Acts as the transactional boundary for all review-related operations and state changes.
* Enforces business rules for rating validity (1-5 scale) and review authenticity.
* Controls review visibility and moderation status for content management.

## 2. Structure

* **Aggregate Root:** `Review`
* **Key Value Objects:**
  * `ReviewId`: Unique identifier for the review.
  * `Rating`: Validated rating value (1-5 scale).
  * `OrderId`: Links the review to the order it's based on (ensures authenticity).
  * `UserId`: References the customer who wrote the review.
  * `RestaurantId`: References the restaurant being reviewed.

## 3. Lifecycle & State Management

### 3.1. Creation (Factory Method)

The only valid way to create a `Review` is through its static factory method.

```csharp
public static Result<Review> Create(
    OrderId orderId,
    UserId customerId,
    RestaurantId restaurantId,
    Rating rating,
    string? comment = null)
```

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `orderId` | `OrderId` | The completed order this review is based on (ensures authenticity). |
| `customerId` | `UserId` | The customer writing the review. |
| `restaurantId` | `RestaurantId` | The restaurant being reviewed. |
| `rating` | `Rating` | The rating value (1-5 scale, validated by Rating value object). |
| `comment` | `string?` | Optional text comment from the customer. |

**Validation Rules & Potential Errors:**

* `orderId` cannot be null. (Returns `ReviewErrors.InvalidOrderId`)
* `customerId` cannot be null. (Returns `ReviewErrors.InvalidCustomerId`)
* `restaurantId` cannot be null. (Returns `ReviewErrors.InvalidRestaurantId`)
* `rating` must be between 1 and 5 (enforced by Rating value object). (Returns `ReviewErrors.InvalidRating`)

### 3.2. State Transitions & Commands (Public Methods)

These methods modify the state of the aggregate. All state changes must go through these methods.

| Method Signature | Description | Key Invariants Checked | Potential Errors |
| :--- | :--- | :--- | :--- |
| `Result MarkAsModerated()` | Marks the review as having been moderated by an admin. | None (idempotent operation). | None |
| `Result Hide()` | Hides the review from public view. | None (idempotent operation). | None |
| `Result Show()` | Shows the review in public view. | None (idempotent operation). | None |
| `Result AddReply(string reply)` | Adds a restaurant's reply to the review. | Reply cannot be empty, and only one reply is allowed. | `ReviewErrors.EmptyReply`, `ReviewErrors.ReviewAlreadyReplied` |
| `Result MarkAsDeleted()` | Marks the review as deleted (soft delete). | None. | None |

## 4. Exposed State & Queries

### 4.1. Public Properties

| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | `ReviewId` | The unique identifier of the review. |
| `OrderId` | `OrderId` | The order this review is based on (ensures authenticity). |
| `CustomerId` | `UserId` | The customer who wrote the review. |
| `RestaurantId` | `RestaurantId` | The restaurant being reviewed. |
| `Rating` | `Rating` | The validated rating value (1-5 scale). |
| `Comment` | `string?` | Optional text comment from the customer. |
| `SubmissionTimestamp` | `DateTime` | When the review was originally submitted. |
| `IsModerated` | `bool` | Whether the review has been moderated by an admin. |
| `IsHidden` | `bool` | Whether the review is hidden from public view. |
| `Reply` | `string?` | Optional reply from the restaurant. |

### 4.2. Public Query Methods

The Review aggregate has no additional query methods beyond accessing its properties.

## 5. Communication (Domain Events)

The aggregate raises the following domain events to communicate significant state changes to the rest of the system.

| Event Name | When It's Raised | Description |
| :--- | :--- | :--- |
| `ReviewCreated` | During the `Create` factory method. | Signals that a new review has been successfully created with all key details. |
| `ReviewModerated` | After a successful call to `MarkAsModerated`. | Signals that the review has been marked as moderated by an admin. |
| `ReviewHidden` | After a successful call to `Hide`. | Signals that the review has been hidden from public view. |
| `ReviewShown` | After a successful call to `Show`. | Signals that the review has been made visible to the public. |
| `ReviewReplied` | After a successful call to `AddReply`. | Signals that the restaurant has replied to the review. |
| `ReviewDeleted` | After a successful call to `MarkAsDeleted`. | Signals that the review has been marked for deletion. |

## 6. Key Value Objects

### 6.1. Rating

Represents a validated customer rating on a 1-5 scale.

**Factory Method:**

* `Rating.Create(int value)` - Creates a rating value with validation

**Validation Rules:**

* Value must be between 1 and 5 (inclusive)
* Returns `ReviewErrors.InvalidRating` for invalid values

## 7. Business Rules & Invariants

### 7.1. Review Authenticity

* Reviews must be anchored to a valid `OrderId` to ensure authenticity.
* Only customers who have placed orders can write reviews for those specific restaurants.
* The combination of `OrderId`, `CustomerId`, and `RestaurantId` provides a strong authenticity guarantee.

### 7.2. Rating Validation

* Ratings must be on a strict 1-5 scale with no exceptions.
* The Rating value object enforces this constraint at creation time.

### 7.3. Reply Management

* Reviews can have at most one reply from the restaurant.
* Replies cannot be empty or whitespace-only.
* Once a reply is added, it cannot be modified (immutable).

### 7.4. Visibility and Moderation

* Reviews can be hidden or shown independently of their moderation status.
* Moderation is a one-way operation (cannot be "unmoderated").
* All state changes are idempotent to prevent duplicate events.

## 8. External Dependencies

The aggregate references other aggregates by ID only:

* `OrderId` - Links to the Order aggregate that this review is based on
* `UserId` - Links to the User aggregate (customer who wrote the review)
* `RestaurantId` - Links to the Restaurant aggregate being reviewed

**Note:** The Application Service is responsible for validating that the order actually belongs to the customer and that the order is in a completed state before allowing review creation.
