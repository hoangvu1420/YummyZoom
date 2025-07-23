# Context for Compilation Errors in `TeamCartConversionServiceFailureTests.cs`

## 1. Introduction

The compilation errors in `TeamCartConversionServiceFailureTests.cs` are due to a recent refactoring of the `TeamCartConversionService`. The service's primary responsibility is to convert a `TeamCart` into a final `Order`. The refactoring introduced a dependency on `OrderFinancialService` to handle all financial calculations, which changed the method signature for `ConvertToOrder`. The tests were not updated to reflect this new signature, causing them to fail during compilation.

This document provides the necessary context to understand and fix these errors.

## 2. The Logic Under Test: `TeamCartConversionService`

The `TeamCartConversionService` orchestrates the conversion of a `TeamCart` that is in the `ReadyToConfirm` state into an `Order`. It validates the cart, calculates the final financials, creates payment records, and finalizes the state of both the `Order` and the `TeamCart`.

### `TeamCartConversionService.cs`

```csharp
using Domain.AggregationModels.Order;
using Domain.AggregationModels.TeamCart;
using Domain.Common.Interfaces;
using Domain.Common.Models;
using Domain.Common.Services;
using Domain.Errors;
using Domain.Services.Interfaces;

namespace Domain.Services;

public class TeamCartConversionService : ITeamCartConversionService
{
    private readonly IOrderFinancialService _orderFinancialService;

    public TeamCartConversionService(IOrderFinancialService orderFinancialService)
    {
        _orderFinancialService = orderFinancialService;
    }

    public Result<(Order Order, TeamCart TeamCart)> ConvertToOrder(
        TeamCart teamCart,
        DeliveryAddress deliveryAddress,
        string specialInstructions,
        Coupon? coupon,
        int currentUserCouponUsageCount,
        Money deliveryFee,
        Money taxAmount)
    {
        if (teamCart.Status is not TeamCartStatus.ReadyToConfirm)
            return Result.Failure<(Order, TeamCart)>(TeamCartErrors.InvalidStatusForConversion(teamCart.Status));

        var orderItems = teamCart.Items.Select(item => OrderItem.Create(
            item.DishId,
            item.DishName,
            item.DishPrice,
            item.Quantity,
            item.SpecialInstructions).Value).ToList();

        var financialDetailsResult = _orderFinancialService.CalculateFinalOrderFinancials(
            orderItems,
            coupon,
            currentUserCouponUsageCount,
            deliveryFee,
            taxAmount);

        if (financialDetailsResult.IsFailure)
            return Result.Failure<(Order, TeamCart)>(financialDetailsResult.Error);

        var (subtotal, discount, finalTotal) = financialDetailsResult.Value;

        var paymentTransactions = CreatePaymentTransactions(
            teamCart.MemberPayments,
            finalOrderTotal);

        var orderResult = Order.Create(
            teamCart.HostId,
            teamCart.RestaurantId,
            deliveryAddress,
            orderItems,
            paymentTransactions,
            subtotal,
            discount,
            deliveryFee,
            taxAmount,
            finalTotal,
            specialInstructions,
            coupon?.Id);

        if (orderResult.IsFailure)
            return Result.Failure<(Order, TeamCart)>(orderResult.Error);

        var updatedTeamCart = teamCart.MarkAsConverted(orderResult.Value.Id).Value;

        return (orderResult.Value, updatedTeamCart);
    }

    private static List<PaymentTransaction> CreatePaymentTransactions(
        IReadOnlyCollection<MemberPayment> memberPayments,
        Money finalOrderTotal)
    {
        var totalPaid = memberPayments.Aggregate(Money.Zero, (acc, mp) => acc + mp.Amount);
        var adjustmentFactor = finalOrderTotal / totalPaid;

        return memberPayments.Select(mp => PaymentTransaction.Create(
            mp.UserId,
            mp.Amount * adjustmentFactor,
            mp.PaymentMethod).Value).ToList();
    }
}
```

## 3. The Failing Tests: `TeamCartConversionServiceFailureTests.cs`

This test file contains scenarios where the `TeamCart` conversion is expected to fail. All tests are currently failing to compile because they call the old `ConvertToOrder` method signature.

### `TeamCartConversionServiceFailureTests.cs` (Illustrative Snippets)

```csharp
// Note: This is the original, non-compiling code.

[Theory]
[InlineData(TeamCartStatus.Open)]
[InlineData(TeamCartStatus.Converted)]
[InlineData(TeamCartStatus.Expired)]
public void ConvertToOrder_Should_Fail_When_TeamCart_Is_In_Invalid_Status(TeamCartStatus status)
{
    // Arrange
    var teamCart = TeamCartTestHelpers.CreateTeamCartWithStatus(status);
    // ... other arrangements

    // Act
    var result = _sut.ConvertToOrder(teamCart, // ... other params); // COMPILE ERROR HERE

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Should().Be(TeamCartErrors.InvalidStatusForConversion(status));
}

[Fact]
public void ConvertToOrder_Should_Fail_When_Financial_Calculation_Fails()
{
    // Arrange
    var teamCart = TeamCartTestHelpers.CreateReadyToConfirmTeamCart();
    var coupon = CouponTestHelpers.CreateActiveCoupon();
    SetupFailedFinancialCalculation(coupon); // Mocks the financial service to fail

    // Act
    var result = _sut.ConvertToOrder(teamCart, // ... other params); // COMPILE ERROR HERE

    // Assert
    result.IsFailure.Should().BeTrue();
    result.Error.Should().Be(CouponErrors.NotApplicable);
}
```

## 4. Supporting Code and Definitions

### `TeamCart` Aggregate
The `TeamCart` holds items for a group order. Its status must be `ReadyToConfirm` for conversion. After conversion, its status becomes `Converted`.

### `Order` Aggregate
The `Order` is created by the service. Its `Create` method requires all financial details to be passed in, as it does not perform calculations itself.

### `TeamCartTestHelpers.cs`
This class provides factory methods to create `TeamCart` instances in various states for testing purposes (e.g., `CreateReadyToConfirmTeamCart`, `CreateExpiredTeamCart`).

### Error Definitions
Errors like `TeamCartErrors.InvalidStatusForConversion` and `CouponErrors.NotApplicable` are custom error types used to return specific failure reasons from the service.

## 5. Desired Outcome: The Fix

The goal is to make the tests in `TeamCartConversionServiceFailureTests.cs` compile and pass. This requires updating all calls to `_sut.ConvertToOrder` to match the new method signature:

**New Signature:**
```csharp
public Result<(Order Order, TeamCart TeamCart)> ConvertToOrder(
    TeamCart teamCart,
    DeliveryAddress deliveryAddress,
    string specialInstructions,
    Coupon? coupon,
    int currentUserCouponUsageCount,
    Money deliveryFee,
    Money taxAmount)
```

Each test needs to be updated to provide the required arguments. For failure tests, many of these can be dummy values, as the test is designed to fail before the full logic is executed. For example, when testing for an invalid `TeamCart` status, the values for `deliveryAddress`, `coupon`, etc., are irrelevant.

## 6. Conclusion

The compilation errors are a direct result of the `TeamCartConversionService` refactoring. By updating the test methods to use the new `ConvertToOrder` signature and providing the necessary arguments, the tests can be fixed to correctly validate the failure scenarios of the conversion logic.