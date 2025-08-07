You're absolutely right! Based on our earlier discussion and the current test architecture, we need a flexible approach that supports both global service mocking (for most tests) and dynamic service replacement (for specific test suites). Let me outline the proper implementation strategy:

## Current Test Architecture Analysis

### Existing Pattern:
1. **`CustomWebApplicationFactory`** - Global service mocking (FCM, etc.)
2. **`BaseTestFixture`** - Standard test setup with `ResetState()`
3. **Testing.cs** - Static utilities (`Send()`, `GetService()`, `SetUserId()`)
4. **TestInfrastructure.cs** - Core DI container management

### Current Limitations:
- No dynamic service replacement capability
- All service mocking is global/static
- Cannot configure different services per test suite

## Proposed Implementation Outline

### 1. **Enhanced Service Replacement Infrastructure**

#### A. Add Service Replacement to Testing.cs
```csharp
// New methods to add to Testing.cs
public static void ReplaceService<TInterface>(TInterface implementation) where TInterface : class
public static void ReplaceService<TInterface, TImplementation>() where TInterface : class where TImplementation : class, TInterface
public static void ResetServiceReplacements() // Clear all replacements
```

#### B. Enhance TestInfrastructure.cs
```csharp
// Track service replacements
private static readonly Dictionary<Type, object> _serviceReplacements = new();

// Method to rebuild container with replacements
private static void RebuildContainerWithReplacements()

// Enhanced factory creation that applies replacements
private static CustomWebApplicationFactory CreateFactoryWithReplacements()
```

### 2. **Three-Tier Service Configuration Strategy**

#### Tier 1: Global Defaults (CustomWebApplicationFactory)
```csharp
private static void ConfigureMockServices(IServiceCollection services)
{
    ConfigureFcmServiceMock(services);
    ConfigureDefaultPaymentGatewayMock(services); // New - safe default
    // Other global mocks...
}

// Safe default that works for most tests
private static void ConfigureDefaultPaymentGatewayMock(IServiceCollection services)
{
    // Basic mock that returns success for standard scenarios
}
```

#### Tier 2: Suite-Level Configuration (Test Base Classes)
```csharp
public abstract class InitiateOrderTestBase : BaseTestFixture
{
    protected Mock<IPaymentGatewayService> PaymentGatewayMock { get; private set; }
    
    [SetUp]
    public virtual async Task SetUp()
    {
        // Override global mock with suite-specific configuration
        PaymentGatewayMock = InitiateOrderTestHelper.SetupSuccessfulPaymentGatewayMock();
        ReplaceService<IPaymentGatewayService>(PaymentGatewayMock.Object);
    }
}

public abstract class PaymentIntegrationTestBase : BaseTestFixture
{
    [SetUp] 
    public virtual async Task SetUp()
    {
        // Use real Stripe service for integration tests
        ReplaceService<IPaymentGatewayService, StripePaymentGatewayService>();
    }
}
```

#### Tier 3: Test-Level Overrides (Individual Tests)
```csharp
[Test]
public async Task SpecificScenario_WithCustomPaymentBehavior()
{
    // Override suite-level configuration for this specific test
    var customMock = new Mock<IPaymentGatewayService>();
    customMock.Setup(x => x.CreatePaymentIntentAsync(...)).ReturnsAsync(customResult);
    ReplaceService<IPaymentGatewayService>(customMock.Object);
    
    // Test logic...
}
```

### 3. **Service Replacement Lifecycle Management**

#### A. Container Lifecycle
```
Test Start → BaseTestFixture.TestSetUp() → ResetState() → Clear Service Replacements
          ↓
Suite SetUp → Suite-level service replacements applied
          ↓
Test SetUp → Test-level service replacements applied (if any)
          ↓
Test Execution → Uses configured services
          ↓
Test End → Service replacements preserved for next test in suite
          ↓
Suite End → Service replacements cleared
```

#### B. Performance Optimization
- **Lazy Container Recreation**: Only rebuild DI container when service replacements change
- **Replacement Tracking**: Compare current vs requested replacements to avoid unnecessary rebuilds
- **Suite-Level Caching**: Cache container configuration at suite level when possible

### 4. **Test Helper Integration**

#### Enhanced InitiateOrderTestHelper
```csharp
public static class InitiateOrderTestHelper
{
    #region Service Configuration Helpers
    
    public static void ConfigureForSuccessfulPayments()
    {
        var mock = SetupSuccessfulPaymentGatewayMock();
        Testing.ReplaceService<IPaymentGatewayService>(mock.Object);
    }
    
    public static void ConfigureForPaymentFailures(string errorMessage)
    {
        var mock = SetupFailingPaymentGatewayMock(errorMessage);
        Testing.ReplaceService<IPaymentGatewayService>(mock.Object);
    }
    
    public static void ConfigureForIntegrationTesting()
    {
        Testing.ReplaceService<IPaymentGatewayService, StripePaymentGatewayService>();
    }
    
    #endregion
    
    // Existing command builders, assertion helpers, etc.
}
```

### 5. **Usage Patterns for Different Test Types**

#### Pattern A: Command Logic Tests (Most Common)
```csharp
public class InitiateOrderValidationTests : InitiateOrderTestBase
{
    // Inherits PaymentGatewayMock setup from base class
    // Uses mocked payment service by default
    // Individual tests can override if needed
}
```

#### Pattern B: Integration Tests
```csharp
public class PaymentIntegrationTests : BaseTestFixture
{
    [SetUp]
    public async Task SetUp()
    {
        ReplaceService<IPaymentGatewayService, StripePaymentGatewayService>();
        // Uses real Stripe service
    }
}
```

#### Pattern C: Mixed/Custom Tests
```csharp
public class OrderWorkflowTests : BaseTestFixture
{
    [SetUp]
    public async Task SetUp()
    {
        // Real order service, mocked payment service
        var paymentMock = new Mock<IPaymentGatewayService>();
        ReplaceService<IPaymentGatewayService>(paymentMock.Object);
        // Keep real order repository, etc.
    }
}
```

### 6. **Benefits of This Approach**

#### ✅ **Flexibility**
- Global defaults for common scenarios
- Suite-level configuration for specialized needs
- Test-level overrides for edge cases

#### ✅ **Performance**
- Minimal container recreation
- Efficient service replacement tracking
- Cached configurations where possible

#### ✅ **Maintainability**
- Clear service configuration hierarchy
- Centralized mock management
- Consistent patterns across test suites

#### ✅ **Developer Experience**
- Simple API (`ReplaceService<T>()`)
- Intuitive inheritance-based configuration
- Helper methods for common scenarios

### 7. **Implementation Priority**

1. **Phase 1**: Add basic service replacement to Testing.cs and TestInfrastructure.cs
2. **Phase 2**: Add default `IPaymentGatewayService` mock to `CustomWebApplicationFactory`
3. **Phase 3**: Update `InitiateOrderTestBase` to use service replacement
4. **Phase 4**: Add performance optimizations (lazy rebuilding, caching)
5. **Phase 5**: Create helper methods for common service configurations

This approach maintains backward compatibility while providing the flexibility we need for different test scenarios, aligning perfectly with the existing test architecture patterns.