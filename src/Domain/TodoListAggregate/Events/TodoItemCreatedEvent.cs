using YummyZoom.Domain.TodoListAggregate.Entities;

namespace YummyZoom.Domain.TodoListAggregate.Events;

public record TodoItemCreatedEvent(TodoItem Item) : DomainEventBase;
