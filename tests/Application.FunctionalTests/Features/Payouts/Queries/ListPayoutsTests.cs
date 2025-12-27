using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Payouts.Queries.ListPayouts;
using YummyZoom.Domain.PayoutAggregate.Enums;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Payouts.Queries;

public class ListPayoutsTests : BaseTestFixture
{
    [Test]
    public async Task ListPayouts_WithStatusFilter_ReturnsMatchingRows()
    {
        await RunAsRestaurantOwnerAsync("owner@list.com", Testing.TestData.DefaultRestaurantId);
        var account = await PayoutTestHelper.CreateAccountWithBalanceAsync(Testing.TestData.DefaultRestaurantId, 120m, withPayoutMethod: true);

        await PayoutTestHelper.CreatePayoutAsync(
            account.Id,
            RestaurantId.Create(Testing.TestData.DefaultRestaurantId),
            20m,
            PayoutStatus.Requested);

        await PayoutTestHelper.CreatePayoutAsync(
            account.Id,
            RestaurantId.Create(Testing.TestData.DefaultRestaurantId),
            30m,
            PayoutStatus.Completed);

        var query = new ListPayoutsQuery(
            RestaurantGuid: Testing.TestData.DefaultRestaurantId,
            Status: PayoutStatus.Completed.ToString(),
            From: null,
            To: null,
            PageNumber: 1,
            PageSize: 10);

        var result = await SendAsync(query);
        result.ShouldBeSuccessful();

        result.Value.Items.Count.Should().Be(1);
        result.Value.Items.Single().Status.Should().Be(PayoutStatus.Completed.ToString());
    }
}
