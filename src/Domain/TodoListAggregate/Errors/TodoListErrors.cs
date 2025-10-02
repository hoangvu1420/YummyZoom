using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.TodoListAggregate.Errors;

public static class TodoListErrors
{
    public static Error NotFound(Guid todoListId)
    {
        return Error.NotFound(
        "TodoLists.NotFound",
        $"The to-do list with the Id = '{todoListId}' was not found");
    }

    public static Error InvalidTodoListId(string todoListId)
    {
        return Error.Validation(
        "TodoLists.InvalidTodoListId",
        $"The to-do list with the Id = '{todoListId}' is invalid.");
    }
}
