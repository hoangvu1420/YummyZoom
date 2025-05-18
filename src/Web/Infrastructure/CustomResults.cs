using Microsoft.AspNetCore.Mvc;
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
            var parts = error.Code.Split('.');
            return parts.Length > 0 ? parts[0] : "Error";
        }
    }
}
