using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.TodoListAggregate.Errors;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TodoItems.Commands.UpdateTodoItem;

public record UpdateTodoItemCommand : IRequest<Result<Unit>>
{
    public Guid Id { get; init; }
    public Guid ListId { get; init; }
    public string? Title { get; init; }
    public bool IsDone { get; init; }
}

public class UpdateTodoItemCommandHandler : IRequestHandler<UpdateTodoItemCommand, Result<Unit>>
{
    private readonly IApplicationDbContext _context;

    public UpdateTodoItemCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Unit>> Handle(UpdateTodoItemCommand request, CancellationToken cancellationToken)
    {
        var todoList = await _context.TodoLists
            .Include(l => l.Items)
            .FirstOrDefaultAsync(l => l.Id == TodoListId.Create(request.ListId), cancellationToken);

        if (todoList is null)
            return Result.Failure<Unit>(TodoListErrors.NotFound(request.ListId));

        var item = todoList.Items.FirstOrDefault(i => i.Id.Value == request.Id);

        if (item is null)
            return Result.Failure<Unit>(TodoItemErrors.NotFound(request.Id));

        if (request.Title is not null)
            item.UpdateTitle(request.Title);

        if (request.IsDone)
            item.Complete();
        else
            item.Incomplete();

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(Unit.Value);
    }
}
