using Dapper;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Common.Security;
using YummyZoom.Application.Orders.Queries.Common;
using YummyZoom.Application.Payouts.Queries.Common;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.Payouts.Queries.ListPayouts;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record ListPayoutsQuery(
    Guid RestaurantGuid,
    string? Status,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int PageNumber,
    int PageSize) : IRequest<Result<PaginatedList<PayoutSummaryDto>>>, IRestaurantQuery
{
    RestaurantId IRestaurantQuery.RestaurantId => RestaurantId.Create(RestaurantGuid);
}

public sealed class ListPayoutsQueryHandler : IRequestHandler<ListPayoutsQuery, Result<PaginatedList<PayoutSummaryDto>>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<ListPayoutsQueryHandler> _logger;

    public ListPayoutsQueryHandler(
        IDbConnectionFactory dbConnectionFactory,
        ILogger<ListPayoutsQueryHandler> logger)
    {
        _dbConnectionFactory = dbConnectionFactory ?? throw new ArgumentNullException(nameof(dbConnectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<PaginatedList<PayoutSummaryDto>>> Handle(ListPayoutsQuery request, CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        const string selectColumns = """
            p."Id" AS PayoutId,
            p."Amount_Amount" AS Amount,
            p."Amount_Currency" AS Currency,
            p."Status" AS Status,
            p."RequestedAt" AS RequestedAt,
            p."CompletedAt" AS CompletedAt,
            p."FailedAt" AS FailedAt
            """;

        const string fromAndWhere = """
            FROM "Payouts" p
            WHERE p."RestaurantId" = @RestaurantId
              AND (@Status IS NULL OR p."Status" = @Status)
              AND (@From IS NULL OR p."RequestedAt" >= @From)
              AND (@To IS NULL OR p."RequestedAt" <= @To)
            """;

        var orderByClause = "p.\"RequestedAt\" DESC, p.\"Id\" DESC";

        var (countSql, pageSql) = DapperPagination.BuildPagedSql(
            selectColumns,
            fromAndWhere,
            orderByClause,
            request.PageNumber,
            request.PageSize);

        var parameters = new
        {
            RestaurantId = request.RestaurantGuid,
            request.Status,
            request.From,
            request.To
        };

        var page = await connection.QueryPageAsync<PayoutSummaryDto>(
            countSql,
            pageSql,
            parameters,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        _logger.LogInformation(
            "Retrieved {Returned} payouts for restaurant {RestaurantId} (page {Page}/{Size})",
            page.Items.Count,
            request.RestaurantGuid,
            request.PageNumber,
            request.PageSize);

        return Result.Success(page);
    }
}
