using FluentAssertions.Primitives;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.FunctionalTests.Common;

public static class ResultAssertions
{
    public static void ShouldBeSuccessful<T>(this Result<T> result)
    {
        result.IsSuccess.Should().BeTrue("the operation should have succeeded");
    }

    public static void ShouldBeFailure<T>(this Result<T> result, string? errorCode = null)
    {
        result.IsSuccess.Should().BeFalse("the operation should have failed");
        if (errorCode != null)
            result.Error.Code.Should().Be(errorCode, $"the error code should be '{errorCode}'");
    }

    public static T ValueOrFail<T>(this Result<T> result)
    {
        result.ShouldBeSuccessful();
        return result.Value;
    }
    
    public static ObjectAssertions ShouldHaveValue<T>(this Result<T> result)
    {
        result.ShouldBeSuccessful();
        return result.Value.Should();
    }

    public static void ShouldBeSuccessful(this Result result)
    {
        result.IsSuccess.Should().BeTrue("the operation should have succeeded");
    }
}
