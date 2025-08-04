using YummyZoom.Application.Common.Models;
using YummyZoom.Domain.TodoListAggregate;
using YummyZoom.Domain.MenuEntity;

namespace YummyZoom.Application.Common.Interfaces.IRepositories;

public interface IApplicationDbContext
{
    DbSet<TodoList> TodoLists { get; }
    DbSet<ProcessedWebhookEvent> ProcessedWebhookEvents { get; }
    DbSet<Menu> Menus { get; }
    DbSet<MenuCategory> MenuCategories { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
