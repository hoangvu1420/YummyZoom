using FluentValidation.Results;

namespace YummyZoom.Application.Common.Exceptions;

public class ValidationException : Exception
{
    public ValidationException()
        : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : base(BuildMessage(failures))
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }

    public IDictionary<string, string[]> Errors { get; }

    private static string BuildMessage(IEnumerable<ValidationFailure> failures)
    {
        if (failures == null)
        {
            return "One or more validation failures have occurred.";
        }

        var parts = failures
            .Where(f => f is not null)
            .Select(f => string.IsNullOrWhiteSpace(f.PropertyName)
                ? f.ErrorMessage
                : $"{f.PropertyName}: {f.ErrorMessage}")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();

        if (parts.Length == 0)
        {
            return "One or more validation failures have occurred.";
        }

        return $"Validation failed: {string.Join(" | ", parts)}";
    }
}
