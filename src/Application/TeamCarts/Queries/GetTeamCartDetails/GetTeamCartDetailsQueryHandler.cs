using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.TeamCarts.Queries.Common;
using YummyZoom.Domain.TeamCartAggregate.Enums;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Queries.GetTeamCartDetails;

/// <summary>
/// Handler that loads a TeamCart with its members, items, and payments using SQL queries.
/// Authorization is enforced post-fetch: caller must be a member of the team cart.
/// </summary>
public sealed class GetTeamCartDetailsQueryHandler : IRequestHandler<GetTeamCartDetailsQuery, Result<GetTeamCartDetailsResponse>>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly IUser _currentUser;
    private readonly ILogger<GetTeamCartDetailsQueryHandler> _logger;

    public GetTeamCartDetailsQueryHandler(
        IDbConnectionFactory dbConnectionFactory,
        IUser currentUser,
        ILogger<GetTeamCartDetailsQueryHandler> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<GetTeamCartDetailsResponse>> Handle(GetTeamCartDetailsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.DomainUserId is null)
        {
            throw new UnauthorizedAccessException();
        }

        using var connection = _dbConnectionFactory.CreateConnection();

        // Load the main TeamCart data
        const string teamCartSql = """
            SELECT
                tc."Id"                     AS "TeamCartId",
                tc."RestaurantId"           AS "RestaurantId",
                tc."HostUserId"             AS "HostUserId",
                tc."Status"                 AS "Status",
                tc."ShareToken_Value"       AS "ShareTokenValue",
                tc."Deadline"               AS "Deadline",
                tc."CreatedAt"              AS "CreatedAt",
                tc."ExpiresAt"              AS "ExpiresAt",
                tc."TipAmount_Amount"       AS "TipAmount",
                tc."TipAmount_Currency"     AS "TipCurrency",
                tc."AppliedCouponId"        AS "AppliedCouponId"
            FROM "TeamCarts" tc
            WHERE tc."Id" = @TeamCartId
            """;

        var teamCartRow = await connection.QuerySingleOrDefaultAsync<TeamCartDetailsRow>(
            new CommandDefinition(teamCartSql, new { TeamCartId = request.TeamCartIdGuid }, cancellationToken: cancellationToken));

        if (teamCartRow is null)
        {
            _logger.LogWarning("TeamCart not found: {TeamCartId}", request.TeamCartIdGuid);
            return Result.Failure<GetTeamCartDetailsResponse>(GetTeamCartDetailsErrors.NotFound);
        }

        // Load members and check authorization
        const string membersSql = """
            SELECT
                tcm."TeamCartMemberId"  AS "TeamCartMemberId",
                tcm."UserId"            AS "UserId", 
                tcm."Name"              AS "Name",
                tcm."Role"              AS "Role"
            FROM "TeamCartMembers" tcm
            WHERE tcm."TeamCartId" = @TeamCartId
            ORDER BY tcm."TeamCartMemberId"
            """;

        var memberRows = await connection.QueryAsync<TeamCartMemberRow>(
            new CommandDefinition(membersSql, new { TeamCartId = request.TeamCartIdGuid }, cancellationToken: cancellationToken));

        var members = memberRows.Select(m => new TeamCartMemberDto(
            m.TeamCartMemberId,
            m.UserId,
            m.Name,
            m.Role
        )).ToList();

        // Authorization check: user must be a member
        var currentUserId = _currentUser.DomainUserId.Value;
        if (!members.Any(m => m.UserId == currentUserId))
        {
            _logger.LogWarning("User {UserId} is not a member of TeamCart {TeamCartId}", currentUserId, request.TeamCartIdGuid);
            return Result.Failure<GetTeamCartDetailsResponse>(GetTeamCartDetailsErrors.NotMember);
        }

        // Load items
        const string itemsSql = """
            SELECT
                tci."TeamCartItemId"                AS "TeamCartItemId",
                tci."AddedByUserId"                 AS "AddedByUserId",
                tci."Snapshot_MenuItemId"           AS "MenuItemId",
                tci."Snapshot_MenuCategoryId"       AS "MenuCategoryId",
                tci."Snapshot_ItemName"             AS "ItemName",
                tci."Quantity"                      AS "Quantity",
                tci."BasePrice_Amount"              AS "BasePriceAmount",
                tci."BasePrice_Currency"            AS "BasePriceCurrency",
                tci."SelectedCustomizations"        AS "SelectedCustomizations"
            FROM "TeamCartItems" tci
            WHERE tci."TeamCartId" = @TeamCartId
            ORDER BY tci."TeamCartItemId"
            """;

        var itemRows = await connection.QueryAsync<TeamCartItemRow>(
            new CommandDefinition(itemsSql, new { TeamCartId = request.TeamCartIdGuid }, cancellationToken: cancellationToken));

        var items = itemRows.Select(i => new TeamCartItemDto(
            i.TeamCartItemId,
            i.AddedByUserId,
            i.MenuItemId,
            i.MenuCategoryId,
            i.ItemName,
            i.Quantity,
            i.BasePriceAmount,
            i.BasePriceCurrency,
            ParseCustomizations(i.SelectedCustomizations)
        )).ToList();

        // Load member payments
        const string paymentsSql = """
            SELECT
                tcmp."UserId"               AS "UserId",
                tcmp."Method"               AS "PaymentMethod",
                tcmp."Status"               AS "PaymentStatus",
                tcmp."Payment_Amount"       AS "Amount",
                tcmp."Payment_Currency"     AS "Currency",
                tcmp."OnlineTransactionId"  AS "TransactionId",
                tcmp."UpdatedAt"            AS "ProcessedAt"
            FROM "TeamCartMemberPayments" tcmp
            WHERE tcmp."TeamCartId" = @TeamCartId
            ORDER BY tcmp."MemberPaymentId"
            """;

        var paymentRows = await connection.QueryAsync<MemberPaymentRow>(
            new CommandDefinition(paymentsSql, new { TeamCartId = request.TeamCartIdGuid }, cancellationToken: cancellationToken));

        var memberPayments = paymentRows.Select(p => new MemberPaymentDto(
            p.UserId,
            p.PaymentMethod,
            p.PaymentStatus,
            p.Amount,
            p.Currency,
            p.TransactionId,
            p.ProcessedAt
        )).ToList();

        // Calculate financial totals (simplified - in real implementation might need coupon/tax calculations)
        var subtotal = items.Sum(i => i.BasePriceAmount * i.Quantity);
        var currency = teamCartRow.TipCurrency;
        var total = subtotal + teamCartRow.TipAmount; // Simplified - no delivery fee/tax in this example

        var teamCartDetails = new TeamCartDetailsDto(
            teamCartRow.TeamCartId,
            teamCartRow.RestaurantId,
            teamCartRow.HostUserId,
            Enum.Parse<TeamCartStatus>(teamCartRow.Status),
            MaskShareToken(teamCartRow.ShareTokenValue),
            teamCartRow.Deadline,
            teamCartRow.CreatedAt,
            teamCartRow.ExpiresAt,
            teamCartRow.TipAmount,
            teamCartRow.TipCurrency,
            teamCartRow.AppliedCouponId,
            0m, // DiscountAmount - would need coupon lookup
            currency,
            subtotal,
            0m, // DeliveryFee - simplified
            0m, // TaxAmount - simplified
            total,
            currency,
            members,
            items,
            memberPayments
        );

        return Result.Success(new GetTeamCartDetailsResponse(teamCartDetails));
    }

    private static string MaskShareToken(string token)
    {
        return token.Length >= 4 ? $"***{token[^4..]}" : "***";
    }

    private static IReadOnlyList<TeamCartItemCustomizationDto> ParseCustomizations(string? customizationsJson)
    {
        if (string.IsNullOrWhiteSpace(customizationsJson))
            return new List<TeamCartItemCustomizationDto>();

        try
        {
            var customizations = JsonSerializer.Deserialize<List<TeamCartItemCustomizationJson>>(customizationsJson);
            return customizations?.Select(c => new TeamCartItemCustomizationDto(
                c.CustomizationGroupId,
                c.GroupDisplayTitle,
                c.CustomizationOptionId,
                c.OptionDisplayTitle,
                c.PriceAdjustment_Amount,
                c.PriceAdjustment_Currency
            )).ToList() ?? new List<TeamCartItemCustomizationDto>();
        }
        catch (JsonException)
        {
            return new List<TeamCartItemCustomizationDto>();
        }
    }

    // Internal row-shaping types for Dapper (avoid leaking into public API)
    private sealed record TeamCartDetailsRow(
        Guid TeamCartId,
        Guid RestaurantId,
        Guid HostUserId,
        string Status,
        string ShareTokenValue,
        DateTime? Deadline,
        DateTime CreatedAt,
        DateTime ExpiresAt,
        decimal TipAmount,
        string TipCurrency,
        Guid? AppliedCouponId
    );

    private sealed record TeamCartMemberRow(
        Guid TeamCartMemberId,
        Guid UserId,
        string Name,
        string Role
    );

    private sealed record TeamCartItemRow(
        Guid TeamCartItemId,
        Guid AddedByUserId,
        Guid MenuItemId,
        Guid MenuCategoryId,
        string ItemName,
        int Quantity,
        decimal BasePriceAmount,
        string BasePriceCurrency,
        string? SelectedCustomizations
    );

    private sealed record MemberPaymentRow(
        Guid UserId,
        string PaymentMethod,
        string PaymentStatus,
        decimal Amount,
        string Currency,
        string? TransactionId,
        DateTime? ProcessedAt
    );

    private sealed record TeamCartItemCustomizationJson(
        Guid CustomizationGroupId,
        string GroupDisplayTitle,
        Guid CustomizationOptionId,
        string OptionDisplayTitle,
        decimal PriceAdjustment_Amount,
        string PriceAdjustment_Currency
    );
}
