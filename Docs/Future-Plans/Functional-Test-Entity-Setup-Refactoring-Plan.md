# Functional Test Entity Setup Refactoring Plan

## Overview

This document outlines a comprehensive plan to refactor the functional test infrastructure in YummyZoom to provide robust, maintainable, and reusable entity setup patterns. The refactoring addresses current issues with missing test data dependencies and establishes a foundation for scalable test development.

## Current State Analysis

### Problems Identified

1. **Missing Entity Dependencies**: Tests fail due to missing required entities (e.g., MenuItems for order flow)
2. **Random GUID Generation**: Test helpers generate random IDs that don't correspond to actual database entities
3. **Scattered Setup Logic**: Entity creation logic is duplicated across test files
4. **Incomplete Test Data**: Tests don't set up complete object graphs needed for complex scenarios
5. **Poor Maintainability**: Changes to domain entities require updates in multiple test files

### Current Test Failure Example
```
InitiateOrder.MenuItemsNotFound: One or more menu items were not found.
```
This occurs because `PaymentTestHelper.BuildTestOrderItems()` generates random MenuItemIds that don't exist in the test database.

## Goals and Objectives

### Primary Goals
1. **Fix Immediate Test Failures**: Ensure all existing functional tests pass
2. **Establish Reusable Patterns**: Create consistent, reusable entity setup patterns
3. **Improve Test Maintainability**: Centralize entity creation logic
4. **Enable Complex Scenarios**: Support multi-entity test scenarios easily
5. **Performance Optimization**: Minimize database operations in test setup

### Success Criteria
- [ ] All functional tests pass consistently
- [ ] New tests can be written with minimal entity setup boilerplate
- [ ] Test data creation is consistent across the test suite
- [ ] Entity relationships are properly maintained in test data
- [ ] Test execution time is not significantly impacted

## Architecture Design

### 1. Test Data Architecture

```
tests/Application.FunctionalTests/
├── TestData/
│   ├── Builders/
│   │   ├── Core/
│   │   │   ├── ITestDataBuilder.cs
│   │   │   ├── TestDataContext.cs
│   │   │   └── TestDataSeeder.cs
│   │   ├── Aggregates/
│   │   │   ├── RestaurantTestDataBuilder.cs
│   │   │   ├── MenuItemTestDataBuilder.cs
│   │   │   ├── OrderTestDataBuilder.cs
│   │   │   ├── UserTestDataBuilder.cs
│   │   │   ├── CouponTestDataBuilder.cs
│   │   │   └── TeamCartTestDataBuilder.cs
│   │   └── ValueObjects/
│   │       ├── AddressTestDataBuilder.cs
│   │       ├── MoneyTestDataBuilder.cs
│   │       └── ContactInfoTestDataBuilder.cs
│   ├── Scenarios/
│   │   ├── OrderFlowScenarios.cs
│   │   ├── PaymentScenarios.cs
│   │   ├── RestaurantScenarios.cs
│   │   ├── TeamCartScenarios.cs
│   │   └── CouponScenarios.cs
│   ├── Models/
│   │   ├── TestDataResults.cs
│   │   ├── ScenarioResults.cs
│   │   └── EntityCollections.cs
│   └── Extensions/
│       ├── TestDataBuilderExtensions.cs
│       └── DatabaseTestExtensions.cs
```

### 2. Builder Pattern Implementation

#### Base Builder Interface
```csharp
public interface ITestDataBuilder<TEntity, TResult>
{
    Task<TResult> BuildAsync(TestDataContext context);
    TBuilder Reset();
}

public interface ITestDataBuilder<TEntity> : ITestDataBuilder<TEntity, TEntity>
{
}
```

#### Fluent Builder Pattern
```csharp
public class RestaurantTestDataBuilder : ITestDataBuilder<Restaurant, RestaurantTestData>
{
    public RestaurantTestDataBuilder WithName(string name);
    public RestaurantTestDataBuilder WithCuisine(string cuisine);
    public RestaurantTestDataBuilder AsActive();
    public RestaurantTestDataBuilder AsVerified();
    public RestaurantTestDataBuilder WithMenuItems(int count = 3);
    public RestaurantTestDataBuilder WithCoupons(params CouponConfiguration[] coupons);
    public Task<RestaurantTestData> BuildAsync(TestDataContext context);
}
```

### 3. Scenario-Based Setup

#### Predefined Scenarios
```csharp
public static class OrderFlowScenarios
{
    public static async Task<OrderFlowTestData> BasicOnlineOrderScenario(TestDataContext context);
    public static async Task<OrderFlowTestData> CashOnDeliveryOrderScenario(TestDataContext context);
    public static async Task<OrderFlowTestData> OrderWithCouponScenario(TestDataContext context);
    public static async Task<OrderFlowTestData> TeamCartOrderScenario(TestDataContext context);
    public static async Task<OrderFlowTestData> OrderWithUnavailableItemsScenario(TestDataContext context);
}
```

## Implementation Plan

### Phase 1: Foundation Infrastructure (Week 1-2)

#### Step 1.1: Create Core Interfaces and Base Classes
- [ ] **File**: `tests/Application.FunctionalTests/TestData/Core/ITestDataBuilder.cs`
  - Define base builder interfaces
  - Include async operations support
  - Support for fluent API
  
- [ ] **File**: `tests/Application.FunctionalTests/TestData/Core/TestDataContext.cs`
  - Database context wrapper for test operations
  - Entity tracking and relationship management
  - Cleanup coordination

- [ ] **File**: `tests/Application.FunctionalTests/TestData/Core/TestDataSeeder.cs`
  - Central orchestrator for test data creation
  - Dependency resolution between entities
  - Performance optimization (bulk operations)

#### Step 1.2: Create Data Transfer Objects
- [ ] **File**: `tests/Application.FunctionalTests/TestData/Models/TestDataResults.cs`
  ```csharp
  public record RestaurantTestData(
      Guid Id,
      string Name,
      string Cuisine,
      bool IsActive,
      bool IsVerified,
      List<MenuItemTestData> MenuItems,
      List<CouponTestData> Coupons);

  public record MenuItemTestData(
      Guid Id,
      Guid RestaurantId,
      Guid CategoryId,
      string Name,
      decimal Price,
      string Currency,
      bool IsAvailable);

  public record OrderFlowTestData(
      Guid CustomerId,
      RestaurantTestData Restaurant,
      List<MenuItemTestData> MenuItems,
      List<CouponTestData> Coupons);
  ```

#### Step 1.3: Enhanced Base Test Fixture
- [ ] **File**: `tests/Application.FunctionalTests/Common/EntityTestFixture.cs`
  - Extend BaseTestFixture with entity setup capabilities
  - Provide common test data access patterns
  - Integrate with TestDataContext

### Phase 2: Core Entity Builders (Week 2-3)

#### Step 2.1: Restaurant Builder
- [ ] **File**: `tests/Application.FunctionalTests/TestData/Builders/Aggregates/RestaurantTestDataBuilder.cs`
  ```csharp
  public class RestaurantTestDataBuilder
  {
      public RestaurantTestDataBuilder WithName(string name);
      public RestaurantTestDataBuilder WithCuisine(string cuisine);
      public RestaurantTestDataBuilder WithAddress(string street, string city, string state, string zipCode, string country);
      public RestaurantTestDataBuilder WithContactInfo(string phone, string email);
      public RestaurantTestDataBuilder WithBusinessHours(string hours);
      public RestaurantTestDataBuilder AsActive();
      public RestaurantTestDataBuilder AsVerified();
      public RestaurantTestDataBuilder AsAcceptingOrders();
      public RestaurantTestDataBuilder WithMenuCategory(string categoryName);
      public RestaurantTestDataBuilder WithMenuItems(int count = 3, bool allAvailable = true);
      public RestaurantTestDataBuilder WithCoupons(params CouponConfiguration[] coupons);
      public Task<RestaurantTestData> BuildAsync(TestDataContext context);
  }
  ```

#### Step 2.2: MenuItem Builder
- [ ] **File**: `tests/Application.FunctionalTests/TestData/Builders/Aggregates/MenuItemTestDataBuilder.cs`
  ```csharp
  public class MenuItemTestDataBuilder
  {
      public MenuItemTestDataBuilder ForRestaurant(Guid restaurantId);
      public MenuItemTestDataBuilder WithName(string name);
      public MenuItemTestDataBuilder WithDescription(string description);
      public MenuItemTestDataBuilder WithPrice(decimal amount, string currency = "USD");
      public MenuItemTestDataBuilder WithCategory(Guid categoryId);
      public MenuItemTestDataBuilder WithImage(string imageUrl);
      public MenuItemTestDataBuilder AsAvailable(bool available = true);
      public MenuItemTestDataBuilder WithDietaryTags(params string[] tags);
      public MenuItemTestDataBuilder WithCustomizations(params CustomizationConfiguration[] customizations);
      public Task<MenuItemTestData> BuildAsync(TestDataContext context);
  }
  ```

#### Step 2.3: User/Customer Builder
- [ ] **File**: `tests/Application.FunctionalTests/TestData/Builders/Aggregates/UserTestDataBuilder.cs`
  ```csharp
  public class UserTestDataBuilder
  {
      public UserTestDataBuilder WithEmail(string email);
      public UserTestDataBuilder WithFirstName(string firstName);
      public UserTestDataBuilder WithLastName(string lastName);
      public UserTestDataBuilder WithRole(string role);
      public UserTestDataBuilder AsDefaultUser();
      public Task<UserTestData> BuildAsync(TestDataContext context);
  }
  ```

### Phase 3: Advanced Entity Builders (Week 3-4)

#### Step 3.1: Order Builder
- [ ] **File**: `tests/Application.FunctionalTests/TestData/Builders/Aggregates/OrderTestDataBuilder.cs`
  ```csharp
  public class OrderTestDataBuilder
  {
      public OrderTestDataBuilder ForCustomer(Guid customerId);
      public OrderTestDataBuilder ForRestaurant(Guid restaurantId);
      public OrderTestDataBuilder WithItems(params (Guid menuItemId, int quantity)[] items);
      public OrderTestDataBuilder WithDeliveryAddress(AddressConfiguration address);
      public OrderTestDataBuilder WithPaymentMethod(PaymentMethodType paymentMethod);
      public OrderTestDataBuilder WithSpecialInstructions(string instructions);
      public OrderTestDataBuilder WithCoupon(string couponCode);
      public OrderTestDataBuilder WithTip(decimal amount);
      public OrderTestDataBuilder AsOnlinePayment();
      public OrderTestDataBuilder AsCashOnDelivery();
      public Task<OrderTestData> BuildAsync(TestDataContext context);
  }
  ```

#### Step 3.2: Coupon Builder
- [ ] **File**: `tests/Application.FunctionalTests/TestData/Builders/Aggregates/CouponTestDataBuilder.cs`
  ```csharp
  public class CouponTestDataBuilder
  {
      public CouponTestDataBuilder ForRestaurant(Guid restaurantId);
      public CouponTestDataBuilder WithCode(string code);
      public CouponTestDataBuilder WithDiscountPercentage(decimal percentage);
      public CouponTestDataBuilder WithDiscountAmount(decimal amount, string currency = "USD");
      public CouponTestDataBuilder WithValidityPeriod(DateTime start, DateTime end);
      public CouponTestDataBuilder AsActive();
      public CouponTestDataBuilder AsExpired();
      public CouponTestDataBuilder WithUsageLimit(int limit);
      public CouponTestDataBuilder WithMinimumOrderAmount(decimal amount, string currency = "USD");
      public Task<CouponTestData> BuildAsync(TestDataContext context);
  }
  ```

#### Step 3.3: TeamCart Builder
- [ ] **File**: `tests/Application.FunctionalTests/TestData/Builders/Aggregates/TeamCartTestDataBuilder.cs`
  ```csharp
  public class TeamCartTestDataBuilder
  {
      public TeamCartTestDataBuilder WithCreator(Guid creatorId);
      public TeamCartTestDataBuilder ForRestaurant(Guid restaurantId);
      public TeamCartTestDataBuilder WithInvitees(params Guid[] userIds);
      public TeamCartTestDataBuilder WithItems(params (Guid menuItemId, int quantity, Guid userId)[] items);
      public TeamCartTestDataBuilder WithDeadline(DateTime deadline);
      public TeamCartTestDataBuilder AsActive();
      public TeamCartTestDataBuilder AsFinalized();
      public Task<TeamCartTestData> BuildAsync(TestDataContext context);
  }
  ```

### Phase 4: Scenario Implementations (Week 4-5)

#### Step 4.1: Order Flow Scenarios
- [ ] **File**: `tests/Application.FunctionalTests/TestData/Scenarios/OrderFlowScenarios.cs`
  ```csharp
  public static class OrderFlowScenarios
  {
      public static async Task<OrderFlowTestData> BasicOnlineOrderScenario(TestDataContext context)
      {
          // Creates: User + Active Restaurant + MenuCategory + 3 MenuItems + All dependencies
      }

      public static async Task<OrderFlowTestData> CashOnDeliveryOrderScenario(TestDataContext context)
      {
          // Creates: Complete setup for COD order testing
      }

      public static async Task<OrderFlowTestData> OrderWithCouponScenario(TestDataContext context)
      {
          // Creates: Setup with valid coupon for discount testing
      }

      public static async Task<OrderFlowTestData> OrderWithExpiredCouponScenario(TestDataContext context)
      {
          // Creates: Setup with expired coupon for error testing
      }

      public static async Task<OrderFlowTestData> TeamCartOrderScenario(TestDataContext context)
      {
          // Creates: Multi-user team cart scenario
      }

      public static async Task<OrderFlowTestData> OrderWithUnavailableItemsScenario(TestDataContext context)
      {
          // Creates: Setup with some unavailable menu items for error testing
      }

      public static async Task<OrderFlowTestData> HighVolumeOrderScenario(TestDataContext context)
      {
          // Creates: Large restaurant with many items for performance testing
      }
  }
  ```

#### Step 4.2: Payment Scenarios
- [ ] **File**: `tests/Application.FunctionalTests/TestData/Scenarios/PaymentScenarios.cs`
  ```csharp
  public static class PaymentScenarios
  {
      public static async Task<PaymentTestData> SuccessfulCreditCardPaymentScenario(TestDataContext context);
      public static async Task<PaymentTestData> FailedPaymentScenario(TestDataContext context);
      public static async Task<PaymentTestData> RefundScenario(TestDataContext context);
      public static async Task<PaymentTestData> WebhookProcessingScenario(TestDataContext context);
  }
  ```

#### Step 4.3: Restaurant Management Scenarios
- [ ] **File**: `tests/Application.FunctionalTests/TestData/Scenarios/RestaurantScenarios.cs`
  ```csharp
  public static class RestaurantScenarios
  {
      public static async Task<RestaurantTestData> NewRestaurantSetup(TestDataContext context);
      public static async Task<RestaurantTestData> ActiveRestaurantWithFullMenu(TestDataContext context);
      public static async Task<RestaurantTestData> RestaurantWithSeasonalMenu(TestDataContext context);
      public static async Task<RestaurantTestData> RestaurantWithPromotions(TestDataContext context);
  }
  ```

### Phase 5: Integration and Migration (Week 5-6)

#### Step 5.1: Update Existing Helper Classes
- [ ] **Update**: `tests/Application.FunctionalTests/Features/Orders/PaymentIntegration/PaymentTestHelper.cs`
  ```csharp
  // Replace current methods with:
  public static async Task<InitiateOrderCommand> BuildValidOnlineOrderCommandAsync(
      TestDataContext context,
      Guid? customerId = null,
      Guid? restaurantId = null,
      string paymentMethod = "CreditCard",
      decimal? tipAmount = 5.00m)
  {
      var testData = restaurantId.HasValue 
          ? await context.GetRestaurantTestDataAsync(restaurantId.Value)
          : await OrderFlowScenarios.BasicOnlineOrderScenario(context);
      
      return new InitiateOrderCommand(
          CustomerId: customerId ?? testData.CustomerId,
          RestaurantId: testData.Restaurant.Id,
          Items: BuildOrderItemsFromTestData(testData.MenuItems.Take(2)),
          DeliveryAddress: BuildTestDeliveryAddress(),
          PaymentMethod: paymentMethod,
          SpecialInstructions: "Test order - please handle with care",
          CouponCode: null,
          TipAmount: tipAmount,
          TeamCartId: null
      );
  }
  ```

#### Step 5.2: Update Test Base Classes
- [ ] **Update**: `tests/Application.FunctionalTests/Common/BaseTestFixture.cs`
  ```csharp
  public abstract class BaseTestFixture
  {
      protected TestDataContext TestDataContext { get; private set; } = null!;

      [SetUp]
      public virtual async Task SetUp()
      {
          await ResetState();
          TestDataContext = new TestDataContext(GetDatabaseConnection());
      }

      // Add convenience methods
      protected async Task<RestaurantTestData> CreateBasicRestaurant() =>
          await RestaurantScenarios.ActiveRestaurantWithFullMenu(TestDataContext);

      protected async Task<OrderFlowTestData> CreateOrderFlowData() =>
          await OrderFlowScenarios.BasicOnlineOrderScenario(TestDataContext);
  }
  ```

#### Step 5.3: Migrate Existing Tests
- [ ] **Update**: `tests/Application.FunctionalTests/Features/Orders/PaymentIntegration/OnlineOrderPaymentTests.cs`
  ```csharp
  public class OnlineOrderPaymentTests : BaseTestFixture
  {
      private OrderFlowTestData _orderFlowData = null!;

      [SetUp]
      public async Task SetUp()
      {
          await base.SetUp();
          
          // Replace manual entity creation with scenario
          _orderFlowData = await OrderFlowScenarios.BasicOnlineOrderScenario(TestDataContext);
          
          // Set up Stripe configuration
          var stripeOptions = GetService<IOptions<StripeOptions>>().Value;
          StripeConfiguration.ApiKey = stripeOptions.SecretKey;
      }

      [Test]
      public async Task HandleWebhook_WhenProcessedTwice_ShouldBeIdempotent()
      {
          // Use real data from scenario
          var initiateOrderCommand = await PaymentTestHelper.BuildValidOnlineOrderCommandAsync(
              TestDataContext,
              customerId: _orderFlowData.CustomerId,
              restaurantId: _orderFlowData.Restaurant.Id);

          // Rest of test remains the same...
      }
  }
  ```

### Phase 6: Advanced Features and Optimizations (Week 6-7)

#### Step 6.1: Performance Optimizations
- [ ] **Bulk Operations Support**
  ```csharp
  public class BulkTestDataBuilder
  {
      public BulkTestDataBuilder CreateRestaurants(int count);
      public BulkTestDataBuilder CreateMenuItems(int countPerRestaurant);
      public BulkTestDataBuilder CreateUsers(int count);
      public async Task<BulkTestData> ExecuteAsync(TestDataContext context);
  }
  ```

- [ ] **Caching and Reuse**
  ```csharp
  public class TestDataCache
  {
      public async Task<RestaurantTestData> GetOrCreateRestaurant(string key, Func<Task<RestaurantTestData>> factory);
      public async Task<UserTestData> GetOrCreateUser(string key, Func<Task<UserTestData>> factory);
  }
  ```

#### Step 6.2: Advanced Scenarios
- [ ] **Multi-tenant Scenarios**
- [ ] **Performance Testing Scenarios**
- [ ] **Error Condition Scenarios**
- [ ] **Integration Testing Scenarios**

#### Step 6.3: Validation and Testing
- [ ] **Data Integrity Validation**
  ```csharp
  public class TestDataValidator
  {
      public async Task ValidateOrderFlowData(OrderFlowTestData data);
      public async Task ValidateEntityRelationships(TestDataContext context);
  }
  ```

- [ ] **Test Data Metrics**
  ```csharp
  public class TestDataMetrics
  {
      public TimeSpan SetupTime { get; }
      public int EntitiesCreated { get; }
      public long DatabaseOperations { get; }
  }
  ```

### Phase 7: Documentation and Training (Week 7-8)

#### Step 7.1: Documentation
- [ ] **File**: `Docs/Development-Guidelines/Functional-Test-Data-Setup-Guidelines.md`
  - Best practices for using the new infrastructure
  - Common patterns and examples
  - Performance considerations
  - Troubleshooting guide

- [ ] **File**: `tests/Application.FunctionalTests/TestData/README.md`
  - Architecture overview
  - Quick start guide
  - API reference
  - Examples for each builder

#### Step 7.2: Code Examples and Templates
- [ ] **File**: `tests/Application.FunctionalTests/TestData/Examples/`
  - Common test scenarios
  - Builder usage patterns
  - Performance optimization examples

#### Step 7.3: Migration Guide
- [ ] **File**: `Docs/Migration-Guides/Test-Data-Setup-Migration.md`
  - Step-by-step migration for existing tests
  - Before/after comparisons
  - Common migration issues and solutions

## Success Metrics

### Immediate Success Indicators
- [ ] All existing functional tests pass without modification
- [ ] New tests can be written with 80% less entity setup code
- [ ] Test execution time increases by less than 20%

### Long-term Success Indicators
- [ ] Developer satisfaction with test writing experience
- [ ] Reduced test maintenance overhead
- [ ] Improved test coverage through easier test creation
- [ ] Fewer test-related bugs in CI/CD pipeline

### Performance Targets
- [ ] Entity setup time < 500ms per test
- [ ] Memory usage increase < 10% during test execution
- [ ] Database operations minimized through batching

## Risk Assessment and Mitigation

### High Risk Items
1. **Performance Impact**: Risk of slower test execution
   - *Mitigation*: Implement caching and bulk operations
   - *Monitoring*: Measure setup time before/after implementation

2. **Complex Migration**: Risk of breaking existing tests
   - *Mitigation*: Phased migration approach with backward compatibility
   - *Monitoring*: Comprehensive test suite validation

3. **Over-Engineering**: Risk of creating overly complex infrastructure
   - *Mitigation*: Start simple, iterate based on real needs
   - *Monitoring*: Regular code review and refactoring

### Medium Risk Items
1. **Developer Adoption**: Risk of developers not using new patterns
   - *Mitigation*: Comprehensive documentation and training
   - *Monitoring*: Code review enforcement

2. **Maintenance Overhead**: Risk of new infrastructure requiring maintenance
   - *Mitigation*: Good test coverage for test infrastructure itself
   - *Monitoring*: Regular infrastructure health checks

## Timeline and Milestones

### Week 1-2: Foundation
- [ ] Core interfaces and base classes
- [ ] TestDataContext implementation
- [ ] Enhanced BaseTestFixture

### Week 3-4: Core Builders
- [ ] Restaurant, MenuItem, User builders
- [ ] Basic scenario implementations
- [ ] Fix immediate test failures

### Week 5-6: Advanced Features
- [ ] Order, Coupon, TeamCart builders
- [ ] Complete scenario library
- [ ] Migration of existing tests

### Week 7-8: Optimization and Documentation
- [ ] Performance optimizations
- [ ] Complete documentation
- [ ] Team training and rollout

## Dependencies and Prerequisites

### Technical Dependencies
- [ ] EF Core test database infrastructure
- [ ] Existing domain entity implementations
- [ ] Current test framework setup

### Team Dependencies
- [ ] Domain expert review of entity relationships
- [ ] QA team input on test scenarios
- [ ] Development team training schedule

### External Dependencies
- [ ] Database migration capabilities
- [ ] CI/CD pipeline compatibility
- [ ] Performance testing environment

## Future Considerations

### Potential Extensions
1. **Auto-generated Test Data**: Use libraries like AutoFixture for property population
2. **Schema Evolution Support**: Handle domain model changes automatically
3. **Cross-aggregate Transaction Testing**: Complex multi-aggregate scenarios
4. **Test Data Versioning**: Support for different test data versions
5. **Real-time Test Data Monitoring**: Dashboard for test data health

### Integration Opportunities
1. **Integration with Unit Test Helpers**: Shared test data patterns
2. **API Testing Integration**: REST API endpoint testing with same data
3. **Performance Test Integration**: Load testing with realistic data
4. **Documentation Generation**: Auto-generated test scenario documentation

This comprehensive plan provides a roadmap for transforming the functional test infrastructure into a robust, maintainable, and scalable system that will serve the YummyZoom project well into the future.