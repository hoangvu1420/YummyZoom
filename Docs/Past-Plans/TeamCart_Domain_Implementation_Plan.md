# Team Cart Domain Implementation Plan

## Overview

This document outlines the domain layer implementation plan for the Team Cart feature in YummyZoom. The Team Cart enables collaborative ordering where a host creates a shared cart, invites guests via a link, and manages mixed payment methods (online + Cash on Delivery) before converting to a final Order.

## Architecture Alignment

The implementation follows YummyZoom's established Clean Architecture and DDD patterns:
- **Domain-First Approach**: Core business logic encapsulated in aggregates
- **Event-Driven Design**: Domain events drive real-time collaboration
- **Result Pattern**: Consistent error handling across all operations
- **Immutable Value Objects**: Type-safe identifiers and data structures
- **Factory Methods**: Controlled object creation with validation

## 1. New Aggregate: TeamCartAggregate

### Directory Structure
```
src/Domain/TeamCartAggregate/
├── TeamCart.cs                    (Aggregate Root)
├── Entities/
│   ├── TeamCartMember.cs
│   ├── TeamCartItem.cs
│   └── MemberPayment.cs
├── Enums/
│   ├── TeamCartStatus.cs
│   ├── MemberRole.cs
│   ├── PaymentStatus.cs
│   └── PaymentMethod.cs
├── ValueObjects/
│   ├── TeamCartId.cs
│   ├── ShareableLinkToken.cs
│   ├── TeamCartMemberId.cs
│   ├── TeamCartItemId.cs
│   ├── MemberPaymentId.cs
│   └── TeamCartItemCustomization.cs
├── Events/
│   ├── TeamCartCreated.cs
│   ├── MemberJoined.cs
│   ├── ItemAddedToTeamCart.cs
│   ├── MemberCommittedToPayment.cs
│   ├── TeamCartReadyForConfirmation.cs
│   ├── TeamCartConverted.cs
│   └── TeamCartExpired.cs
└── Errors/
    └── TeamCartErrors.cs
```

## 2. TeamCart Aggregate Root Design

### Core Properties
```csharp
public sealed class TeamCart : AggregateRoot<TeamCartId, Guid>, ICreationAuditable
{
    private readonly List<TeamCartMember> _members = [];
    private readonly List<TeamCartItem> _items = [];
    private readonly List<MemberPayment> _memberPayments = [];

    public RestaurantId RestaurantId { get; private set; }
    public UserId HostUserId { get; private set; }
    public TeamCartStatus Status { get; private set; }
    public ShareableLinkToken ShareToken { get; private set; }
    public DateTime? Deadline { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    
    // Read-only collections
    public IReadOnlyList<TeamCartMember> Members => _members.AsReadOnly();
    public IReadOnlyList<TeamCartItem> Items => _items.AsReadOnly();
    public IReadOnlyList<MemberPayment> MemberPayments => _memberPayments.AsReadOnly();
}
```

### Status Flow
```
Open → AwaitingPayments → ReadyToConfirm → Converted
  ↓           ↓               ↓
Expired    Expired         Expired
```

### Key Methods
- `Create(UserId hostId, RestaurantId restaurantId)` - Static factory
- `AddMember(UserId userId, string name)` - Add participant
- `AddItem(UserId userId, MenuItemId menuItemId, int quantity, List<TeamCartItemCustomization> customizations)` - Add item
- `SetDeadline(DateTime deadline)` - Host sets deadline
- `CommitToPayment(UserId userId, PaymentMethod method)` - Payment commitment
- `RecordOnlinePaymentSuccess(UserId userId, string transactionId)` - Track payments
- `ConvertToOrder()` - Generate Order creation data

## 3. Child Entities

### TeamCartMember
```csharp
public sealed class TeamCartMember : Entity<TeamCartMemberId>
{
    public UserId UserId { get; private set; }
    public string Name { get; private set; }
    public MemberRole Role { get; private set; }
    public PaymentStatus PaymentStatus { get; private set; }
}
```

### TeamCartItem
```csharp
public sealed class TeamCartItem : Entity<TeamCartItemId>
{
    private readonly List<TeamCartItemCustomization> _selectedCustomizations = [];
    
    public UserId AddedByUserId { get; private set; }
    public MenuItemId Snapshot_MenuItemId { get; private set; }
    public MenuCategoryId Snapshot_MenuCategoryId { get; private set; }
    public string Snapshot_ItemName { get; private set; }
    public Money Snapshot_BasePriceAtOrder { get; private set; }
    public int Quantity { get; private set; }
    public Money LineItemTotal { get; private set; }
    
    public IReadOnlyList<TeamCartItemCustomization> SelectedCustomizations => _selectedCustomizations.AsReadOnly();
}
```

### MemberPayment
```csharp
public sealed class MemberPayment : Entity<MemberPaymentId>
{
    public UserId UserId { get; private set; }
    public Money Amount { get; private set; }
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string? OnlineTransactionId { get; private set; }
}
```

## 4. Enums

### TeamCartStatus
```csharp
public enum TeamCartStatus
{
    Open,                    // Members can join and add items
    AwaitingPayments,       // Checkout initiated, waiting for payments
    ReadyToConfirm,         // All payments complete/committed
    Converted,              // Successfully converted to Order
    Expired                 // Timed out or manually expired
}
```

### MemberRole
```csharp
public enum MemberRole
{
    Host,                   // Creator and manager of the cart
    Guest                   // Invited participant
}
```

### PaymentMethod
```csharp
public enum PaymentMethod
{
    Online,                 // Credit card, e-wallet, etc.
    CashOnDelivery         // COD commitment
}
```

### PaymentStatus
```csharp
public enum PaymentStatus
{
    Pending,               // No payment commitment yet
    CommittedToCOD,        // Committed to pay cash on delivery
    PaidOnline,            // Successfully paid online
    Failed                 // Online payment failed
}
```

## 5. Value Objects

### TeamCartId
```csharp
public sealed class TeamCartId : AggregateRootId<Guid>
{
    public override Guid Value { get; protected set; }
    
    public static TeamCartId CreateUnique() => new(Guid.NewGuid());
    public static TeamCartId Create(Guid value) => new(value);
    public static Result<TeamCartId> Create(string value) => // Parse and validate
}
```

### ShareableLinkToken
```csharp
public sealed class ShareableLinkToken : ValueObject
{
    public string Value { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    
    public static ShareableLinkToken CreateUnique(TimeSpan validFor)
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}
```

## 6. Customization Integration

The TeamCart will leverage the existing CustomizationGroup aggregate for item customizations:

### TeamCartItemCustomization Value Object
```csharp
public sealed class TeamCartItemCustomization : ValueObject
{
    public string Snapshot_CustomizationGroupName { get; private set; }
    public string Snapshot_ChoiceName { get; private set; }
    public Money Snapshot_ChoicePriceAdjustmentAtOrder { get; private set; }

    private TeamCartItemCustomization(
        string snapshotCustomizationGroupName,
        string snapshotChoiceName,
        Money snapshotChoicePriceAdjustmentAtOrder)
    {
        Snapshot_CustomizationGroupName = snapshotCustomizationGroupName;
        Snapshot_ChoiceName = snapshotChoiceName;
        Snapshot_ChoicePriceAdjustmentAtOrder = snapshotChoicePriceAdjustmentAtOrder;
    }

    public static Result<TeamCartItemCustomization> Create(
        string snapshotCustomizationGroupName,
        string snapshotChoiceName,
        Money snapshotChoicePriceAdjustmentAtOrder)
    {
        if (string.IsNullOrWhiteSpace(snapshotCustomizationGroupName) ||
            string.IsNullOrWhiteSpace(snapshotChoiceName))
        {
            return Result.Failure<TeamCartItemCustomization>(TeamCartErrors.InvalidCustomization);
        }

        return new TeamCartItemCustomization(
            snapshotCustomizationGroupName,
            snapshotChoiceName,
            snapshotChoicePriceAdjustmentAtOrder);
    }

    // Conversion method for Order creation
    public OrderItemCustomization ToOrderItemCustomization()
    {
        return OrderItemCustomization.Create(
            Snapshot_CustomizationGroupName,
            Snapshot_ChoiceName,
            Snapshot_ChoicePriceAdjustmentAtOrder).Value;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Snapshot_CustomizationGroupName;
        yield return Snapshot_ChoiceName;
        yield return Snapshot_ChoicePriceAdjustmentAtOrder;
    }
}
```

### TeamCartItem Customization Support
- Create dedicated `TeamCartItemCustomization` value object for TeamCart aggregate
- Snapshot customization data at time of adding to cart
- Maintain price consistency through snapshots
- Support full customization workflow from existing system
- Clean conversion to `OrderItemCustomization` during Order creation

### Integration Points
- `TeamCartItem` contains `List<TeamCartItemCustomization>`
- Customizations validated against `CustomizationGroup` rules during item addition
- Price adjustments calculated and snapshotted at TeamCart level
- Seamless conversion to `OrderItemCustomization` during Order creation via `ToOrderItemCustomization()` method

## 7. Business Rules & Invariants

### TeamCart Invariants
- Must have exactly one Host (cannot be removed)
- Items can only be added in `Open` status
- Status transitions must follow defined flow
- Conversion only allowed in `ReadyToConfirm` status
- All online payments must succeed before confirmation
- ExpiresAt must be after CreatedAt

### Payment Rules
- Mixed payment methods allowed within single cart
- Host acts as guarantor for all COD payments
- Online payments must complete before order placement
- Total cash amount calculated for delivery driver
- Each member can only have one payment commitment

### Member Management Rules
- Host automatically added upon creation
- Guest names must be provided (for display purposes)
- No limit on number of participants (as per requirements)
- Members cannot be removed once added (audit trail)

## 8. Domain Events

### Core Events
```csharp
public record TeamCartCreated(TeamCartId TeamCartId, UserId HostId, RestaurantId RestaurantId);
public record MemberJoined(TeamCartId TeamCartId, UserId UserId, string Name);
public record ItemAddedToTeamCart(TeamCartId TeamCartId, TeamCartItemId ItemId, UserId AddedBy, MenuItemId MenuItemId);
public record MemberCommittedToPayment(TeamCartId TeamCartId, UserId UserId, PaymentMethod Method, Money Amount);
public record TeamCartReadyForConfirmation(TeamCartId TeamCartId, Money TotalAmount, Money CashAmount);
public record TeamCartConverted(TeamCartId TeamCartId, OrderId OrderId);
public record TeamCartExpired(TeamCartId TeamCartId);
```

### Event Usage
- Drive real-time UI updates via read model synchronization
- Trigger notification workflows
- Enable audit trail and analytics
- Support integration with external systems

## 9. Order Integration

### Modifications to Order Aggregate
```csharp
public sealed class Order : AggregateRoot<OrderId, Guid>
{
    // New property for audit trail
    public TeamCartId? SourceTeamCartId { get; private set; }
    
    // Existing PaymentTransaction entity gets new property
    // (in PaymentTransaction.cs)
    public UserId? PaidByUserId { get; private set; }
}
```

### Conversion Process
1. `TeamCart.ConvertToOrder()` validates state and creates conversion data
2. Application layer receives structured data for Order creation
3. New static factory: `Order.CreateFromTeamCart(teamCartData)`
4. Order created with proper payment transaction structure:
   - Individual online payments with `PaidByUserId`
   - Single COD transaction for total cash amount (Host as guarantor)

### Conversion Data Structure
```csharp
public sealed class TeamCartOrderData
{
    public UserId CustomerId { get; init; }           // Host becomes customer
    public RestaurantId RestaurantId { get; init; }
    public List<OrderItem> OrderItems { get; init; }
    public List<PaymentTransactionData> PaymentTransactions { get; init; }
    public Money TotalAmount { get; init; }
    public TeamCartId SourceTeamCartId { get; init; }
    // ... other Order properties
}
```

## 10. Error Handling

### TeamCartErrors
```csharp
public static class TeamCartErrors
{
    public static readonly Error TeamCartNotFound = Error.NotFound("TeamCart.NotFound", "Team cart not found");
    public static readonly Error InvalidTokenForJoining = Error.Validation("TeamCart.InvalidToken", "Invalid or expired token");
    public static readonly Error CannotAddItemsToClosedCart = Error.Validation("TeamCart.ClosedCart", "Cannot add items to closed cart");
    public static readonly Error PaymentNotCompleted = Error.Validation("TeamCart.PaymentIncomplete", "Payment not completed");
    public static readonly Error InvalidStatusForConversion = Error.Validation("TeamCart.InvalidStatus", "Invalid status for conversion");
    public static readonly Error TeamCartExpired = Error.Validation("TeamCart.Expired", "Team cart has expired");
    public static readonly Error HostCannotLeave = Error.Validation("TeamCart.HostCannotLeave", "Host cannot leave the cart");
    public static readonly Error MemberAlreadyExists = Error.Validation("TeamCart.MemberExists", "Member already in cart");
    public static readonly Error DeadlineInPast = Error.Validation("TeamCart.DeadlineInPast", "Deadline cannot be in the past");
}
```

## 11. Testing Strategy

### Unit Test Structure
```
tests/Domain.UnitTests/TeamCartAggregate/
├── TeamCartCreationTests.cs
├── TeamCartMemberManagementTests.cs
├── TeamCartItemManagementTests.cs
├── TeamCartPaymentWorkflowTests.cs
├── TeamCartStatusTransitionTests.cs
├── TeamCartConversionTests.cs
├── TeamCartBusinessRulesTests.cs
├── TeamCartCustomizationTests.cs
└── Entities/
    ├── TeamCartMemberTests.cs
    ├── TeamCartItemTests.cs
    └── MemberPaymentTests.cs
└── ValueObjects/
    ├── TeamCartIdTests.cs
    └── ShareableLinkTokenTests.cs
```

### Test Coverage Areas
- **Creation & Validation**: Factory method scenarios, validation rules
- **Member Management**: Adding members, role assignments, duplicate handling
- **Item Management**: Adding items with customizations, quantity validation
- **Payment Workflow**: Online payments, COD commitments, mixed scenarios
- **Status Transitions**: Valid/invalid transitions, business rule enforcement
- **Conversion Logic**: Order data generation, payment transaction creation
- **Error Scenarios**: All error conditions and edge cases
- **Domain Events**: Event raising and content validation

### Test Helpers
```csharp
public static class TeamCartTestHelpers
{
    public static TeamCart CreateValidTeamCart(UserId? hostId = null, RestaurantId? restaurantId = null)
    public static TeamCartMember CreateValidMember(UserId? userId = null, MemberRole role = MemberRole.Guest)
    public static TeamCartItem CreateValidItem(UserId? addedBy = null, MenuItemId? menuItemId = null)
    public static List<TeamCartItemCustomization> CreateValidCustomizations()
}
```

## 12. Implementation Phases

### Phase 1: Core Aggregate Foundation
**Scope**: Basic TeamCart structure and member management
- [x] Create TeamCart aggregate root with core properties
- [x] Implement TeamCartMember entity
- [x] Add basic factory methods and validation
- [x] Create core value objects (TeamCartId, ShareableLinkToken)
- [x] Implement member addition/management
- [x] Write foundational unit tests

**Deliverables**: 
- Working TeamCart creation and member management
- Core domain events (TeamCartCreated, MemberJoined)
- Basic error handling and validation

### Phase 2: Item Management & Customizations
- [x] Implement TeamCartItemCustomization Value Object
- [x] Implement TeamCartItem entity
- [x] Add item management methods to TeamCart
- [x] Implement price calculation with customizations
- [x] Create item-related domain events
- [x] Write comprehensive item management tests

**Deliverables**:
- Full item addition workflow with customizations
- Price calculation and snapshotting
- ItemAddedToTeamCart domain event

### Phase 3: Payment Workflow
**Scope**: Mixed payment method handling
- [x] Implement MemberPayment entity
- [x] Add payment commitment methods
- [x] Handle online payment tracking
- [x] Implement payment status management
- [x] Create payment-related domain events
- [x] Write payment workflow tests

**Deliverables**:
- Complete payment commitment workflow
- Online payment success tracking
- Mixed payment method support

### Phase 4: Order Integration & Conversion
**Scope**: TeamCart to Order conversion
- [x] Modify Order aggregate for TeamCart support
- [x] Implement conversion logic in TeamCart
- [x] Create Order factory method for TeamCart data
- [x] Handle payment transaction creation
- [x] Write integration tests
- [x] End-to-end conversion testing

**Deliverables**:
- Working TeamCart to Order conversion
- Proper payment transaction structure
- Complete audit trail

## 13. Key Design Benefits

### Domain Purity
- Clean separation between collaborative cart and immutable order
- No pollution of Order aggregate with temporary collaboration logic
- Maintains existing Order behavior and contracts

### Event-Driven Architecture
- Real-time collaboration through domain events
- Decoupled notification and UI update mechanisms
- Extensible for future features (analytics, recommendations)

### Payment Flexibility
- Elegant handling of mixed payment methods
- Clear separation of online vs COD responsibilities
- Host as guarantor model for cash payments

### Type Safety
- Strong typing through value objects
- Compile-time validation of business rules
- Clear aggregate boundaries and relationships

### Testability
- Clear separation of concerns enables focused testing
- Rich domain model supports comprehensive unit testing
- Event-driven design facilitates integration testing

## 14. Future Extensibility

The design supports planned Phase 2 enhancements:

### Recurring Team Carts ("Lunch Train")
- Add `RecurrencePattern` value object
- Extend TeamCart with scheduling properties
- New domain events for recurring cart creation

### Smart Coupon Suggestions
- Integration points with CouponAggregate
- Real-time cart analysis for coupon eligibility
- Event-driven coupon recommendation workflow

### Post-Delivery Features
- Extend MemberPayment for post-delivery tracking
- New domain events for payment reminders
- Integration with notification systems

## 15. Non-Functional Considerations

### Performance
- Aggregate size kept reasonable (typical team size 5-15 people)
- Event-driven read model updates for real-time UI
- Efficient conversion process to Order

### Scalability
- Stateless domain logic enables horizontal scaling
- Event sourcing potential for audit and replay
- Clear aggregate boundaries prevent cross-aggregate transactions

### Security
- ShareableLinkToken with expiration for access control
- Host-only operations clearly defined
- Payment data properly encapsulated

This implementation plan provides a comprehensive foundation for the Team Cart feature while maintaining alignment with YummyZoom's established architecture and patterns. The phased approach enables incremental development and testing, ensuring quality and maintainability throughout the implementation process.