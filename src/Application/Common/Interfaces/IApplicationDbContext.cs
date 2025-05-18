using YummyZoom.Domain.TodoListAggregate;

namespace YummyZoom.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<TodoList> TodoLists { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
