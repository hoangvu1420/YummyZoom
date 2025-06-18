using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.Common.Models;

namespace YummyZoom.Infrastructure.Data.Interceptors;

public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly IUser _user;
    private readonly TimeProvider _dateTime;

    public AuditableEntityInterceptor(
        IUser user,
        TimeProvider dateTime)
    {
        _user = user;
        _dateTime = dateTime;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public void UpdateEntities(DbContext? context)
    {
        if (context == null) return;

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified || entry.HasChangedOwnedEntities())
            {
                var utcNow = _dateTime.GetUtcNow();
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedBy = _user.Id;
                    entry.Entity.Created = utcNow;

                    // Handle self-registration scenario for User entity
                    if (entry.Entity is YummyZoom.Domain.UserAggregate.User userEntity && string.IsNullOrEmpty(entry.Entity.CreatedBy))
                    {
                        entry.Entity.CreatedBy = userEntity.Id.Value.ToString();
                        entry.Entity.LastModifiedBy = userEntity.Id.Value.ToString(); // Also set LastModifiedBy to self
                    }
                    else
                    {
                        entry.Entity.LastModifiedBy = _user.Id;
                    }
                }
                else // For EntityState.Modified
                {
                    entry.Entity.LastModifiedBy = _user.Id;
                }
                entry.Entity.LastModified = utcNow;
            }
        }
    }
}

public static class Extensions
{
    public static bool HasChangedOwnedEntities(this EntityEntry entry) =>
        entry.References.Any(r => 
            r.TargetEntry != null && 
            r.TargetEntry.Metadata.IsOwned() && 
            (r.TargetEntry.State == EntityState.Added || r.TargetEntry.State == EntityState.Modified));
}
