using YummyZoom.SharedKernel;
using YummyZoom.Domain.TodoListAggregate.Errors;

namespace YummyZoom.Domain.TodoListAggregate.ValueObjects;

public sealed class TodoItemId : AggregateRootId<Guid>
{
    public override Guid Value { get; protected set; }

    private TodoItemId(Guid value)
    {
        Value = value;
    }

    public static TodoItemId CreateUnique()
    {
        return new TodoItemId(Guid.NewGuid());
    }

    public static TodoItemId Create(Guid value)
    {
        return new TodoItemId(value);
    }

    public static Result<TodoItemId> Create(string value)
    {
        return Guid.TryParse(value, out var guid) ? 
            Result.Success(new TodoItemId(guid)) :
            Result.Failure<TodoItemId>(TodoItemErrors.InvalidTodoItemId(value));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

#pragma warning disable CS8618
    private TodoItemId()
    {
    }
#pragma warning restore CS8618
}
