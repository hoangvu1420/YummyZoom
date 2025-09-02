
we just implemented the code and tests follow the [Universal_Search_MVP.md](Docs/Future-Plans/Universal_Search_MVP.md). now debug the failing test: 

Expected res.Value.Items to contain only items matching Equals(x.Cuisine, "Italian", OrdinalIgnoreCase), but the collection is empty.
   at FluentAssertions.Execution.LateBoundTestFramework.Throw(String message)
   at FluentAssertions.Execution.TestFrameworkProvider.Throw(String message)
   at FluentAssertions.Execution.DefaultAssertionStrategy.HandleFailure(String message)
   at FluentAssertions.Execution.AssertionScope.FailWith(Func`1 failReasonFunc)
   at FluentAssertions.Execution.AssertionScope.FailWith(Func`1 failReasonFunc)
   at FluentAssertions.Execution.AssertionScope.FailWith(String message, Object[] args)
   at FluentAssertions.Execution.GivenSelector`1.FailWith(String message, Object[] args)
   at FluentAssertions.Execution.GivenSelector`1.FailWith(String message)
   at FluentAssertions.Collections.GenericCollectionAssertions`3.OnlyContain(Expression`1 predicate, String because, Object[] becauseArgs)
   at YummyZoom.Application.FunctionalTests.Features.Search.UniversalSearchTests.CuisineFilter_IncludesOnlyRequested() in E:\source\repos\CA\YummyZoom\tests\Application.FunctionalTests\Features\Search\UniversalSearchTests.cs:line 86
   at NUnit.Framework.Internal.TaskAwaitAdapter.GenericAdapter`1.BlockUntilCompleted()
   at NUnit.Framework.Internal.MessagePumpStrategy.NoMessagePumpStrategy.WaitForCompletion(AwaitAdapter awaiter)
   at NUnit.Framework.Internal.AsyncToSyncAdapter.Await(Func`1 invoke)
   at NUnit.Framework.Internal.Commands.TestMethodCommand.RunTestMethod(TestExecutionContext context)
   at NUnit.Framework.Internal.Commands.TestMethodCommand.Execute(TestExecutionContext context)
   at NUnit.Framework.Internal.Commands.BeforeAndAfterTestCommand.<>c__DisplayClass1_0.<Execute>b__0()
   at NUnit.Framework.Internal.Commands.DelegatingTestCommand.RunTestMethodInThreadAbortSafeZone(TestExecutionContext context, Action action)

-----

Recent changes: added the src\Infrastructure\Data\Migrations\20250902074034_AddSearchIndexItemsTsvTriggers.cs migration that define trigger function to update the ts columns instead of defining the computed column like in the plan, to avoid some pgsql exception.
Analyze and debug the test.
run the test using dotnet test command with the right test project and filter for the specific test. you can consider to add debug traces with writeline in the test and the code implementation to understand and identify the issue.