### A General Guide to Debugging Domain Service Integration Tests

This guide provides a systematic approach to debugging test failures in the `TeamCartConversionService`, which integrates with the real `OrderFinancialService`. The core principle is to **follow the data and the `Result` object** through the service layers to pinpoint where the actual outcome diverges from the test's expectation.

#### Step 1: Isolate the Failing Test and Formulate a Hypothesis

1.  **Isolate:** Focus on a single failing test. Running the entire test suite can create confusing, interleaved output.
2.  **State the Goal:** Clearly articulate what the test is trying to prove. For example: "This test should prove that conversion fails when an expired coupon is used."
3.  **Formulate a Hypothesis:** Based on the failure message (e.g., `Expected... IsFalse but found True`), form a preliminary hypothesis.
    *   *If the test expected a failure but got success:* "My `Arrange` step is likely not creating the specific data conditions (e.g., an expired coupon, a zero-payment cart) needed to trigger the failure path in the real service logic."
    *   *If the test expected success but got a failure:* "An unexpected error is being triggered. I need to trace the `Result` object to see which validation is failing and why."

#### Step 2: Add Debug Trace Points to Key Locations

Place `Console.WriteLine` statements at critical decision points to create a "breadcrumb trail" of the execution. This allows you to see the state of key variables without a full debugger.

**1. In the Service Under Test (`TeamCartConversionService.cs`):**
Trace the high-level flow and the inputs/outputs of its dependencies.

```csharp
// In TeamCartConversionService.cs - ConvertToOrder method

public Result<(Order Order, TeamCart TeamCart)> ConvertToOrder(...)
{
    Console.WriteLine($"\n--- [DEBUG] Starting ConvertToOrder for TeamCart: {teamCart.Id.Value} ---");
    Console.WriteLine($"[DEBUG] TeamCart Status: {teamCart.Status}");

    // Trace inputs to financial service
    Console.WriteLine($"[DEBUG] ==> Calling financial service with Subtotal Base, Coupon: {coupon?.Id.Value ?? "None"}");
    var subtotal = _financialService.CalculateSubtotal(orderItems);
    // ...
    var totalAmount = _financialService.CalculateFinalTotal(...);
    Console.WriteLine($"[DEBUG] <== Financial service calculated: Subtotal={subtotal.Amount}, Discount={discountAmount.Amount}, FinalTotal={totalAmount.Amount}");

    // Trace the result of each major internal step
    Console.WriteLine("[DEBUG] ==> Calling CreateSucceededPaymentTransactions...");
    var paymentTransactionsResult = CreateSucceededPaymentTransactions(teamCart, totalAmount);
    Console.WriteLine($"[DEBUG] <== CreateSucceededPaymentTransactions Result: IsFailure={paymentTransactionsResult.IsFailure}");
    if (paymentTransactionsResult.IsFailure)
    {
        Console.WriteLine($"[DEBUG] !!! Conversion failed at payment creation. Error: {paymentTransactionsResult.Error.Code}");
        return ...;
    }

    Console.WriteLine("[DEBUG] ==> Calling Order.Create...");
    var orderResult = Order.Create(...);
    Console.WriteLine($"[DEBUG] <== Order.Create Result: IsFailure={orderResult.IsFailure}");
    if (orderResult.IsFailure)
    {
        Console.WriteLine($"[DEBUG] !!! Conversion failed at order creation. Error: {orderResult.Error.Code}");
        return ...;
    }
    
    Console.WriteLine("[DEBUG] --- Conversion Succeeded ---");
    return (orderResult.Value, teamCart);
}
```

**2. In the Dependency (`OrderFinancialService.cs`):**
Trace the internal logic to understand *why* it's succeeding or failing.

```csharp
// In OrderFinancialService.cs - ValidateAndCalculateDiscount method

public virtual Result<Money> ValidateAndCalculateDiscount(...)
{
    Console.WriteLine($"\n--- [DEBUG] Inside ValidateAndCalculateDiscount for Coupon: {coupon.Id.Value} ---");
    var now = currentTime ?? DateTime.UtcNow;
    Console.WriteLine($"[DEBUG] Current Time: {now}, Coupon End Date: {coupon.ValidityEndDate}");

    if (now > coupon.ValidityEndDate) 
    {
        Console.WriteLine("[DEBUG] !!! Coupon validation failed: Expired.");
        return Result.Failure<Money>(CouponErrors.CouponExpired);
    }
    // ... add traces for other validation rules ...
    
    Console.WriteLine("[DEBUG] Coupon validation succeeded.");
    return ...;
}
```

**3. In Private Helper Methods (e.g., `CreateSucceededPaymentTransactions`):**
These are often where complex logic resides. Trace the key calculations.

```csharp
// In TeamCartConversionService.cs - CreateSucceededPaymentTransactions method

private Result<List<PaymentTransaction>> CreateSucceededPaymentTransactions(...)
{
    Console.WriteLine("\n--- [DEBUG] Inside CreateSucceededPaymentTransactions ---");
    var totalPaidByMembers = teamCart.MemberPayments.Sum(p => p.Amount.Amount);
    Console.WriteLine($"[DEBUG] Target Order Total: {totalAmount.Amount}");
    Console.WriteLine($"[DEBUG] Sum of Member Payments: {totalPaidByMembers}");
    
    // ... trace adjustment factor and final sum ...
}
```

#### Step 3: Run the Test and Analyze Output

Run the single, isolated failing test from the command line to get a clean, focused output.

```bash
dotnet test tests/Domain.UnitTests/Domain.UnitTests.csproj --filter "NameOfFailingTest"
```

Carefully read the console output. Follow the "breadcrumb trail" you created.
*   Where does the execution path diverge from what you expected?
*   Are the values of key variables (like `totalAmount`, `discountAmount`, `adjustmentFactor`) what you anticipated?
*   Is a validation check passing when it should fail, or vice-versa?

#### Step 4: Implement Fixes Based on Findings

Analyze the discrepancy between the debug output and your expectation. The fix will fall into one of three categories:

1.  **The Test Setup is Incorrect:** The `Arrange` phase of the test does not correctly create the conditions for the desired outcome.
    *   **Action:** Modify the test's setup logic. For example, to test an expired coupon, ensure the coupon object's `ValidityEndDate` is actually in the past. To test a payment mismatch, you might need to use reflection to put the `TeamCart` into an inconsistent state that bypasses its own protective business rules.

2.  **The Production Code has a Bug:** The logic in the domain service or its dependency is not correctly handling the scenario.
    *   **Action:** Modify the method in `TeamCartConversionService.cs` or `OrderFinancialService.cs`. For instance, if the payment adjustment logic was flawed, you would correct the calculation there.

3.  **The Test is Invalid:** The test is trying to verify a scenario that the system is specifically designed to prevent or handle gracefully. The production code is correct, and the test's expectation is wrong.
    *   **Example:** A test expects a failure when member payments don't exactly sum to the final total, but the service is *designed* to use an `adjustmentFactor` to correct this. The "failure" the test looks for can never happen.
    *   **Action:**
        *   **Rewrite the test** to assert the *correct, successful* behavior (e.g., "it should successfully adjust payments and pass").
        *   **Delete the test** if it no longer provides value or tests an impossible state.
        *   **Create a new test** for a *different, valid* failure condition that you discovered during your analysis.

After applying a fix, re-run the fixed test to confirm it now passes, then remove the temporary `Console.WriteLine` statements.