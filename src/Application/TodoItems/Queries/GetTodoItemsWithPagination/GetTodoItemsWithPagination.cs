using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Models;
using YummyZoom.Domain.TodoListAggregate.Errors;
using YummyZoom.Domain.TodoListAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.TodoItems.Queries.GetTodoItemsWithPagination;

public record GetTodoItemsWithPaginationQuery : IRequest<Result<PaginatedList<TodoItemBriefDto>>>
{
    public Guid ListId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetTodoItemsWithPaginationQueryHandler : IRequestHandler<GetTodoItemsWithPaginationQuery, Result<PaginatedList<TodoItemBriefDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetTodoItemsWithPaginationQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<PaginatedList<TodoItemBriefDto>>> Handle(GetTodoItemsWithPaginationQuery request, CancellationToken cancellationToken)
    {
        var listId = TodoListId.Create(request.ListId);

        // Check if the list exists
        var listExists = await _context.TodoLists
            .AsNoTracking()
            .AnyAsync(l => l.Id == listId, cancellationToken);

        if (!listExists)
            return Result.Failure<PaginatedList<TodoItemBriefDto>>(TodoListErrors.NotFound(request.ListId));

        // Query for the items with projection and apply pagination directly on the IQueryable
        var itemsQuery = _context.TodoLists
            .AsNoTracking()
            .Where(l => l.Id == listId)
            .SelectMany(l => l.Items)
            .OrderBy(x => x.Title)
            .ProjectTo<TodoItemBriefDto>(_mapper.ConfigurationProvider);

        var paginatedList = await PaginatedList<TodoItemBriefDto>.CreateAsync(
            itemsQuery,
            request.PageNumber,
            request.PageSize);

        return Result.Success(paginatedList);
    }
}
