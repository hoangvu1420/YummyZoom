using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Domain.TodoListAggregate.Events;

namespace YummyZoom.Application.TodoItems.EventHandlers;

public sealed class TodoItemCreatedEventHandler
    : IdempotentNotificationHandler<TodoItemCreatedEvent>
{
    private readonly ILogger<TodoItemCreatedEventHandler> _logger;

    public TodoItemCreatedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        ILogger<TodoItemCreatedEventHandler> logger) : base(uow, inbox)
        => _logger = logger;

    protected override Task HandleCore(TodoItemCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("YummyZoom Domain Event: {DomainEvent}", notification.GetType().Name);

        return Task.CompletedTask;
    }
}
