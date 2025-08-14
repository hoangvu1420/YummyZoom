using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Domain.TodoListAggregate.Events;

namespace YummyZoom.Application.TodoItems.EventHandlers;

public sealed class TodoItemCompletedEventHandler
    : IdempotentNotificationHandler<TodoItemCompletedEvent>
{
    private readonly ILogger<TodoItemCompletedEventHandler> _logger;

    public TodoItemCompletedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        ILogger<TodoItemCompletedEventHandler> logger) : base(uow, inbox)
        => _logger = logger;

    protected override Task HandleCore(TodoItemCompletedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("YummyZoom Domain Event: {DomainEvent}", notification.GetType().Name);

        return Task.CompletedTask;
    }
}
