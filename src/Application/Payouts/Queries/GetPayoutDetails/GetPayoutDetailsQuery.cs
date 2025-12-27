using Dapper;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Security;
using YummyZoom.Application.Payouts.Queries.Common;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Application.Payouts.Queries.GetPayoutDetails;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record GetPayoutDetailsQuery(Guid RestaurantGuid, Guid PayoutId)
    : IRequest<Result<PayoutDetailsDto>>, IRestaurantQuery
{
    RestaurantId IRestaurantQuery.RestaurantId => RestaurantId.Create(RestaurantGuid);
}

public sealed class GetPayoutDetailsQueryHandler : IRequestHandler<GetPayoutDetailsQuery, Result<PayoutDetailsDto>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<GetPayoutDetailsQueryHandler> _logger;

    public GetPayoutDetailsQueryHandler(
        IDbConnectionFactory dbConnectionFactory,
        ILogger<GetPayoutDetailsQueryHandler> logger)
    {
        _dbConnectionFactory = dbConnectionFactory ?? throw new ArgumentNullException(nameof(dbConnectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<PayoutDetailsDto>> Handle(GetPayoutDetailsQuery request, CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        const string sql = """
            SELECT
                p."Id" AS PayoutId,
                p."RestaurantId" AS RestaurantId,
                p."RestaurantAccountId" AS RestaurantAccountId,
                p."Amount_Amount" AS Amount,
                p."Amount_Currency" AS Currency,
                p."Status" AS Status,
                p."RequestedAt" AS RequestedAt,
                p."CompletedAt" AS CompletedAt,
                p."FailedAt" AS FailedAt,
                p."ProviderReferenceId" AS ProviderReferenceId,
                p."FailureReason" AS FailureReason,
                p."IdempotencyKey" AS IdempotencyKey
            FROM "Payouts" p
            WHERE p."Id" = @PayoutId AND p."RestaurantId" = @RestaurantId
            """;

        var payout = await connection.QuerySingleOrDefaultAsync<PayoutDetailsDto>(
            new CommandDefinition(sql, new { request.PayoutId, RestaurantId = request.RestaurantGuid }, cancellationToken: cancellationToken));

        if (payout is null)
        {
            return Result.Failure<PayoutDetailsDto>(GetPayoutDetailsErrors.NotFound);
        }

        _logger.LogInformation(
            "Retrieved payout {PayoutId} for restaurant {RestaurantId}",
            request.PayoutId,
            request.RestaurantGuid);

        return Result.Success(payout);
    }
}

public static class GetPayoutDetailsErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "GetPayoutDetails.NotFound", "Payout not found.");
}
