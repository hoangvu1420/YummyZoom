using YummyZoom.Application.Common.Models;
using YummyZoom.Domain.MenuEntity;
using YummyZoom.Domain.TodoListAggregate;

namespace YummyZoom.Application.Common.Interfaces.IRepositories;

public interface IApplicationDbContext
{
    DbSet<ProcessedWebhookEvent> ProcessedWebhookEvents { get; }

    DbSet<TodoList> TodoLists { get; }
    DbSet<Menu> Menus { get; }
    DbSet<MenuCategory> MenuCategories { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
