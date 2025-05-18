using ErrorOr;

namespace YummyZoom.Domain.Common.Errors;

public static partial class Errors
{
    public static class Color
    {
        public static Error Unsupported => Error.Validation(
            code: "Color.Unsupported",
            description: "Color is unsupported");
    }
}
