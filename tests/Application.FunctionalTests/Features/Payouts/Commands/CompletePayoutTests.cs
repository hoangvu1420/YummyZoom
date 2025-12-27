using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.Payouts.Commands.CompletePayout;
using YummyZoom.Domain.PayoutAggregate.Enums;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Payouts.Commands;

public class CompletePayoutTests : BaseTestFixture
{
    [Test]
    public async Task CompletePayout_DeductsBalanceAndClearsHold()
    {
        await RunAsRestaurantOwnerAsync("owner@complete.com", Testing.TestData.DefaultRestaurantId);
        await PayoutTestHelper.CreateAccountWithBalanceAsync(Testing.TestData.DefaultRestaurantId, 100m, withPayoutMethod: true);
        await PayoutTestHelper.ReserveHoldAsync(Testing.TestData.DefaultRestaurantId, 30m);

        var accountId = await TestDatabaseManager.ExecuteInScopeAsync(async db =>
            (await db.RestaurantAccounts.FirstAsync()).Id);

        var payout = await PayoutTestHelper.CreatePayoutAsync(
            accountId: accountId,
            restaurantId: RestaurantId.Create(Testing.TestData.DefaultRestaurantId),
            amount: 30m,
            status: PayoutStatus.Processing);

        var cmd = new CompletePayoutCommand(payout.Id.Value, "mock_ref");
        var result = await SendAsync(cmd);
        result.ShouldBeSuccessful();

        await TestDatabaseManager.ExecuteInScopeAsync(async db =>
        {
            var account = await db.RestaurantAccounts.FirstAsync();
            account.CurrentBalance.Amount.Should().Be(70m);
            account.PendingPayoutTotal.Amount.Should().Be(0m);

            var updatedPayout = await db.Payouts.FirstAsync();
            updatedPayout.Status.Should().Be(PayoutStatus.Completed);
        });
    }
}
