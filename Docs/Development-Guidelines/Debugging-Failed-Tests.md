### The YummyZoom Guide to Debugging Failed Tests

This guide provides a systematic, three-step process for debugging any failed test in the YummyZoom project, whether it's a focused **Unit Test** with mocks or a broader **Integration Test** with real dependencies.

The core principle is always the same: **Isolate, Trace, and Analyze.**

---

### Step 1: Isolate the Test & Formulate a Hypothesis

Before writing any code, understand the failure.

1.  **Isolate the Target:** Run *only the single failing test* from your IDE or the command line. This provides a clean, focused output without noise from other tests.
    ```bash
    # Example:
    dotnet test path/to/test/project/YourTestProject.csproj --filter "NameOfFailingTest"
    ```

2.  **State the Test's Goal:** In one sentence, articulate what the test is supposed to prove.
    *   *Unit Test Example:* "This test proves the `Order.Cancel` method fails if the order status is already `Delivered`."
    *   *Integration Test Example:* "This test proves the `TeamCartConversionService` fails when a genuinely expired coupon is used."

3.  **Formulate a Hypothesis:** Based on the failure message, make an educated guess about the root cause.
    *   **If the test expected FAILURE but got SUCCESS:**
        *   *Hypothesis (Unit Test):* "My mock setup is wrong. `mock.Setup(...).Returns(Result.Failure(...))` is not being triggered, or the parameters don't match the actual call."
        *   *Hypothesis (Integration Test):* "My `Arrange` step didn't create the right conditions. The real dependency (e.g., `OrderFinancialService`) is correctly handling the input I gave it, which wasn't a true failure case."

    *   **If the test expected SUCCESS but got FAILURE:**
        *   *Hypothesis (Unit Test):* "The method under test is returning a `Failure` result unexpectedly. I need to trace its internal logic."
        *   *Hypothesis (Integration Test):* "A real dependency is unexpectedly returning a `Failure` result. I need to trace the data flowing into it."

### Step 2: Trace the Execution Flow with Debug Lines

Add temporary `Console.WriteLine` statements to create a "breadcrumb trail" of the execution path. This is often faster and more effective than a step-by-step debugger for understanding logical flow.

1.  **In the Method Under Test:** Trace entry/exit points and the results of key operations.
    ```csharp
    // Example in a method under test
    public Result MyMethod(...)
    {
        Console.WriteLine($"\n--- [DEBUG] Entering MyMethod with param: {param.Value} ---");
        
        // For Unit Tests: Trace the call to the mock
        Console.WriteLine("[DEBUG] ==> Calling dependency.DoWork...");
        var dependencyResult = _dependency.DoWork(...);
        Console.WriteLine($"[DEBUG] <== Dependency returned: IsFailure={dependencyResult.IsFailure}");

        if (dependencyResult.IsFailure) { /* ... */ }

        Console.WriteLine("[DEBUG] --- MyMethod Succeeded ---");
        return Result.Success();
    }
    ```

2.  **In Real Dependencies (for Integration Tests):** Add trace points inside the dependency's methods to see *why* it's producing its result.
    ```csharp
    // Example in a real dependency like OrderFinancialService
    public Result<Money> ValidateAndCalculateDiscount(...)
    {
        Console.WriteLine($"\n--- [DEBUG] Inside ValidateAndCalculateDiscount for Coupon: {coupon.Id.Value} ---");
        var now = currentTime ?? DateTime.UtcNow;
        Console.WriteLine($"[DEBUG] Current Time: {now}, Coupon End Date: {coupon.ValidityEndDate}");

        if (now > coupon.ValidityEndDate) 
        {
            Console.WriteLine("[DEBUG] !!! Coupon validation failed: Expired.");
            return Result.Failure<Money>(CouponErrors.CouponExpired);
        }
        // ...
    }
    ```

### Step 3: Analyze the Output and Implement the Fix

Re-run the isolated test and carefully analyze the debug output. Compare the actual execution flow and variable states to your expectations. The fix will fall into one of these three categories:

1.  **The Test `Arrange` Step is Incorrect:** The test setup is flawed.
    *   **Unit Test Symptom:** The debug log shows that the parameters in the actual call to a mock (`_dependency.DoWork(param1)`) do not match the parameters in the `mock.Setup(...)` expression.
    *   **Integration Test Symptom:** The debug log shows that the real dependency received valid data and correctly returned a success result, even though the test intended to create a failure condition.
    *   **Action:** **Fix the test setup.** Adjust the `Arrange` block to create the precise conditions needed. This may involve correcting mock parameters or creating more realistic input data for real dependencies.

2.  **The Production Code has a Bug:** The logic in the method under test (or its real dependency) is incorrect.
    *   **Symptom:** The debug log clearly shows the code taking an incorrect logical path or performing a wrong calculation, leading to an unexpected `Success` or `Failure`.
    *   **Action:** **Fix the production code.** Correct the logic in the relevant domain aggregate, service, or entity.

3.  **The Test's Expectation is Invalid:** The test asserts an outcome that the system is correctly designed to prevent. The production code is right, and the test is wrong.
    *   **Symptom:** The debug log shows the system behaving exactly as designed (e.g., gracefully handling a potential error), but the test `Assert` block expects a hard failure.
    *   **Action:** **Fix the test's assertion.**
        *   **Rewrite the test** to assert the *correct, actual* behavior.
        *   Or, **delete the test** if it no longer provides value.
        *   Or, **create a new test** for a *different, valid* failure condition that you discovered during your analysis.

Finally, after the test passes, **remove all temporary `Console.WriteLine` statements** to keep the codebase clean.