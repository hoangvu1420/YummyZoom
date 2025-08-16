using System.Data;
using Dapper;
using YummyZoom.Application.Common.Models;

namespace YummyZoom.Application.Orders.Queries.Common;

/// <summary>
/// Helper for constructing <see cref="PaginatedList{T}"/> results using Dapper.
/// Mirrors the EF-based pagination developer experience while keeping SQL explicit & optimized.
/// Current implementation issues two queries (COUNT + page) for clarity. Can be enhanced with window
/// functions if profiling shows need.
/// </summary>
public static class DapperPagination
{
    /// <summary>
    /// Executes a paginated query using separate COUNT and page data SQL statements.
    /// </summary>
    public static async Task<PaginatedList<T>> QueryPageAsync<T>(
        this IDbConnection connection,
        string countSql,
        string pageSql,
        object parameters,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize));

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));

        if (totalCount == 0)
        {
            return new PaginatedList<T>(Array.Empty<T>(), 0, pageNumber, pageSize);
        }

        var items = await connection.QueryAsync<T>(
            new CommandDefinition(pageSql, parameters, cancellationToken: cancellationToken));
        var list = items as IReadOnlyCollection<T> ?? items.ToList();
        return new PaginatedList<T>(list, totalCount, pageNumber, pageSize);
    }

    /// <summary>
    /// Helper to build simple COUNT + page SQL pair. Expects <paramref name="fromAndWhere"/> to start with FROM.
    /// </summary>
    public static (string CountSql, string PageSql) BuildPagedSql(
        string selectColumns,
        string fromAndWhere,
        string orderByClause,
        int pageNumber,
        int pageSize,
        bool includeOrderByKeyword = false,
        bool usePostgresStyle = true)
    {
        var countSql = $"SELECT COUNT(1) {fromAndWhere}";
        var orderPrefix = includeOrderByKeyword ? string.Empty : "ORDER BY ";
        var offset = (pageNumber - 1) * pageSize;
        var paginationSyntax = usePostgresStyle
            ? $"LIMIT {pageSize} OFFSET {offset}"
            : $"/* adapt pagination syntax for other providers */";
        var pageSql = $"SELECT {selectColumns} {fromAndWhere} {orderPrefix}{orderByClause} {paginationSyntax}";
        return (countSql, pageSql);
    }
}
