using YummyZoom.Domain.TodoListAggregate;

namespace YummyZoom.Application.Common.Interfaces.IRepositories;

public interface IApplicationDbContext
{
    DbSet<TodoList> TodoLists { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
