using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.Common.Models;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Interceptors;

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

        var utcNow = _dateTime.GetUtcNow();

        // Handle creation auditing for entities that implement ICreationAuditable
        foreach (var entry in context.ChangeTracker.Entries<ICreationAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedBy = _user.Id;
                entry.Entity.Created = utcNow;

                // Handle self-registration scenario for User entity
                if (entry.Entity is YummyZoom.Domain.UserAggregate.User userEntity && string.IsNullOrEmpty(entry.Entity.CreatedBy))
                {
                    entry.Entity.CreatedBy = userEntity.Id.Value.ToString();
                }
            }
        }

        // Handle modification auditing for entities that implement IModificationAuditable
        foreach (var entry in context.ChangeTracker.Entries<IModificationAuditable>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified || entry.HasChangedOwnedEntities())
            {
                entry.Entity.LastModifiedBy = _user.Id;
                entry.Entity.LastModified = utcNow;

                // Handle self-registration scenario for User entity
                if (entry.State == EntityState.Added && entry.Entity is YummyZoom.Domain.UserAggregate.User userEntity && string.IsNullOrEmpty(entry.Entity.LastModifiedBy))
                {
                    entry.Entity.LastModifiedBy = userEntity.Id.Value.ToString();
                }
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
