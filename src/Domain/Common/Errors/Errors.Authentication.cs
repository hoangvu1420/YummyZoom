using ErrorOr;

namespace YummyZoom.Domain.Common.Errors;

public static partial class Errors
{
    public static class Authentication
    {
        public static Error InvalidCredentials => Error.Validation(
            code: "Authentication.InvalidCredentials",
            description: "Invalid email or password.");
    }
}
