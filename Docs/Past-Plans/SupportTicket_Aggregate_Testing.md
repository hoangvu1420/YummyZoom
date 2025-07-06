We are creating comprehensive, efficient and high-coverage unit tests for the src\Domain\SupportTicketAggregate. I'll repeat the plan for you:

## SupportTicket Aggregate Testing Structure

### **Test Folder Structure**
```
tests/Domain.UnitTests/SupportTicketAggregate/
├── SupportTicketCoreTests.cs              # Core aggregate functionality
├── SupportTicketMessagingTests.cs         # Message-related operations  
├── SupportTicketStatusTests.cs            # Status transitions and validation
├── SupportTicketAssignmentTests.cs        # Admin assignment operations
├── SupportTicketBusinessRulesTests.cs     # Business logic and query methods
├── SupportTicketContextLinkTests.cs       # Context link management
├── SupportTicketValidationTests.cs        # Validation rules and edge cases
├── Entities/
│   └── TicketMessageTests.cs             # TicketMessage entity tests
└── ValueObjects/
    ├── ContextLinkTests.cs                # ContextLink value object tests
    └── TicketNumberTests.cs               # TicketNumber value object tests
```

### **Test Coverage Strategy**

#### **1. SupportTicketCoreTests.cs**
**Responsibilities:** Basic aggregate lifecycle and creation
- ✅ **Factory Methods**
  - `Create()` with valid inputs (both overloads)
  - `Create()` with invalid subject (empty, too long)
  - `Create()` with no context links
  - `Create()` with invalid initial message
  - Domain events raised on creation

#### **2. SupportTicketMessagingTests.cs**
**Responsibilities:** All message-related operations
- ✅ **AddMessage() Method**
  - Add public message successfully
  - Add internal note (admin only)
  - Prevent internal notes from non-admins
  - Prevent messages on closed tickets
  - Auto-status update when customer responds
  - Domain events for message addition
- ✅ **Message Query Methods**
  - `GetPublicMessages()` filtering
  - `GetInternalNotes()` filtering
  - `GetLatestMessage()` ordering
  - `GetMessageCount()` accuracy

#### **3. SupportTicketStatusTests.cs**
**Responsibilities:** Status transitions and business rules
- ✅ **UpdateStatus() Method**
  - Valid status transitions for each status
  - Invalid status transitions blocked
  - Admin authorization for Resolved/Closed
  - Prevent duplicate status changes
  - Auto-assignment on InProgress
  - Domain events for status changes
- ✅ **Status Query Methods**
  - `IsOpen()`, `IsClosed()`, `IsResolved()`
  - `RequiresCustomerResponse()`, `IsInFinalState()`
  - `CanChangeStatus()`, `CanCustomerRespond()`

#### **4. SupportTicketAssignmentTests.cs**
**Responsibilities:** Admin assignment and management
- ✅ **AssignToAdmin() Method**
  - Successful assignment
  - Invalid admin ID validation
  - Domain events for assignment
- ✅ **UnassignFromAdmin() Method**
  - Successful unassignment
  - Prevent unassignment when not assigned
- ✅ **Assignment Query Methods**
  - `IsAssignedToAdmin()` accuracy
  - `CanBeAssignedToAdmin()` business rules
  - `NeedsAdminAttention()` logic

#### **5. SupportTicketBusinessRulesTests.cs**
**Responsibilities:** Complex business logic and query methods
- ✅ **Priority Management**
  - `UpdatePriority()` with admin validation
  - `IsValidPriorityEscalation()` logic
  - `IsHighPriority()` detection
  - `IsEscalationCandidate()` rules
- ✅ **Time-based Operations**
  - `GetAge()` calculation
  - `GetTimeSinceLastUpdate()` calculation
  - `ShouldAutoEscalate()` business rules
  - `IsStale()` detection logic
- ✅ **Subject Management**
  - `UpdateSubject()` with validation
  - Subject length validation

#### **6. SupportTicketContextLinkTests.cs**
**Responsibilities:** Context link management
- ✅ **AddContextLink() Method**
  - Add new context link successfully
  - Prevent duplicate context links
  - Admin authorization validation
- ✅ **RemoveContextLink() Method**
  - Remove existing context link
  - Prevent removal of last context link
  - Handle non-existent context links
- ✅ **Context Query Methods**
  - `HasContextLinkForEntity()` accuracy
  - `IsRelatedToEntity()` logic

#### **7. SupportTicketValidationTests.cs**
**Responsibilities:** Edge cases and validation rules
- ✅ **Static Validation Methods**
  - `IsValidSubjectLength()` rules
  - `IsValidMessageLength()` rules
  - `IsValidPriorityEscalation()` logic
- ✅ **Edge Cases**
  - Boundary value testing
  - Null/empty input handling
  - Concurrent operation scenarios
- ✅ **Error Scenarios**
  - Comprehensive error message validation
  - Error type verification

#### **8. Entities/TicketMessageTests.cs**
**Responsibilities:** TicketMessage entity validation
- ✅ **Factory Methods**
  - `Create()` with valid inputs (both overloads)
  - Author ID validation
  - Message text validation (empty, too long)
  - Timestamp handling
- ✅ **Immutability**
  - Verify no update methods exist
  - Property encapsulation

#### **9. ValueObjects/ContextLinkTests.cs**
**Responsibilities:** ContextLink value object validation
- ✅ **Factory Methods**
  - `Create()` with valid inputs
  - Entity ID validation (empty, invalid GUID)
  - Entity type validation
- ✅ **Equality**
  - Value-based equality
  - GetHashCode consistency

#### **10. ValueObjects/TicketNumberTests.cs**
**Responsibilities:** TicketNumber value object validation
- ✅ **Factory Methods**
  - `Create()` with valid string
  - `CreateFromSequence()` format validation
  - Invalid input handling (empty, too long)
- ✅ **Format Validation**
  - Proper ticket number format (TKT-YYYY-XXXXXX)
  - Year consistency

### **Key Testing Patterns to Follow**

1. **Test Naming Convention**: `MethodName_Scenario_ExpectedResult`
2. **Arrange-Act-Assert Pattern**: Clear separation of test phases
3. **FluentAssertions**: Use for readable assertions
4. **Helper Methods**: Create reusable test data factories
5. **Domain Events**: Verify events are raised correctly
6. **Error Testing**: Validate both error types and messages
7. **Business Rules**: Test both positive and negative scenarios

### **Test Data Factories**
Each test class should include helper methods for creating valid test data:
```csharp
private static ContextLink CreateValidContextLink()
private static TicketMessage CreateValidMessage()
private static SupportTicket CreateValidTicket()
```

### **Coverage Goals**
- **90%+ Code Coverage**: Focus on business logic paths
- **All Business Rules**: Every invariant and rule tested
- **Error Scenarios**: Comprehensive error path coverage
- **Domain Events**: All events verified
- **Edge Cases**: Boundary conditions and edge cases

This structure provides comprehensive coverage while keeping tests organized and maintainable. The split by functionality (rather than just by method) makes tests easier to understand and maintain as the aggregate evolves.

---

Always run the desired tests using command line.

Reference existing test files in tests\Domain.UnitTests\SupportTicketAggregate to understand the test pattern.
Pickup the current step.
