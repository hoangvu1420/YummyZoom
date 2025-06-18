using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.TodoListAggregate.Enums;
using YummyZoom.SharedKernel;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.TodoLists.Queries.GetTodos;

[Authorize]
public record GetTodosQuery : IRequest<Result<TodosVm>>;

public class GetTodosQueryHandler : IRequestHandler<GetTodosQuery, Result<TodosVm>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetTodosQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<TodosVm>> Handle(GetTodosQuery request, CancellationToken cancellationToken)
    {
        var vm = new TodosVm
        {
            PriorityLevels = Enum.GetValues<PriorityLevel>()
                .Cast<PriorityLevel>()
                .Select(p => new LookupDto { Id = (int)p, Title = p.ToString() })
                .ToList(),

            Lists = await _context.TodoLists
                .AsNoTracking()
                .ProjectTo<TodoListDto>(_mapper.ConfigurationProvider)
                .OrderBy(t => t.Title)
                .ToListAsync(cancellationToken)
        };
        
        return Result.Success(vm);
    }
}
