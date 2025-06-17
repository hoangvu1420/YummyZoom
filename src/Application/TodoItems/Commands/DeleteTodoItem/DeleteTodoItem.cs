using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.TodoListAggregate.Events;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;
using YummyZoom.Domain.TodoListAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TodoItems.Commands.DeleteTodoItem;

public record DeleteTodoItemCommand(Guid ListId, Guid Id) : IRequest<Result<Unit>>;

public class DeleteTodoItemCommandHandler : IRequestHandler<DeleteTodoItemCommand, Result<Unit>>
{
    private readonly IApplicationDbContext _context;

    public DeleteTodoItemCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Unit>> Handle(DeleteTodoItemCommand request, CancellationToken cancellationToken)
    {
        var todoList = await _context.TodoLists
            .Include(l => l.Items)
            .FirstOrDefaultAsync(l => l.Id == TodoListId.Create(request.ListId), cancellationToken);

        if (todoList is null)
            return Result.Failure<Unit>(TodoListErrors.NotFound(request.ListId));

        var item = todoList.Items.FirstOrDefault(i => i.Id.Value == request.Id);
        
        if (item is null)
            return Result.Failure<Unit>(TodoItemErrors.NotFound(request.Id));

        todoList.RemoveItem(item);
        item.AddDomainEvent(new TodoItemDeletedEvent(item));

        await _context.SaveChangesAsync(cancellationToken);
        
        return Result.Success(Unit.Value);
    }
}
