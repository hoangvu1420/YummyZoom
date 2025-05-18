using YummyZoom.Domain.TodoListAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Domain.TodoListAggregate.ValueObjects;

public sealed class TodoListId : AggregateRootId<Guid>
{
    public override Guid Value { get; protected set; }

    private TodoListId(Guid value)
    {
        Value = value;
    }

    public static TodoListId CreateUnique()
    {
        return new TodoListId(Guid.NewGuid());
    }

    public static TodoListId Create(Guid value)
    {
        return new TodoListId(value);
    }

    public static Result<TodoListId> Create(string value)
    {
        return Guid.TryParse(value, out var guid)
            ? Result.Success(new TodoListId(guid))
            : Result.Failure<TodoListId>(TodoListErrors.InvalidTodoListId(value));
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

#pragma warning disable CS8618
    // For EF Core
    private TodoListId()
    {
    }
#pragma warning restore CS8618
}
