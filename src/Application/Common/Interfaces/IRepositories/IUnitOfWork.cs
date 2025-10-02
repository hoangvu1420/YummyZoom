using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Common.Interfaces.IRepositories;

public interface IUnitOfWork
{
    /// <summary>
    /// Runs all work in one EF retry‐strategy + transaction,
    /// rolling back automatically on failure.
    /// </summary>
    Task<Result<T>> ExecuteInTransactionAsync<T>(
        Func<Task<Result<T>>> work,
        CancellationToken ct = default);

    /// <summary>
    /// Runs all work in one EF retry‐strategy + transaction that returns a simple Result,
    /// rolling back automatically on failure.
    /// </summary>
    Task<Result> ExecuteInTransactionAsync(
        Func<Task<Result>> work,
        CancellationToken ct = default);

    /// <summary>
    /// Saves all changes made in this context to the database.
    /// </summary>
    /// <param name="cancellationToken">A CancellationToken to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous save operation. The task result contains the number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
