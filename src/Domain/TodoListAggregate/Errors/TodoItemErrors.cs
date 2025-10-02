using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.TodoListAggregate.Errors;

public static class TodoItemErrors
{
    public static Error AlreadyCompleted(Guid todoItemId)
    {
        return Error.Problem(
        "TodoItems.AlreadyCompleted",
        $"The todo item with Id = '{todoItemId}' is already completed.");
    }

    public static Error NotFound(Guid todoItemId)
    {
        return Error.NotFound(
        "TodoItems.NotFound",
        $"The to-do item with the Id = '{todoItemId}' was not found");
    }

    public static Error InvalidTodoItemId(string todoItemId)
    {
        return Error.Validation(
        "TodoItems.InvalidTodoItemId",
        $"The to-do item with the Id = '{todoItemId}' is invalid.");
    }
}
