using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TodoLists.Commands.CreateTodoList;

public record CreateTodoListCommand : IRequest<Result<Guid>>
{
    public string? Title { get; init; }
}

public class CreateTodoListCommandHandler : IRequestHandler<CreateTodoListCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateTodoListCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateTodoListCommand request, CancellationToken cancellationToken)
    {
        var entity = TodoList.Create(request.Title ?? string.Empty, Color.White);

        _context.TodoLists.Add(entity);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(entity.Id.Value);
    }
}
