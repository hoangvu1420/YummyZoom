using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Common.Interfaces;

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
} 
