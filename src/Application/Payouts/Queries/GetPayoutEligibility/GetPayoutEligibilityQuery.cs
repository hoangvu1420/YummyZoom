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

namespace YummyZoom.Application.Payouts.Queries.GetPayoutEligibility;

[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public sealed record GetPayoutEligibilityQuery(Guid RestaurantGuid)
    : IRequest<Result<PayoutEligibilityDto>>, IRestaurantQuery
{
    RestaurantId IRestaurantQuery.RestaurantId => RestaurantId.Create(RestaurantGuid);
}

public sealed class GetPayoutEligibilityQueryHandler
    : IRequestHandler<GetPayoutEligibilityQuery, Result<PayoutEligibilityDto>>
{
    private static readonly TimeSpan WeeklyCadence = TimeSpan.FromDays(7);

    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ILogger<GetPayoutEligibilityQueryHandler> _logger;

    public GetPayoutEligibilityQueryHandler(
        IDbConnectionFactory dbConnectionFactory,
        ILogger<GetPayoutEligibilityQueryHandler> logger)
    {
        _dbConnectionFactory = dbConnectionFactory ?? throw new ArgumentNullException(nameof(dbConnectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<PayoutEligibilityDto>> Handle(GetPayoutEligibilityQuery request, CancellationToken cancellationToken)
    {
        using var connection = _dbConnectionFactory.CreateConnection();

        const string accountSql = """
            SELECT
                "CurrentBalance_Amount" AS CurrentBalanceAmount,
                "CurrentBalance_Currency" AS Currency,
                "PendingPayoutTotal_Amount" AS PendingPayoutAmount,
                "PayoutMethod_Details" AS PayoutMethodDetails
            FROM "RestaurantAccounts"
            WHERE "RestaurantId" = @RestaurantId
            """;

        var account = await connection.QuerySingleOrDefaultAsync<AccountRow>(
            new CommandDefinition(accountSql, new { RestaurantId = request.RestaurantGuid }, cancellationToken: cancellationToken));

        if (account is null)
        {
            return Result.Failure<PayoutEligibilityDto>(PayoutEligibilityErrors.AccountNotFound);
        }

        var availableAmount = account.CurrentBalanceAmount - account.PendingPayoutAmount;
        var hasPayoutMethod = !string.IsNullOrWhiteSpace(account.PayoutMethodDetails);

        const string latestCompletedSql = """
            SELECT MAX("CompletedAt")
            FROM "Payouts"
            WHERE "RestaurantId" = @RestaurantId AND "CompletedAt" IS NOT NULL
            """;

        const string latestRequestedSql = """
            SELECT MAX("RequestedAt")
            FROM "Payouts"
            WHERE "RestaurantId" = @RestaurantId
            """;

        var latestCompletedAt = await connection.ExecuteScalarAsync<DateTimeOffset?>(
            new CommandDefinition(latestCompletedSql, new { RestaurantId = request.RestaurantGuid }, cancellationToken: cancellationToken));

        var cadenceAnchor = latestCompletedAt ?? await connection.ExecuteScalarAsync<DateTimeOffset?>(
            new CommandDefinition(latestRequestedSql, new { RestaurantId = request.RestaurantGuid }, cancellationToken: cancellationToken));

        var now = DateTimeOffset.UtcNow;
        DateTimeOffset? nextEligibleAt = null;
        string? ineligibilityReason = null;

        if (!hasPayoutMethod)
        {
            ineligibilityReason = "PayoutMethodMissing";
        }
        else if (availableAmount <= 0)
        {
            ineligibilityReason = "InsufficientBalance";
        }
        else if (cadenceAnchor.HasValue && cadenceAnchor.Value.Add(WeeklyCadence) > now)
        {
            nextEligibleAt = cadenceAnchor.Value.Add(WeeklyCadence);
            ineligibilityReason = "WeeklyCadence";
        }

        var isEligible = string.IsNullOrEmpty(ineligibilityReason);

        var dto = new PayoutEligibilityDto(
            isEligible,
            availableAmount,
            account.Currency,
            hasPayoutMethod,
            nextEligibleAt,
            ineligibilityReason);

        _logger.LogInformation(
            "Payout eligibility for restaurant {RestaurantId}: eligible={Eligible} available={Available} {Currency}",
            request.RestaurantGuid,
            isEligible,
            availableAmount,
            account.Currency);

        return Result.Success(dto);
    }

    private sealed record AccountRow(
        decimal CurrentBalanceAmount,
        string Currency,
        decimal PendingPayoutAmount,
        string? PayoutMethodDetails);
}

public static class PayoutEligibilityErrors
{
    public static readonly Error AccountNotFound = Error.NotFound(
        "PayoutEligibility.AccountNotFound", "Restaurant account not found.");
}
