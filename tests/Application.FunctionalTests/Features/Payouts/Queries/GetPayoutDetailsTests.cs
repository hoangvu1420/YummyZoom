using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Payouts.Queries.GetPayoutDetails;
using YummyZoom.Domain.PayoutAggregate.Enums;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Payouts.Queries;

public class GetPayoutDetailsTests : BaseTestFixture
{
    [Test]
    public async Task GetPayoutDetails_ReturnsPayout()
    {
        await RunAsRestaurantOwnerAsync("owner@details.com", Testing.TestData.DefaultRestaurantId);
        var account = await PayoutTestHelper.CreateAccountWithBalanceAsync(Testing.TestData.DefaultRestaurantId, 75m, withPayoutMethod: true);

        var payout = await PayoutTestHelper.CreatePayoutAsync(
            account.Id,
            RestaurantId.Create(Testing.TestData.DefaultRestaurantId),
            25m,
            PayoutStatus.Processing);

        var query = new GetPayoutDetailsQuery(Testing.TestData.DefaultRestaurantId, payout.Id.Value);
        var result = await SendAsync(query);
        result.ShouldBeSuccessful();

        result.Value.PayoutId.Should().Be(payout.Id.Value);
        result.Value.Amount.Should().Be(25m);
        result.Value.Status.Should().Be(PayoutStatus.Processing.ToString());
    }
}
