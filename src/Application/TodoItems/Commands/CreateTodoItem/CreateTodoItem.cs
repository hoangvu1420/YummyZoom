using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;
using YummyZoom.Domain.TodoListAggregate.Enums;
using YummyZoom.Domain.TodoListAggregate.Entities;
using YummyZoom.Domain.TodoListAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TodoItems.Commands.CreateTodoItem;

public record CreateTodoItemCommand : IRequest<Result<Guid>>
{
    public Guid ListId { get; init; }
    public string? Title { get; init; }
    public string? Note { get; init; }
    public PriorityLevel Priority { get; init; }
    public DateTime? Reminder { get; init; }
}

public class CreateTodoItemCommandHandler : IRequestHandler<CreateTodoItemCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateTodoItemCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateTodoItemCommand request, CancellationToken cancellationToken)
    {
        var listId = TodoListId.Create(request.ListId);
        var todoList = await _context.TodoLists
            .FindAsync([listId], cancellationToken);

        if (todoList is null)
            return Result.Failure<Guid>(TodoListErrors.NotFound(request.ListId));

        var todoItem = TodoItem.Create(
            request.Title,
            request.Note,
            request.Priority,
            request.Reminder);

        todoList.AddItem(todoItem);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(todoItem.Id.Value);
    }
}
