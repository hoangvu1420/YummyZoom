using ErrorOr;

namespace YummyZoom.Domain.Common.Errors;

public static partial class Errors
{
    public static class TodoList
    {
        public static Error InvalidTodoListId => Error.Validation(
            code: "TodoList.InvalidId",
            description: "TodoList ID is invalid");

        public static Error NotFound => Error.NotFound(
            code: "TodoList.NotFound",
            description: "TodoList with given ID does not exist");
    }
}
