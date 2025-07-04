## Rules for Writing Effective Aggregate Documentation

1. **Focus on the Public Contract:** The primary audience is a developer consuming the aggregate from an outer layer (e.g., the Application Service). They only care about the aggregate's public surface: its factory, public methods, public properties, and the events it raises. **Do not document private methods or internal implementation details.**

2. **Be Precise and Unambiguous:** Use the actual method signatures from the code (`Result AddMessage(...)`). List the specific `Error` types that can be returned (e.g., `SupportTicketErrors.InvalidStatusTransition`). This removes guesswork.

3. **Keep it Synchronized with Code:** This is the most critical rule. The documentation is only useful if it's accurate. The best practice is to **make documentation updates part of the Pull Request** that modifies the aggregate's public contract. If you add a new public method, you must add it to this document before the PR is approved.

4. **Explain the "Why," Not Just the "What":** Don't just list a method. Briefly explain its business purpose. For invariants, explain the business rule it's protecting (e.g., "Ensures a closed ticket cannot be modified").

5. **Use Tables for Scannability:** Structured tables for methods, events, and properties are much easier to scan and digest than long paragraphs of text.

6. **Link, Don't Duplicate:** The document is a condensed summary. Always provide a direct link to the aggregate's source file for developers who need to dive deeper.

7. **Version and Date It:** A `Last Updated` timestamp and `Version` number help developers quickly assess if the document is recent and relevant to the code they are looking at.

8. Be concise and compact but comprehensive.

---

## Aggregate Documentation Template

Here is a general, condensed format for documenting a DDD aggregate.

---

## Aggregate Documentation: `[AggregateName]`

* **Version:** 1.0
* **Last Updated:** YYYY-MM-DD
* **Source File:** `[Link to the aggregate's .cs file in the source repository]`

### 1. Overview

**Description:**
*A brief, one-paragraph summary of the aggregate's purpose in the business domain. This should be taken directly from the high-level domain design.*

**Core Responsibilities:**

* Manages the lifecycle of a [business concept].
* Acts as the transactional boundary for all [related operations].
* Enforces the business rules for [key invariant #1].
* Enforces the business rules for [key invariant #2].

### 2. Structure

* **Aggregate Root:** `[AggregateName]`
* **Key Child Entities:**
  * `[ChildEntityName]`: (Brief one-line description of its role).
  * *(List any others)*
* **Key Value Objects:**
  * `[ValueObjectName]`: (Brief one-line description, e.g., "Represents the monetary value and currency").
  * *(List any others that are important for understanding the public contract)*

### 3. Lifecycle & State Management

#### 3.1. Creation (Factory Method)

The only valid way to create a `[AggregateName]` is through its static factory method.

```csharp
public static Result<[AggregateName]> Create(...)
```

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `parameter1` | `string` | The primary name/subject for the aggregate. |
| `parameter2` | `IReadOnlyList<...>` | The initial collection of related items. |

**Validation Rules & Potential Errors:**

* `parameter1` cannot be null or empty. (Returns `[ErrorType.InvalidX]`)
* `parameter2` must contain at least one item. (Returns `[ErrorType.CollectionCannotBeEmpty]`)

#### 3.2. State Transitions & Commands (Public Methods)

These methods modify the state of the aggregate. All state changes must go through these methods.

| Method Signature | Description | Key Invariants Checked | Potential Errors |
| :--- | :--- | :--- | :--- |
| `Result AddItem(Item item)` | Adds a new item to the internal collection. | Checks if the maximum number of items has been reached. | `Errors.MaxItemsExceeded` |
| `Result UpdateStatus(Status newStatus)`| Transitions the aggregate to a new status. | Enforces the valid state machine (e.g., `Open` -> `InProgress`). Checks if the user has permission. | `Errors.InvalidStatusTransition`, `Errors.Unauthorized` |
| `Result AssignTo(Guid userId)` | Assigns ownership to a user. | Ensures the aggregate is in an "assignable" state. | `Errors.CannotAssignInCurrentState`|

### 4. Exposed State & Queries

#### 4.1. Public Properties

| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | `[AggregateName]Id` | The unique identifier of the aggregate. |
| `Status`| `[StatusEnum]` | The current status in its lifecycle. |
| `Items` | `IReadOnlyList<Item>` | A read-only view of the internal item collection. |

#### 4.2. Public Query Methods

These methods provide information about the aggregate's state without changing it.

| Method Signature | Description |
| :--- | :--- |
| `bool IsOverdue()` | Returns `true` if the aggregate's due date is in the past. |
| `bool NeedsAttention()` | Returns `true` if the aggregate meets specific business criteria for needing attention. |

### 5. Communication (Domain Events)

The aggregate raises the following domain events to communicate significant state changes to the rest of the system.

| Event Name | When It's Raised | Description |
| :--- | :--- | :--- |
| `[AggregateName]Created` | During the `Create` factory method. | Signals that a new aggregate has been successfully created. |
| `[AggregateName]StatusUpdated`| After a successful call to `UpdateStatus`. | Signals that the aggregate's status has changed. |
| `ItemAddedTo[AggregateName]`| After a successful call to `AddItem`. | Signals that a new item was added. |
