using Microsoft.EntityFrameworkCore;
using YummyZoom.Domain.Common.Models;

namespace YummyZoom.Infrastructure.Data.Extensions;

/// <summary>
/// Extension methods for handling soft delete functionality in EF Core
/// </summary>
public static class SoftDeleteExtensions
{
    /// <summary>
    /// Ignores the soft delete query filter for the specified entity type
    /// </summary>
    /// <typeparam name="T">The entity type that implements ISoftDeletableEntity</typeparam>
    /// <param name="queryable">The queryable to modify</param>
    /// <returns>A queryable that includes soft-deleted entities</returns>
    public static IQueryable<T> IncludeSoftDeleted<T>(this IQueryable<T> queryable)
        where T : class, ISoftDeletableEntity
    {
        return queryable.IgnoreQueryFilters();
    }

    /// <summary>
    /// Filters to only soft-deleted entities
    /// </summary>
    /// <typeparam name="T">The entity type that implements ISoftDeletableEntity</typeparam>
    /// <param name="queryable">The queryable to modify</param>
    /// <returns>A queryable that only includes soft-deleted entities</returns>
    public static IQueryable<T> OnlySoftDeleted<T>(this IQueryable<T> queryable)
        where T : class, ISoftDeletableEntity
    {
        return queryable.IgnoreQueryFilters().Where(e => e.IsDeleted);
    }

    /// <summary>
    /// Gets all entities including soft-deleted ones with their deletion status
    /// </summary>
    /// <typeparam name="T">The entity type that implements ISoftDeletableEntity</typeparam>
    /// <param name="queryable">The queryable to modify</param>
    /// <returns>A queryable that includes all entities regardless of deletion status</returns>
    public static IQueryable<T> WithSoftDeleteStatus<T>(this IQueryable<T> queryable)
        where T : class, ISoftDeletableEntity
    {
        return queryable.IgnoreQueryFilters();
    }

    /// <summary>
    /// Restores a soft-deleted entity by setting IsDeleted to false
    /// </summary>
    /// <typeparam name="T">The entity type that implements ISoftDeletableEntity</typeparam>
    /// <param name="entity">The entity to restore</param>
    public static void Restore<T>(this T entity)
        where T : ISoftDeletableEntity
    {
        entity.IsDeleted = false;
        entity.DeletedOn = null;
        entity.DeletedBy = null;
    }
}
