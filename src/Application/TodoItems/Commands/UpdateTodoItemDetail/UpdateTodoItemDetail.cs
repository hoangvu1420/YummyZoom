using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.TodoListAggregate.Enums;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;
using YummyZoom.Domain.TodoListAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TodoItems.Commands.UpdateTodoItemDetail;

public record UpdateTodoItemDetailCommand : IRequest<Result<Unit>>
{
    public Guid ListId { get; init; }
    public Guid Id { get; init; }
    public PriorityLevel Priority { get; init; }
    public string? Note { get; init; }
    public DateTime? Reminder { get; init; }
}

public class UpdateTodoItemDetailCommandHandler : IRequestHandler<UpdateTodoItemDetailCommand, Result<Unit>>
{
    private readonly IApplicationDbContext _context;

    public UpdateTodoItemDetailCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Unit>> Handle(UpdateTodoItemDetailCommand request, CancellationToken cancellationToken)
    {
        var todoList = await _context.TodoLists
            .Include(l => l.Items)
            .FirstOrDefaultAsync(l => l.Id == TodoListId.Create(request.ListId), cancellationToken);

        if (todoList is null)
            return Result.Failure<Unit>(TodoListErrors.NotFound(request.ListId));

        var item = todoList.Items.FirstOrDefault(i => i.Id.Value == request.Id);
        
        if (item is null)
            return Result.Failure<Unit>(TodoItemErrors.NotFound(request.Id));

        item.UpdatePriority(request.Priority);
        item.UpdateNote(request.Note);
        item.UpdateReminder(request.Reminder);

        await _context.SaveChangesAsync(cancellationToken);
        
        return Result.Success(Unit.Value);
    }
}
