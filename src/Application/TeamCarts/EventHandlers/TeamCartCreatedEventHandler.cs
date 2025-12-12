using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Notifications;
using YummyZoom.Application.TeamCarts.Models;
using YummyZoom.Domain.TeamCartAggregate.Events;

namespace YummyZoom.Application.TeamCarts.EventHandlers;

/// <summary>
/// Handles TeamCartCreated domain events by creating the initial TeamCart VM in the real-time store
/// and notifying connected clients.
/// Idempotent via inbox infrastructure.
/// </summary>
public sealed class TeamCartCreatedEventHandler : IdempotentNotificationHandler<TeamCartCreated>
{
    private readonly IDbConnectionFactory _dbConnectionFactory;
    private readonly ITeamCartStore _store;
    private readonly ITeamCartRealtimeNotifier _notifier;
    private readonly ILogger<TeamCartCreatedEventHandler> _logger;

    public TeamCartCreatedEventHandler(
        IUnitOfWork uow,
        IInboxStore inbox,
        IDbConnectionFactory dbConnectionFactory,
        ITeamCartStore store,
        ITeamCartRealtimeNotifier notifier,
        ILogger<TeamCartCreatedEventHandler> logger) : base(uow, inbox)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _store = store;
        _notifier = notifier;
        _logger = logger;
    }

    protected override async Task HandleCore(TeamCartCreated notification, CancellationToken ct)
    {
        var cartId = notification.TeamCartId;
        _logger.LogDebug("Handling TeamCartCreated (EventId={EventId}, CartId={CartId})", notification.EventId, cartId.Value);

        using var connection = _dbConnectionFactory.CreateConnection();

        // Optimized query to fetch TeamCart, Restaurant, and Members in one go
        const string sql = """
            SELECT
                tc."Id"                     AS "CartId",
                tc."RestaurantId"           AS "RestaurantId",
                r."Name"                    AS "RestaurantName",
                tc."Status"                 AS "Status",
                tc."Deadline"               AS "Deadline",
                tc."ExpiresAt"              AS "ExpiresAt",
                tc."ShareToken_Value"       AS "ShareToken",
                tc."TipAmount_Amount"       AS "TipAmount",
                tc."TipAmount_Currency"     AS "TipCurrency",
                tcm."UserId"                AS "MemberUserId",
                tcm."Name"                  AS "MemberName",
                tcm."Role"                  AS "MemberRole"
            FROM "TeamCarts" tc
            JOIN "Restaurants" r ON tc."RestaurantId" = r."Id"
            LEFT JOIN "TeamCartMembers" tcm ON tc."Id" = tcm."TeamCartId"
            WHERE tc."Id" = @CartId
            """;

        var rows = await connection.QueryAsync<TeamCartFlatRow>(
            new CommandDefinition(sql, new { CartId = cartId.Value }, cancellationToken: ct));

        var flatRows = rows.ToList();
        if (flatRows.Count == 0)
        {
            _logger.LogWarning("TeamCartCreated handler could not find cart (CartId={CartId}, EventId={EventId})", cartId.Value, notification.EventId);
            return;
        }

        // Map flat rows to TeamCartViewModel
        var firstRow = flatRows[0];

        var vm = new TeamCartViewModel
        {
            CartId = notification.TeamCartId,
            RestaurantId = firstRow.RestaurantId,
            RestaurantName = firstRow.RestaurantName,
            Status = Enum.Parse<Domain.TeamCartAggregate.Enums.TeamCartStatus>(firstRow.Status),
            Deadline = firstRow.Deadline,
            ExpiresAt = firstRow.ExpiresAt,
            ShareToken = firstRow.ShareToken,
            ShareTokenMasked = firstRow.ShareToken.Length >= 4 ? $"***{firstRow.ShareToken[^4..]}" : "***",
            TipAmount = firstRow.TipAmount,
            TipCurrency = firstRow.TipCurrency,

            // Financial defaults for new cart
            CouponCode = null,
            DiscountAmount = 0m,
            Subtotal = 0m,
            Currency = firstRow.TipCurrency, // Assuming base currency matches tip currency logic
            DeliveryFee = 0m,
            TaxAmount = 0m,
            Total = 0m,
            CashOnDeliveryPortion = 0m,

            Version = 1,
            Items = new List<TeamCartViewModel.Item>(),
            Members = new List<TeamCartViewModel.Member>()
        };

        foreach (var row in flatRows)
        {
            if (row.MemberUserId != Guid.Empty)
            {
                vm.Members.Add(new TeamCartViewModel.Member
                {
                    UserId = row.MemberUserId,
                    Name = row.MemberName,
                    Role = row.MemberRole,
                    PaymentStatus = "Pending",
                    CommittedAmount = 0m,
                    OnlineTransactionId = null
                });
            }
        }

        try
        {
            await _store.CreateVmAsync(vm, ct);
            await _notifier.NotifyCartUpdated(vm.CartId, ct);

            // Suppress push notification for TeamCartCreated as users are already aware
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create VM or notify for TeamCartId={CartId} (EventId={EventId})", cartId.Value, notification.EventId);
            throw;
        }
    }

    // Private DTO class for Dapper result mapping
    private sealed record TeamCartFlatRow(
        Guid CartId,
        Guid RestaurantId,
        string RestaurantName,
        string Status,
        DateTime? Deadline,
        DateTime ExpiresAt,
        string ShareToken,
        decimal TipAmount,
        string TipCurrency,
        Guid MemberUserId,
        string MemberName,
        string MemberRole
    );
}

