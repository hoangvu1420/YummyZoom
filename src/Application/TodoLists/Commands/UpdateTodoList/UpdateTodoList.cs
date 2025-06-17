using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;
using YummyZoom.Domain.TodoListAggregate.Errors;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TodoLists.Commands.UpdateTodoList;

public record UpdateTodoListCommand : IRequest<Result<Unit>>
{
    public Guid Id { get; init; }

    public string? Title { get; init; }
}

public class UpdateTodoListCommandHandler : IRequestHandler<UpdateTodoListCommand, Result<Unit>>
{
    private readonly IApplicationDbContext _context;

    public UpdateTodoListCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Unit>> Handle(UpdateTodoListCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.TodoLists
            .FindAsync([TodoListId.Create(request.Id)], cancellationToken);

        if (entity is null)
            return Result.Failure<Unit>(TodoListErrors.NotFound(request.Id));

        entity.UpdateTitle(request.Title ?? string.Empty);

        await _context.SaveChangesAsync(cancellationToken);
        
        return Result.Success(Unit.Value);
    }
}
