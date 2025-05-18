using YummyZoom.Domain.TodoListAggregate.Entities;

namespace YummyZoom.Domain.TodoListAggregate.Events;

public record TodoItemDeletedEvent(TodoItem Item) : IDomainEvent;
