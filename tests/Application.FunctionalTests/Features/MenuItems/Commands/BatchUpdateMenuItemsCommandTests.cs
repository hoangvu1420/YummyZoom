using System.Text.Json;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.MenuItems.Commands.BatchUpdateMenuItems;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;

namespace YummyZoom.Application.FunctionalTests.Features.MenuItems.Commands;

using static Testing;

public class BatchUpdateMenuItemsCommandTests : BaseTestFixture
{
    [Test]
    public async Task BatchUpdate_Should_UpdateMultipleItems()
    {
        await RunAsRestaurantStaffAsync("batch@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var burgerId = Testing.TestData.GetMenuItemId(DefaultTestData.MenuItems.MainDishes.ClassicBurger.Name);
        var wingsId = Testing.TestData.GetMenuItemId(DefaultTestData.MenuItems.Appetizers.BuffaloWings.Name);

        var cmd = new BatchUpdateMenuItemsCommand(
            RestaurantId: restaurantId,
            Operations: new List<MenuItemBatchUpdateOperation>
            {
                new(burgerId, "isAvailable", JsonSerializer.SerializeToElement(false)),
                new(wingsId, "price", JsonSerializer.SerializeToElement(199000m))
            });

        var result = await SendAsync(cmd);
        await DrainOutboxAsync();

        result.ShouldBeSuccessful();
        result.Value.SuccessCount.Should().Be(2);
        result.Value.FailedCount.Should().Be(0);
        result.Value.Errors.Should().BeEmpty();

        var burger = await FindAsync<MenuItem>(MenuItemId.Create(burgerId));
        var wings = await FindAsync<MenuItem>(MenuItemId.Create(wingsId));

        burger!.IsAvailable.Should().BeFalse();
        wings!.BasePrice.Amount.Should().Be(199000m);
    }

    [Test]
    public async Task BatchUpdate_Should_ReportFailuresAndContinue()
    {
        await RunAsRestaurantStaffAsync("batch2@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var burgerId = Testing.TestData.GetMenuItemId(DefaultTestData.MenuItems.MainDishes.ClassicBurger.Name);
        var missingId = Guid.NewGuid();

        var cmd = new BatchUpdateMenuItemsCommand(
            restaurantId,
            new List<MenuItemBatchUpdateOperation>
            {
                new(burgerId, "price", JsonSerializer.SerializeToElement(200000m)),
                new(missingId, "isAvailable", JsonSerializer.SerializeToElement(true)),
                new(burgerId, "price", JsonSerializer.SerializeToElement(-5m))
            });

        var result = await SendAsync(cmd);

        result.ShouldBeSuccessful();
        result.Value.SuccessCount.Should().Be(1);
        result.Value.FailedCount.Should().Be(2);
        result.Value.Errors.Should().HaveCount(2);
        result.Value.Errors.Should().Contain(e => e.ItemId == missingId);
        result.Value.Errors.Should().Contain(e => e.Message.Contains("price", StringComparison.OrdinalIgnoreCase));

        var burger = await FindAsync<MenuItem>(MenuItemId.Create(burgerId));
        burger!.BasePrice.Amount.Should().Be(200000m);
    }

    [Test]
    public async Task BatchUpdate_WrongRestaurant_ShouldThrowForbidden()
    {
        await RunAsRestaurantStaffAsync("batch3@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var burgerId = Testing.TestData.GetMenuItemId(DefaultTestData.MenuItems.MainDishes.ClassicBurger.Name);

        var second = await TestDataFactory.CreateSecondRestaurantWithMenuItemsAsync();
        await RunAsRestaurantStaffAsync("batch3-second@restaurant.com", second.RestaurantId);

        var cmd = new BatchUpdateMenuItemsCommand(
            RestaurantId: second.RestaurantId,
            Operations: new List<MenuItemBatchUpdateOperation>
            {
                new(burgerId, "isAvailable", JsonSerializer.SerializeToElement(false))
            });

        await FluentActions.Invoking(() => SendAsync(cmd))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task BatchUpdate_InvalidRequest_ShouldFailValidation()
    {
        await RunAsRestaurantStaffAsync("batch4@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var cmd = new BatchUpdateMenuItemsCommand(
            RestaurantId: Guid.Empty,
            Operations: new List<MenuItemBatchUpdateOperation>());

        await FluentActions.Invoking(() => SendAsync(cmd))
            .Should().ThrowAsync<ValidationException>();
    }
}
