using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using YummyZoom.SharedKernel;

namespace YummyZoom.Web.Infrastructure
{
    internal static class CustomResults
    {
        public static IResult Problem(Result result)
        {
            return result.Error.Type switch
            {
                ErrorType.Validation => CreateValidationProblem(result.Error),
                ErrorType.NotFound => CreateNotFoundProblem(result.Error),
                ErrorType.Conflict => CreateConflictProblem(result.Error),
                ErrorType.Failure => CreateFailureProblem(result.Error),
                ErrorType.Problem => CreateProblemResult(result.Error),
                _ => CreateServerErrorProblem(result.Error)
            };
        }

        public static IResult Problem<T>(Result<T> result)
        {
            return Problem((Result)result);
        }

        private static IResult CreateValidationProblem(Error error)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = GetTitle(error),
                Detail = error.Description
            };

            return Results.Problem(problemDetails);
        }

        private static IResult CreateNotFoundProblem(Error error)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                Title = GetTitle(error),
                Detail = error.Description
            };

            return Results.Problem(problemDetails);
        }

        private static IResult CreateConflictProblem(Error error)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                Title = GetTitle(error),
                Detail = error.Description
            };

            return Results.Problem(problemDetails);
        }

        private static IResult CreateFailureProblem(Error error)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = GetTitle(error),
                Detail = error.Description
            };

            return Results.Problem(problemDetails);
        }

        private static IResult CreateProblemResult(Error error)
        {
            // Handle OTP throttling and lockout with special status codes and Retry-After headers
            if (error.Code == "Otp.Throttled" || error.Code == "Otp.LockedOut")
            {
                var statusCode = error.Code == "Otp.LockedOut" 
                    ? StatusCodes.Status423Locked 
                    : StatusCodes.Status429TooManyRequests;

                var problemDetails = new ProblemDetails
                {
                    Status = statusCode,
                    Type = statusCode == StatusCodes.Status423Locked 
                        ? "https://tools.ietf.org/html/rfc4918#section-11.3"
                        : "https://tools.ietf.org/html/rfc6585#section-4",
                    Title = GetTitle(error),
                    Detail = error.Description
                };

                var result = Results.Problem(problemDetails);

                // Extract retry-after seconds from error description if present
                var retryAfterSeconds = ExtractRetryAfterFromDescription(error.Description);
                if (retryAfterSeconds > 0)
                {
                    // Create a custom result that adds the Retry-After header
                    return new RetryAfterResult(result, retryAfterSeconds);
                }

                return result;
            }

            // Default problem handling for other Problem type errors
            var defaultProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = GetTitle(error),
                Detail = error.Description
            };

            return Results.Problem(defaultProblemDetails);
        }

        private static IResult CreateServerErrorProblem(Error error)
        {
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = GetTitle(error),
                Detail = error.Description
            };

            return Results.Problem(problemDetails);
        }

        private static string GetTitle(Error error)
        {
            return error.Code.Length > 0 ? error.Code : "Error";
        }

        private static int ExtractRetryAfterFromDescription(string description)
        {
            // Extract seconds from messages like "Please try again in 60 seconds."
            var match = Regex.Match(description, @"(\d+)\s+seconds?", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var seconds))
            {
                return seconds;
            }

            // Extract minutes and convert to seconds from messages like "Account locked for 5 minutes."
            var minutesMatch = Regex.Match(description, @"(\d+)\s+minutes?", RegexOptions.IgnoreCase);
            if (minutesMatch.Success && int.TryParse(minutesMatch.Groups[1].Value, out var minutes))
            {
                return minutes * 60;
            }

            return 0;
        }
    }

    /// <summary>
    /// Custom IResult that adds a Retry-After header to the response
    /// </summary>
    internal class RetryAfterResult : IResult
    {
        private readonly IResult _innerResult;
        private readonly int _retryAfterSeconds;

        public RetryAfterResult(IResult innerResult, int retryAfterSeconds)
        {
            _innerResult = innerResult;
            _retryAfterSeconds = retryAfterSeconds;
        }

        public async Task ExecuteAsync(HttpContext httpContext)
        {
            // Add the Retry-After header
            httpContext.Response.Headers.RetryAfter = _retryAfterSeconds.ToString();
            
            // Execute the inner result
            await _innerResult.ExecuteAsync(httpContext);
        }
    }

    /// <summary>
    /// Custom IResult that adds an X-YummyZoom-Environment header to the response
    /// </summary>
    internal class EnvironmentHeaderResult : IResult
    {
        private readonly IResult _innerResult;
        private readonly string _environmentName;

        public EnvironmentHeaderResult(IResult innerResult, string environmentName)
        {
            _innerResult = innerResult;
            _environmentName = environmentName;
        }

        public async Task ExecuteAsync(HttpContext httpContext)
        {
            // Add the environment header
            httpContext.Response.Headers["X-YummyZoom-Environment"] = _environmentName;
            
            // Execute the inner result
            await _innerResult.ExecuteAsync(httpContext);
        }
    }
}
