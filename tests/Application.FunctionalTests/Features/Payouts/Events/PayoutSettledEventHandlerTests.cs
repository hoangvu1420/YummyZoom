using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.FunctionalTests.Features.Payouts;
using YummyZoom.Application.Payouts.Commands.CompletePayout;
using YummyZoom.Application.RestaurantAccounts.EventHandlers;
using YummyZoom.Domain.AccountTransactionEntity.Enums;
using YummyZoom.Domain.Common.Constants;
using YummyZoom.Domain.PayoutAggregate.Enums;
using YummyZoom.Domain.RestaurantAccountAggregate.Events;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;
using YummyZoom.Infrastructure.Persistence.EfCore.Models;

namespace YummyZoom.Application.FunctionalTests.Features.Payouts.Events;

using static Testing;

public class PayoutSettledEventHandlerTests : BaseTestFixture
{
    [Test]
    public async Task CompletePayout_Should_Create_PayoutSettlement_Transaction_And_Inbox_Record()
    {
        await RunAsRestaurantOwnerAsync("owner@payoutsettled.com", TestData.DefaultRestaurantId);
        await PayoutTestHelper.CreateAccountWithBalanceAsync(TestData.DefaultRestaurantId, 100m, withPayoutMethod: true);
        await PayoutTestHelper.ReserveHoldAsync(TestData.DefaultRestaurantId, 30m);

        var accountId = await TestDatabaseManager.ExecuteInScopeAsync(async db =>
            (await db.RestaurantAccounts.FirstAsync()).Id);

        var payout = await PayoutTestHelper.CreatePayoutAsync(
            accountId,
            RestaurantId.Create(TestData.DefaultRestaurantId),
            amount: 30m,
            status: PayoutStatus.Processing);

        var cmd = new CompletePayoutCommand(payout.Id.Value, "mock_ref");
        var result = await SendAsync(cmd);
        result.ShouldBeSuccessful();

        await DrainOutboxAsync();

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var handlerName = typeof(PayoutSettledEventHandler).FullName!;

        var inboxEntries = await db.Set<InboxMessage>()
            .Where(x => x.Handler == handlerName)
            .ToListAsync();
        inboxEntries.Should().ContainSingle();

        var outboxEntries = await db.Set<OutboxMessage>()
            .Where(m => m.Type.Contains(nameof(PayoutSettled)))
            .ToListAsync();
        outboxEntries.Should().NotBeEmpty();
        outboxEntries.Should().OnlyContain(m => m.ProcessedOnUtc != null && m.Error == null);

        var transactions = await db.AccountTransactions
            .Where(t => t.RestaurantAccountId == accountId && t.Type == TransactionType.PayoutSettlement)
            .ToListAsync();
        transactions.Should().ContainSingle();

        var transaction = transactions.Single();
        transaction.Amount.Amount.Should().Be(-30m);
        transaction.Amount.Currency.Should().Be(Currencies.Default);
    }

    [Test]
    public async Task PayoutSettled_Should_Be_Idempotent_When_Outbox_Drained_Twice()
    {
        await RunAsRestaurantOwnerAsync("owner@payoutsettled2.com", TestData.DefaultRestaurantId);
        await PayoutTestHelper.CreateAccountWithBalanceAsync(TestData.DefaultRestaurantId, 80m, withPayoutMethod: true);
        await PayoutTestHelper.ReserveHoldAsync(TestData.DefaultRestaurantId, 20m);

        var accountId = await TestDatabaseManager.ExecuteInScopeAsync(async db =>
            (await db.RestaurantAccounts.FirstAsync()).Id);

        var payout = await PayoutTestHelper.CreatePayoutAsync(
            accountId,
            RestaurantId.Create(TestData.DefaultRestaurantId),
            amount: 20m,
            status: PayoutStatus.Processing);

        var cmd = new CompletePayoutCommand(payout.Id.Value, "mock_ref");
        var result = await SendAsync(cmd);
        result.ShouldBeSuccessful();

        await DrainOutboxAsync();

        var handlerName = typeof(PayoutSettledEventHandler).FullName!;
        int transactionCountAfterFirstDrain;
        int inboxCountAfterFirstDrain;

        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            transactionCountAfterFirstDrain = await db.AccountTransactions
                .CountAsync(t => t.RestaurantAccountId == accountId && t.Type == TransactionType.PayoutSettlement);

            inboxCountAfterFirstDrain = await db.Set<InboxMessage>()
                .CountAsync(x => x.Handler == handlerName);
        }

        await DrainOutboxAsync();

        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var transactionCountAfterSecondDrain = await db.AccountTransactions
                .CountAsync(t => t.RestaurantAccountId == accountId && t.Type == TransactionType.PayoutSettlement);

            var inboxCountAfterSecondDrain = await db.Set<InboxMessage>()
                .CountAsync(x => x.Handler == handlerName);

            transactionCountAfterSecondDrain.Should().Be(transactionCountAfterFirstDrain);
            inboxCountAfterSecondDrain.Should().Be(inboxCountAfterFirstDrain);
        }
    }
}
