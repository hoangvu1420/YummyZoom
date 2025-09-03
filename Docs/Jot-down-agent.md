the test: ReviewHidden_UpdatesSummaryAndSearchIndex is failing with the error:

System.NullReferenceException : Object reference not set to an instance of an object.
   at YummyZoom.Application.FunctionalTests.Features.Reviews.Events.ReviewSummaryProjectionTests.ReviewHidden_UpdatesSummaryAndSearchIndex() in E:\source\repos\CA\YummyZoom\tests\Application.FunctionalTests\Features\Reviews\Events\ReviewSummaryProjectionTests.cs:line 87

analyze and debug the test. read the related implementation plan of the feature in Docs\Future-Plans\Restaurant_Review_Summary_Projection.md. you can add debug traces with writeline to investigate the issue.