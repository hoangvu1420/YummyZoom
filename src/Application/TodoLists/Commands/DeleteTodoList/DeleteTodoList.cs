using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;
using YummyZoom.Domain.TodoListAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TodoLists.Commands.DeleteTodoList;

public record DeleteTodoListCommand(Guid Id) : IRequest<Result<Unit>>;

public class DeleteTodoListCommandHandler : IRequestHandler<DeleteTodoListCommand, Result<Unit>>
{
    private readonly IApplicationDbContext _context;

    public DeleteTodoListCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Unit>> Handle(DeleteTodoListCommand request, CancellationToken cancellationToken)
    {
        var id = TodoListId.Create(request.Id);
        var entity = await _context.TodoLists
            .Where(l => l.Id == id)
            .SingleOrDefaultAsync(cancellationToken);

        if (entity is null)
            return Result.Failure<Unit>(TodoListErrors.NotFound(request.Id));

        _context.TodoLists.Remove(entity);

        await _context.SaveChangesAsync(cancellationToken);
        
        return Result.Success(Unit.Value);
    }
}
