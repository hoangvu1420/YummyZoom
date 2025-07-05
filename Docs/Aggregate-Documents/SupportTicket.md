# Aggregate Documentation: `SupportTicket`

* **Version:** 1.0
* **Last Updated:** 2025-01-05
* **Source File:** `src/Domain/SupportTicketAggregate/SupportTicket.cs`

## 1. Overview

**Description:**
The `SupportTicket` aggregate represents a single, trackable case or issue raised by users, restaurants, or the system itself. It manages the complete lifecycle of support issues from submission to resolution, including message threading, status transitions, assignment management, and flexible context linking. The aggregate serves as the central coordination point for customer support operations across the platform.

**Core Responsibilities:**

* Manages the complete lifecycle of support tickets from creation through resolution.
* Acts as the transactional boundary for all ticket-related operations, messaging, and state changes.
* Enforces business rules for status transitions, admin authorization, and message immutability.
* Provides flexible context linking to connect tickets with related platform entities (Users, Orders, Restaurants, Reviews).

## 2. Structure

* **Aggregate Root:** `SupportTicket`
* **Key Child Entities:**
  * `TicketMessage`: Immutable messages within the ticket thread (both public and internal notes).
* **Key Value Objects:**
  * `SupportTicketId`: Unique identifier for the ticket.
  * `TicketNumber`: Human-readable ticket number (e.g., "TKT-2025-000001").
  * `ContextLink`: Links the ticket to related platform entities.
  * `MessageId`: Unique identifier for individual messages.
* **Key Enums:**
  * `SupportTicketStatus`: Ticket lifecycle states (Open, InProgress, PendingCustomerResponse, Resolved, Closed).
  * `SupportTicketType`: Categorizes the ticket (RefundRequest, AccountIssue, RestaurantReactivation, GeneralInquiry).
  * `SupportTicketPriority`: Priority levels (Low, Normal, High, Urgent).
  * `AuthorType`: Message author types (Customer, RestaurantOwner, Admin).
  * `ContextEntityType`: Types of entities that can be linked (User, Order, Restaurant, Review).

## 3. Lifecycle & State Management

### 3.1. Creation (Factory Method)

The only valid way to create a `SupportTicket` is through its static factory method.

```csharp
public static Result<SupportTicket> Create(
    string subject,
    SupportTicketType type,
    SupportTicketPriority priority,
    IReadOnlyList<ContextLink> contextLinks,
    string initialMessage,
    Guid authorId,
    AuthorType authorType,
    int ticketSequenceNumber)
```

| Parameter | Type | Description |
| :--- | :--- | :--- |
| `subject` | `string` | Brief description of the issue (max 200 characters). |
| `type` | `SupportTicketType` | Category of the support request. |
| `priority` | `SupportTicketPriority` | Initial priority level. |
| `contextLinks` | `IReadOnlyList<ContextLink>` | Related entities (at least one required). |
| `initialMessage` | `string` | The first message describing the issue (max 5000 characters). |
| `authorId` | `Guid` | ID of the user creating the ticket. |
| `authorType` | `AuthorType` | Type of author (Customer, RestaurantOwner, Admin). |
| `ticketSequenceNumber` | `int` | Sequential number for generating human-readable ticket number. |

**Validation Rules & Potential Errors:**

* `subject` cannot be empty or exceed 200 characters. (Returns `SupportTicketErrors.InvalidSubject`)
* `contextLinks` must contain at least one link. (Returns `SupportTicketErrors.NoContextLinksProvided`)
* `initialMessage` cannot be empty or exceed 5000 characters. (Returns `SupportTicketErrors.InvalidMessageText`)
* `authorId` cannot be empty. (Returns `SupportTicketErrors.InvalidAuthorId`)

### 3.2. State Transitions & Commands (Public Methods)

These methods modify the state of the aggregate. All state changes must go through these methods.

| Method Signature | Description | Key Invariants Checked | Potential Errors |
| :--- | :--- | :--- | :--- |
| `Result AddMessage(Guid authorId, AuthorType authorType, string messageText, bool isInternalNote = false)` | Adds a new message to the ticket thread. | Checks if ticket is closed, validates internal note permissions. | `SupportTicketErrors.TicketUpdateFailed`, `SupportTicketErrors.InvalidMessageText`, `SupportTicketErrors.InvalidAuthorId` |
| `Result UpdateStatus(SupportTicketStatus newStatus, Guid? adminId = null)` | Transitions the ticket to a new status. | Enforces valid status transitions and admin authorization for final states. | `SupportTicketErrors.InvalidStatusTransition`, `SupportTicketErrors.UnauthorizedStatusChange` |
| `Result AssignToAdmin(Guid adminId)` | Assigns the ticket to an admin. | Validates admin ID and checks if ticket can be assigned. | `SupportTicketErrors.InvalidAdminId` |
| `Result UnassignFromAdmin(Guid adminId)` | Removes admin assignment. | Checks current assignment state. | `SupportTicketErrors.InvalidAdminId`, `SupportTicketErrors.TicketUpdateFailed` |
| `Result UpdatePriority(SupportTicketPriority newPriority, Guid adminId)` | Changes the ticket priority (admin only). | Validates admin authorization. | `SupportTicketErrors.InvalidAdminId` |
| `Result UpdateSubject(string newSubject, Guid adminId)` | Updates the ticket subject (admin only). | Validates subject length and admin authorization. | `SupportTicketErrors.InvalidSubject`, `SupportTicketErrors.InvalidAdminId` |
| `Result AddContextLink(ContextLink contextLink, Guid adminId)` | Adds a new context link (admin only). | Checks for duplicate links and admin authorization. | `SupportTicketErrors.InvalidAdminId`, `SupportTicketErrors.TicketUpdateFailed` |
| `Result RemoveContextLink(ContextEntityType entityType, Guid entityId, Guid adminId)` | Removes a context link (admin only). | Ensures at least one link remains and admin authorization. | `SupportTicketErrors.InvalidAdminId`, `SupportTicketErrors.NoContextLinksProvided` |

## 4. Exposed State & Queries

### 4.1. Public Properties

| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | `SupportTicketId` | The unique identifier of the ticket. |
| `TicketNumber` | `TicketNumber` | Human-readable ticket number (e.g., "TKT-2025-000001"). |
| `Subject` | `string` | Brief description of the issue. |
| `Status` | `SupportTicketStatus` | Current status in the ticket lifecycle. |
| `Priority` | `SupportTicketPriority` | Current priority level. |
| `Type` | `SupportTicketType` | Category of the support request. |
| `SubmissionTimestamp` | `DateTimeOffset` | When the ticket was originally created. |
| `LastUpdateTimestamp` | `DateTimeOffset` | When the ticket was last modified. |
| `AssignedToAdminId` | `Guid?` | ID of the admin currently assigned (null if unassigned). |
| `ContextLinks` | `IReadOnlyList<ContextLink>` | Related platform entities. |
| `Messages` | `IReadOnlyList<TicketMessage>` | All messages in the ticket thread. |

### 4.2. Public Query Methods

These methods provide information about the aggregate's state without changing it.

| Method Signature | Description |
| :--- | :--- |
| `bool IsAssignedToAdmin(Guid adminId)` | Returns `true` if the ticket is assigned to the specified admin. |
| `bool IsOpen()` | Returns `true` if the ticket status is Open. |
| `bool IsClosed()` | Returns `true` if the ticket status is Closed. |
| `bool IsResolved()` | Returns `true` if the ticket status is Resolved. |
| `bool RequiresCustomerResponse()` | Returns `true` if the ticket is waiting for customer response. |
| `bool IsHighPriority()` | Returns `true` if the priority is High or Urgent. |
| `bool HasContextLinkForEntity(ContextEntityType entityType, Guid entityId)` | Returns `true` if the ticket is linked to the specified entity. |
| `IReadOnlyList<TicketMessage> GetPublicMessages()` | Returns only non-internal messages visible to customers. |
| `IReadOnlyList<TicketMessage> GetInternalNotes()` | Returns only internal admin notes. |
| `TicketMessage? GetLatestMessage()` | Returns the most recent message in the thread. |
| `int GetMessageCount()` | Returns the total number of messages. |
| `TimeSpan GetAge()` | Returns how long the ticket has existed. |
| `TimeSpan GetTimeSinceLastUpdate()` | Returns time since the last update. |
| `bool ShouldAutoEscalate(TimeSpan maxAgeForPriority)` | Business rule: determines if ticket should be auto-escalated. |
| `bool IsStale(TimeSpan maxTimeSinceLastUpdate)` | Business rule: determines if ticket needs attention due to inactivity. |
| `bool NeedsAdminAttention()` | Business rule: determines if ticket needs admin assignment. |
| `bool CanCustomerRespond()` | Business rule: determines if customer can add messages. |
| `bool IsEscalationCandidate()` | Business rule: determines if ticket qualifies for priority escalation. |

## 5. Communication (Domain Events)

The aggregate raises the following domain events to communicate significant state changes to the rest of the system.

| Event Name | When It's Raised | Description |
| :--- | :--- | :--- |
| `SupportTicketCreated` | During the `Create` factory method. | Signals that a new support ticket has been created with initial details and context links. |
| `TicketMessageAdded` | After successful calls to `AddMessage` and during creation. | Signals that a new message has been added to the ticket thread. |
| `SupportTicketStatusChanged` | After a successful call to `UpdateStatus`. | Signals that the ticket status has changed, including previous and new status. |
| `SupportTicketAssigned` | After successful calls to `AssignToAdmin` or `UnassignFromAdmin`. | Signals changes in admin assignment, including previous assignment details. |
| `SupportTicketPriorityChanged` | After a successful call to `UpdatePriority`. | Signals that the ticket priority has been updated by an admin. |

## 6. Key Child Entities

### 6.1. TicketMessage

Represents an immutable message within the ticket thread.

**Properties:**

* `Id` (MessageId): Unique identifier
* `AuthorId` (Guid): ID of the message author
* `AuthorType` (AuthorType): Type of author (Customer, RestaurantOwner, Admin)
* `MessageText` (string): Content of the message (max 5000 characters)
* `Timestamp` (DateTimeOffset): When the message was created
* `IsInternalNote` (bool): Whether the message is an admin-only internal note

**Business Rules:**

* Messages are immutable once created - no update methods exist
* Internal notes can only be created by admins
* Message text cannot be empty or exceed 5000 characters

## 7. Key Value Objects

### 7.1. ContextLink

Links the ticket to related platform entities for context.

**Factory Methods:**

* `ContextLink.Create(ContextEntityType entityType, Guid entityId)` - Creates a context link
* `ContextLink.Create(ContextEntityType entityType, string entityId)` - Creates with string ID parsing

**Properties:**

* `EntityType` (ContextEntityType): Type of linked entity (User, Order, Restaurant, Review)
* `EntityID` (Guid): ID of the linked entity

### 7.2. TicketNumber

Human-readable ticket identifier.

**Factory Methods:**

* `TicketNumber.Create(string value)` - Creates from explicit value
* `TicketNumber.CreateFromSequence(int sequenceNumber)` - Auto-generates (e.g., "TKT-2025-000001")

## 8. Business Rules & Invariants

### 8.1. Context Links

* Every ticket must have at least one context link to be meaningful.
* Context links can be added/removed by admins, but at least one must remain.
* Each context link uniquely identifies a related platform entity.

### 8.2. Status Transitions

* Valid transitions are enforced by business logic:
  * Open → InProgress, Closed
  * InProgress → PendingCustomerResponse, Resolved, Closed
  * PendingCustomerResponse → InProgress, Closed
  * Resolved → Closed, InProgress (reopening)
  * Closed → (no transitions allowed)

### 8.3. Authorization Rules

* Only admins can change status to Resolved or Closed.
* Only admins can update priority, subject, and manage context links.
* Only admins can create internal notes.
* Auto-assignment occurs when moving to InProgress status.

### 8.4. Message Management

* Messages are immutable once added to ensure audit trail integrity.
* Customer responses automatically move tickets from PendingCustomerResponse to InProgress.
* Internal notes are only visible to admins.

### 8.5. Priority Escalation

* Priority changes must follow escalation rules (can increase but not decrease without justification).
* Auto-escalation candidates are identified based on age, type, and current priority.

## 9. External Dependencies

The aggregate references other aggregates by ID only through ContextLinks:

* `User` entities - For customer/restaurant owner context
* `Order` entities - For order-related issues
* `Restaurant` entities - For restaurant-specific issues  
* `Review` entities - For review-related disputes

**Note:** The Application Service is responsible for validating the existence and accessibility of linked entities based on the user's permissions and the context of the support request.
