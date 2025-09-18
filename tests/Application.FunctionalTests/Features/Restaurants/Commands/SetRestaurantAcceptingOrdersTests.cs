using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Restaurants.Commands.SetRestaurantAcceptingOrders;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Restaurants.Commands;

public class SetRestaurantAcceptingOrdersTests : BaseTestFixture
{
    [Test]
    public async Task SetAcceptingOrders_ToTrue_Succeeds_AndSetsFlag()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var cmd = new SetRestaurantAcceptingOrdersCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            IsAccepting: true);

        var result = await SendAsync(cmd);
        result.ShouldBeSuccessful();
        result.Value.IsAccepting.Should().BeTrue();

        var agg = await FindAsync<Restaurant>(RestaurantId.Create(Testing.TestData.DefaultRestaurantId));
        agg!.IsAcceptingOrders.Should().BeTrue();
    }

    [Test]
    public async Task SetAcceptingOrders_ToFalse_Succeeds_AndClearsFlag()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        // Ensure starting state is accepting
        var agg = await FindAsync<Restaurant>(RestaurantId.Create(Testing.TestData.DefaultRestaurantId));
        agg!.AcceptOrders();
        await UpdateAsync(agg);

        var cmd = new SetRestaurantAcceptingOrdersCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            IsAccepting: false);

        var result = await SendAsync(cmd);
        result.ShouldBeSuccessful();
        result.Value.IsAccepting.Should().BeFalse();

        var updated = await FindAsync<Restaurant>(RestaurantId.Create(Testing.TestData.DefaultRestaurantId));
        updated!.IsAcceptingOrders.Should().BeFalse();
    }

    [Test]
    public async Task SetAcceptingOrders_ForNonExistentRestaurant_ReturnsNotFound()
    {
        var nonExistentRestaurantId = Guid.NewGuid();
        await RunAsRestaurantStaffAsync("staff@restaurant.com", nonExistentRestaurantId);

        var cmd = new SetRestaurantAcceptingOrdersCommand(
            RestaurantId: nonExistentRestaurantId,
            IsAccepting: true);

        var result = await SendAsync(cmd);
        result.ShouldBeFailure(SetRestaurantAcceptingOrdersErrors.NotFound(nonExistentRestaurantId).Code);
    }

    [Test]
    public async Task SetAcceptingOrders_WithoutAuthorization_ThrowsForbidden()
    {
        await RunAsDefaultUserAsync();

        var cmd = new SetRestaurantAcceptingOrdersCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            IsAccepting: true);

        await FluentActions.Invoking(() => SendAsync(cmd))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task SetAcceptingOrders_WithInvalidData_ThrowsValidation()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var cmd = new SetRestaurantAcceptingOrdersCommand(
            RestaurantId: Guid.Empty,
            IsAccepting: true);

        await FluentActions.Invoking(() => SendAsync(cmd))
            .Should().ThrowAsync<ValidationException>();
    }
}

