using YummyZoom.Domain.TodoListAggregate.Entities;

namespace YummyZoom.Domain.TodoListAggregate.Events;

public record TodoItemCompletedEvent(TodoItem Item) : DomainEventBase;
