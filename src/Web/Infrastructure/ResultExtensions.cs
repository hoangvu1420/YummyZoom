using YummyZoom.SharedKernel;

namespace YummyZoom.Web.Infrastructure
{
    internal static class ResultExtensions
    {
        public static TOut Match<TOut>(
            this Result result,
            Func<TOut> onSuccess,
            Func<Result, TOut> onFailure)
            => result.IsSuccess ? onSuccess() : onFailure(result);

        public static TOut Match<TIn, TOut>(
            this Result<TIn> result,
            Func<TIn, TOut> onSuccess,
            Func<Result<TIn>, TOut> onFailure)
            => result.IsSuccess ? onSuccess(result.Value) : onFailure(result);

        public static IResult ToIResult<T>(this Result<T> r)
            => r.Match(Results.Ok, CustomResults.Problem);

        public static Task<IResult> ToIResultAsync<T>(this Task<Result<T>> task)
            => task.ContinueWith(t => t.Result.ToIResult());

        public static IResult ToIResult(this Result r)
            => r.Match(Results.NoContent, CustomResults.Problem);
    }
}
