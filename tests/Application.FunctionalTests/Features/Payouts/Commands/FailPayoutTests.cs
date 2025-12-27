using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.Payouts.Commands.FailPayout;
using YummyZoom.Domain.PayoutAggregate.Enums;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Payouts.Commands;

public class FailPayoutTests : BaseTestFixture
{
    [Test]
    public async Task FailPayout_ReleasesHoldWithoutDeductingBalance()
    {
        await RunAsRestaurantOwnerAsync("owner@fail.com", Testing.TestData.DefaultRestaurantId);
        await PayoutTestHelper.CreateAccountWithBalanceAsync(Testing.TestData.DefaultRestaurantId, 80m, withPayoutMethod: true);
        await PayoutTestHelper.ReserveHoldAsync(Testing.TestData.DefaultRestaurantId, 20m);

        var payout = await PayoutTestHelper.CreatePayoutAsync(
            accountId: (await TestDatabaseManager.ExecuteInScopeAsync(async db =>
                (await db.RestaurantAccounts.FirstAsync()).Id)),
            restaurantId: RestaurantId.Create(Testing.TestData.DefaultRestaurantId),
            amount: 20m,
            status: PayoutStatus.Processing);

        var cmd = new FailPayoutCommand(payout.Id.Value, "mock failure");
        var result = await SendAsync(cmd);
        result.ShouldBeSuccessful();

        await TestDatabaseManager.ExecuteInScopeAsync(async db =>
        {
            var account = await db.RestaurantAccounts.FirstAsync();
            account.CurrentBalance.Amount.Should().Be(80m);
            account.PendingPayoutTotal.Amount.Should().Be(0m);

            var updatedPayout = await db.Payouts.FirstAsync();
            updatedPayout.Status.Should().Be(PayoutStatus.Failed);
            updatedPayout.FailureReason.Should().NotBeNull();
        });
    }
}
