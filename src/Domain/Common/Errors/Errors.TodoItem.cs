using ErrorOr;

namespace YummyZoom.Domain.Common.Errors;

public static partial class Errors
{
    public static class TodoItem
    {
        public static Error InvalidTodoItemId => Error.Validation(
            code: "TodoItem.InvalidId",
            description: "TodoItem ID is invalid");

        public static Error NotFound => Error.NotFound(
            code: "TodoItem.NotFound",
            description: "TodoItem with given ID does not exist");
    }
}
