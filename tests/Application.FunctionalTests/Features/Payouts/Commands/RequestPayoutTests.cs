using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.Payouts.Commands.RequestPayout;
using YummyZoom.Domain.PayoutAggregate.Enums;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Payouts.Commands;

public class RequestPayoutTests : BaseTestFixture
{
    [Test]
    public async Task RequestPayout_WithBalanceAndMethod_Succeeds()
    {
        await RunAsRestaurantOwnerAsync("owner@payouts.com", Testing.TestData.DefaultRestaurantId);
        await PayoutTestHelper.CreateAccountWithBalanceAsync(Testing.TestData.DefaultRestaurantId, 100m, withPayoutMethod: true);

        var cmd = new RequestPayoutCommand(
            RestaurantGuid: Testing.TestData.DefaultRestaurantId,
            Amount: 40m,
            IdempotencyKey: "payout-key-1");

        var result = await SendAsync(cmd);
        result.ShouldBeSuccessful();

        await TestDatabaseManager.ExecuteInScopeAsync(async db =>
        {
            var payout = await db.Payouts.FirstAsync();
            payout.Status.Should().Be(PayoutStatus.Requested);
            payout.Amount.Amount.Should().Be(40m);

            var account = await db.RestaurantAccounts.FirstAsync();
            account.PendingPayoutTotal.Amount.Should().Be(40m);
            account.CurrentBalance.Amount.Should().Be(100m);
        });
    }

    [Test]
    public async Task RequestPayout_RespectsWeeklyCadence()
    {
        await RunAsRestaurantOwnerAsync("owner@weekly.com", Testing.TestData.DefaultRestaurantId);
        var account = await PayoutTestHelper.CreateAccountWithBalanceAsync(Testing.TestData.DefaultRestaurantId, 50m, withPayoutMethod: true);

        await PayoutTestHelper.CreatePayoutAsync(
            account.Id,
            RestaurantId.Create(Testing.TestData.DefaultRestaurantId),
            10m,
            PayoutStatus.Requested);

        var cmd = new RequestPayoutCommand(
            RestaurantGuid: Testing.TestData.DefaultRestaurantId,
            Amount: 10m);

        var result = await SendAsync(cmd);
        result.ShouldBeFailure("RequestPayout.WeeklyCadenceViolation");
    }
}
