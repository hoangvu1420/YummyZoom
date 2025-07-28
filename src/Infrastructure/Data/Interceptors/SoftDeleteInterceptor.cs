using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.Common.Models;

namespace YummyZoom.Infrastructure.Data.Interceptors;

/// <summary>
/// Interceptor that handles soft delete operations by converting Delete operations to Update operations
/// for entities that implement ISoftDeletableEntity
/// </summary>
public class SoftDeleteInterceptor : SaveChangesInterceptor
{
    private readonly IUser _user;
    private readonly TimeProvider _dateTime;

    public SoftDeleteInterceptor(
        IUser user,
        TimeProvider dateTime)
    {
        _user = user;
        _dateTime = dateTime;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ProcessSoftDeletes(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        ProcessSoftDeletes(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ProcessSoftDeletes(DbContext? context)
    {
        if (context == null) return;

        var utcNow = _dateTime.GetUtcNow();

        // Find all entities marked for deletion that implement ISoftDeletableEntity
        var entriesToSoftDelete = context.ChangeTracker.Entries<ISoftDeletableEntity>()
            .Where(e => e.State == EntityState.Deleted)
            .ToList();

        foreach (var entry in entriesToSoftDelete)
        {
            // Convert the delete operation to an update operation
            entry.State = EntityState.Modified;

            // Set soft delete properties
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedOn = utcNow;
            entry.Entity.DeletedBy = _user.Id;

            // Mark only the soft delete properties as modified
            entry.Property(e => e.IsDeleted).IsModified = true;
            entry.Property(e => e.DeletedOn).IsModified = true;
            entry.Property(e => e.DeletedBy).IsModified = true;
        }
    }
}
