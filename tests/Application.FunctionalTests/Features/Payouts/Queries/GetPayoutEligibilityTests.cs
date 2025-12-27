using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Payouts.Queries.GetPayoutEligibility;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Payouts.Queries;

public class GetPayoutEligibilityTests : BaseTestFixture
{
    [Test]
    public async Task Eligibility_WithNoPayoutMethod_IsNotEligible()
    {
        await RunAsRestaurantOwnerAsync("owner@eligibility.com", Testing.TestData.DefaultRestaurantId);
        await PayoutTestHelper.CreateAccountWithBalanceAsync(Testing.TestData.DefaultRestaurantId, 60m, withPayoutMethod: false);

        var query = new GetPayoutEligibilityQuery(Testing.TestData.DefaultRestaurantId);
        var result = await SendAsync(query);
        result.ShouldBeSuccessful();

        result.Value.IsEligible.Should().BeFalse();
        result.Value.IneligibilityReason.Should().Be("PayoutMethodMissing");
    }

    [Test]
    public async Task Eligibility_WithBalanceAndMethod_IsEligible()
    {
        await RunAsRestaurantOwnerAsync("owner@eligible.com", Testing.TestData.DefaultRestaurantId);
        await PayoutTestHelper.CreateAccountWithBalanceAsync(Testing.TestData.DefaultRestaurantId, 90m, withPayoutMethod: true);

        var query = new GetPayoutEligibilityQuery(Testing.TestData.DefaultRestaurantId);
        var result = await SendAsync(query);
        result.ShouldBeSuccessful();

        result.Value.IsEligible.Should().BeTrue();
        result.Value.AvailableAmount.Should().Be(90m);
    }
}
