using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Security;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Queries.GetActiveTeamCart;

public sealed class GetActiveTeamCartQueryHandler : IRequestHandler<GetActiveTeamCartQuery, Result<GetActiveTeamCartResponse?>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly IUser _currentUser;
    private readonly ILogger<GetActiveTeamCartQueryHandler> _logger;

    public GetActiveTeamCartQueryHandler(
        IDbConnectionFactory dbConnectionFactory,
        IUser currentUser,
        ILogger<GetActiveTeamCartQueryHandler> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<GetActiveTeamCartResponse?>> Handle(GetActiveTeamCartQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.DomainUserId is null)
        {
            return Result.Failure<GetActiveTeamCartResponse?>(Error.Validation("User.Unauthenticated", "User is not authenticated."));
        }

        var userId = _currentUser.DomainUserId.Value;

        using var connection = _dbConnectionFactory.CreateConnection();

        // Optimized query to fetch all necessary data in one go
        // We look for carts where the user is a member and the status is active (Open, Locked, ReadyToConfirm)
        const string sql = """
            SELECT
                tc."Id"                     AS "TeamCartId",
                tc."RestaurantId"           AS "RestaurantId",
                r."Name"                    AS "RestaurantName",
                r."LogoUrl"                 AS "RestaurantImage",
                tc."Status"                 AS "Status",
                tc."HostUserId"             AS "HostUserId",
                tc."CreatedAt"              AS "CreatedAt",
                tc."QuoteVersion"           AS "QuoteVersion",
                tc."MemberTotals"           AS "MemberTotalsJson",
                (
                    SELECT COALESCE(SUM(tci."Quantity"), 0)
                    FROM "TeamCartItems" tci
                    WHERE tci."TeamCartId" = tc."Id"
                )                           AS "TotalItemCount",
                (
                    SELECT COALESCE(SUM(tci."LineItemTotal_Amount"), 0)
                    FROM "TeamCartItems" tci
                    WHERE tci."TeamCartId" = tc."Id" AND tci."AddedByUserId" = @UserId
                )                           AS "MyItemsTotal",
                (
                    SELECT tci."LineItemTotal_Currency"
                    FROM "TeamCartItems" tci
                    WHERE tci."TeamCartId" = tc."Id"
                    LIMIT 1
                )                           AS "Currency"
            FROM "TeamCartMembers" tcm
            JOIN "TeamCarts" tc ON tcm."TeamCartId" = tc."Id"
            JOIN "Restaurants" r ON tc."RestaurantId" = r."Id"
            WHERE tcm."UserId" = @UserId
              AND tc."Status" IN ('Open', 'Locked', 'ReadyToConfirm')
              AND tc."ExpiresAt" > @Now
            ORDER BY tc."CreatedAt" DESC
            LIMIT 1
            """;

        var row = await connection.QuerySingleOrDefaultAsync<ActiveTeamCartRow>(
            new CommandDefinition(sql, new { UserId = userId, Now = DateTime.UtcNow }, cancellationToken: cancellationToken));

        if (row is null)
        {
            return Result.Success<GetActiveTeamCartResponse?>(null);
        }

        // Determine MyShareTotal
        decimal myShareTotal = row.MyItemsTotal;
        string currency = row.Currency ?? "USD"; // Default currency if no items yet

        // If cart is locked/quoted, try to use the official quote from MemberTotals JSON
        if (row.QuoteVersion > 0 && !string.IsNullOrWhiteSpace(row.MemberTotalsJson))
        {
            try
            {
                var memberTotals = JsonSerializer.Deserialize<List<MemberTotalRow>>(row.MemberTotalsJson);
                var myQuote = memberTotals?.FirstOrDefault(m => m.UserId == userId);
                if (myQuote != null)
                {
                    myShareTotal = myQuote.Amount;
                    currency = myQuote.Currency;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse MemberTotals JSON for TeamCart {TeamCartId}", row.TeamCartId);
                // Fallback to MyItemsTotal is already set
            }
        }

        var response = new GetActiveTeamCartResponse(
            row.TeamCartId,
            row.RestaurantId,
            row.RestaurantName,
            row.RestaurantImage,
            row.Status,
            (int)row.TotalItemCount,
            myShareTotal,
            currency,
            row.HostUserId == userId,
            row.CreatedAt
        );

        return Result.Success<GetActiveTeamCartResponse?>(response);
    }

    // INTERNAL DTOs for Dapper mapping
    private sealed record ActiveTeamCartRow(
        Guid TeamCartId,
        Guid RestaurantId,
        string RestaurantName,
        string? RestaurantImage,
        string Status,
        Guid HostUserId,
        DateTime CreatedAt,
        long QuoteVersion,
        string? MemberTotalsJson,
        long TotalItemCount,
        decimal MyItemsTotal,
        string? Currency
    );

    private sealed record MemberTotalRow(
        Guid UserId,
        decimal Amount,
        string Currency
    );
}
