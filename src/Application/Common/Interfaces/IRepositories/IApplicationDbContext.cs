using YummyZoom.Application.Common.Models;
using YummyZoom.Domain.TodoListAggregate;

namespace YummyZoom.Application.Common.Interfaces.IRepositories;

public interface IApplicationDbContext
{
    DbSet<TodoList> TodoLists { get; }
    DbSet<ProcessedWebhookEvent> ProcessedWebhookEvents { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
