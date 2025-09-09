
### **1. TeamCartTestScenario Pattern**
Create a scenario-based approach that encapsulates the entire team cart setup and member management:

```csharp
public class TeamCartTestScenario
{
    public Guid TeamCartId { get; private set; }
    public string ShareToken { get; private set; }
    public Guid HostUserId { get; private set; }
    public Dictionary<string, Guid> GuestUserIds { get; private set; }
    
    // Clean switching methods
    public async Task ActAsHost()
    public async Task ActAsGuest(string guestName)
    public async Task ActAsNonMember()  // For negative testing
}
```

### **2. TeamCartTestBuilder (Fluent Builder Pattern)**
```csharp
public class TeamCartTestBuilder
{
    public static TeamCartTestBuilder Create(Guid restaurantId)
    public TeamCartTestBuilder WithHost(string hostName, string email = null)
    public TeamCartTestBuilder WithGuest(string guestName, string email = null)
    public TeamCartTestBuilder WithMultipleGuests(params string[] guestNames)
    public async Task<TeamCartTestScenario> BuildAsync()
}

// Usage Example:
var scenario = await TeamCartTestBuilder
    .Create(restaurantId)
    .WithHost("Alice Host")
    .WithGuest("Bob Guest")
    .WithGuest("Charlie Guest")
    .BuildAsync();
```

### **3. Enhanced TeamCartRoleTestHelper**
Keep the existing helper but add convenience methods for existing members:

```csharp
public static class TeamCartRoleTestHelper
{
    // Existing methods (keep)
    public static async Task<Guid> RunAsTeamCartHostAsync(Guid teamCartId, string email = null, string password = null)
    
    // New methods for existing members
    public static async Task RunAsExistingTeamCartHostAsync(Guid userId, Guid teamCartId)
    public static async Task RunAsExistingTeamCartMemberAsync(Guid userId, Guid teamCartId)
    
    // Scenario-based setup
    public static async Task<TeamCartTestScenario> SetupTeamCartScenarioAsync(Guid restaurantId, params TeamCartMemberInfo[] members)
}
```

### **4. TeamCartAuthorizationExtensions**
Extension methods for common patterns:

```csharp
public static class TeamCartAuthorizationExtensions
{
    public static async Task AddTeamCartHostPermission(this Guid userId, Guid teamCartId)
    public static async Task AddTeamCartMemberPermission(this Guid userId, Guid teamCartId)
    public static async Task SwitchToTeamCartMember(this Guid userId, Guid teamCartId, MemberRole role)
}
```

### **6. Clean Test Example**
After implementation, tests would look like:

```csharp
[Test]
public async Task GetTeamCartDetails_WithValidCartAndMember_ShouldReturnDetailsWithMembersAndItems()
{
    // Arrange
    var scenario = await TeamCartTestBuilder
        .Create(Testing.TestData.DefaultRestaurantId)
        .WithHost("Alice Host")
        .WithGuest("Bob Guest")
        .BuildAsync();

    // Add items
    await scenario.ActAsHost();
    var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
    await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, itemId, 2));

    await scenario.ActAsGuest("Bob Guest");
    await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, itemId, 1));

    // Host operations
    await scenario.ActAsHost();
    await SendAsync(new LockTeamCartForPaymentCommand(scenario.TeamCartId));
    await SendAsync(new ApplyTipToTeamCartCommand(scenario.TeamCartId, 5.00m));

    // Act
    var result = await SendAsync(new GetTeamCartDetailsQuery(scenario.TeamCartId));

    // Assert
    result.ShouldBeSuccessful();
    // ... clean assertions using scenario.HostUserId, scenario.GuestUserIds["Bob Guest"]
}
```

### 🚨 **Authorization Exception Handling**

**Key Learning:** When authorization policies fail, they throw `ForbiddenAccessException` at the pipeline level, not business logic errors. The test correctly expects:

```csharp
await FluentActions.Invoking(() => 
        SendAsync(new ApplyCouponToTeamCartCommand(scenario.TeamCartId, couponCode)))
    .Should().ThrowAsync<ForbiddenAccessException>();
```

Instead of expecting a business logic error code like `"TeamCart.OnlyHostCanModifyFinancials"`.

This approach would eliminate the repetitive authorization setup code while making the tests much more readable and maintainable. The builder pattern particularly helps with complex scenarios involving multiple members.

## Overall Test Changes Required

Here's the comprehensive guide for updating TeamCart tests to work with the new authorization pattern:

### 1. **Commands That Need TeamCart Host Authorization**
These commands require `MustBeTeamCartHost` policy and tests need `RunAsTeamCartHostAsync()`:
- `LockTeamCartForPaymentCommand` - Only host can lock
- `ApplyTipToTeamCartCommand` - Only host can apply tips
- `ApplyCouponToTeamCartCommand` - Only host can apply coupons
- `RemoveCouponFromTeamCartCommand` - Only host can remove coupons
- `HandleTeamCartStripeWebhookCommand` - Only host handles payments
- `ConvertTeamCartToOrderCommand` - Only host can convert

### 2. **Commands That Need TeamCart Member Authorization**  
These commands require `MustBeTeamCartMember` policy and tests can use either `RunAsTeamCartHostAsync()` or `RunAsTeamCartMemberAsync()`:
- `AddItemToTeamCartCommand` - Members can add items
- `UpdateTeamCartItemQuantityCommand` - Members can update quantities of the items they added
- `RemoveItemFromTeamCartCommand` - Members can remove items they added
- `InitializeTeamCartPaymentCommand` - Members can initialize payment
- `CommitToCodPaymentCommand` - Members can commit to payment

### 3. **Commands That Need Basic Authentication Only**
These commands use `[Authorize]` and tests can use any authenticated user:
- `CreateTeamCartCommand` - Host creates the cart (only need authenticated user)
- `JoinTeamCartCommand` - Anyone can join with valid token (as you fixed)

### 4. **Queries That Need TeamCart Member Authorization**
These queries require `MustBeTeamCartMember` policy:
- `GetTeamCartDetailsQuery` - Members can view details
- `GetTeamCartRealTimeViewModelQuery` - Members can view real-time updates

### 6. **Chicken-and-Egg Problem Solution**

The main challenge is that many tests follow this pattern:
1. Create team cart as host
2. Join as guest  
3. Perform operations as both users

But our authorization requires users to already have permissions. The solution:

**For Join Operations**: Use basic authenticated users (as you fixed with `[Authorize]`)
**For Other Operations**: Use the helper methods to pre-establish permissions

### 7. **Test Initialization Pattern**

Each test class should initialize TeamCart roles:
```csharp
[OneTimeSetUp]
public async Task OneTimeSetUp()
{
    await TeamCartRoleTestHelper.SetupTeamCartAuthorizationTestsAsync();
}
```

### 8. **Files That Need Updates**

Based on the test file structure, these test files need authorization updates:
- GetTeamCartDetailsQueryTests.cs - Use member helpers
- GetTeamCartRealTimeViewModelQueryTests.cs - Use member helpers  
- All command test files in `Commands/` folder - Use appropriate host/member helpers
- Event handler tests - May need member permissions to trigger events

### 9. **Key Principle**

The new pattern separates:
- **Test Setup** (creating users with permissions) from **Business Logic** (the actual commands)
- **Authorization Concerns** (who can do what) from **Domain Logic** (what gets done)

This makes tests more explicit about authorization requirements and more reliable.

The `TeamCartRoleTestHelper` provides the missing piece to make this transition smooth and maintainable.