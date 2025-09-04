using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.MenuItems.Commands.UpdateMenuItemPrice;
using YummyZoom.Infrastructure.Data.Models;

namespace YummyZoom.Application.FunctionalTests.Features.MenuItems.Commands;

using static Testing;

public class UpdateMenuItemPriceTests : BaseTestFixture
{
    [Test]
    public async Task UpdatePrice_WithValidData_ShouldRebuildView_AndReflectNewPrice()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var itemId = Testing.TestData.GetMenuItemId(DefaultTestData.MenuItems.MainDishes.ClassicBurger.Name);

        var cmd = new UpdateMenuItemPriceCommand(
            RestaurantId: restaurantId,
            MenuItemId: itemId,
            Price: 19.75m,
            Currency: "USD");

        // Act
        var result = await SendAsync(cmd);
        await DrainOutboxAsync();

        // Assert
        result.ShouldBeSuccessful();
        var view = await FindAsync<FullMenuView>(restaurantId);
        view.Should().NotBeNull();
        FullMenuViewAssertions.GetItemPriceAmount(view!, itemId).Should().Be(19.75m);
        FullMenuViewAssertions.GetItemPriceCurrency(view!, itemId).Should().Be("USD");
    }

    [Test]
    public async Task UpdatePrice_NonExistentItem_ShouldReturnNotFound()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff2@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var cmd = new UpdateMenuItemPriceCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuItemId: Guid.NewGuid(),
            Price: 10.00m,
            Currency: "USD");

        // Act
        var result = await SendAsync(cmd);

        // Assert
        result.ShouldBeFailure("MenuItem.MenuItemNotFound");
    }

    [Test]
    public async Task UpdatePrice_WrongRestaurantScope_ShouldThrowForbidden()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff3@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var itemId = Testing.TestData.GetMenuItemId(DefaultTestData.MenuItems.MainDishes.ClassicBurger.Name);

        var second = await YummyZoom.Application.FunctionalTests.TestData.TestDataFactory.CreateSecondRestaurantWithMenuItemsAsync();
        await RunAsRestaurantStaffAsync("staff-second@restaurant.com", second.RestaurantId);

        var cmd = new UpdateMenuItemPriceCommand(
            RestaurantId: second.RestaurantId,
            MenuItemId: itemId,
            Price: 11.11m,
            Currency: "USD");

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(cmd))
            .Should().ThrowAsync<YummyZoom.Application.Common.Exceptions.ForbiddenAccessException>();
    }

    [Test]
    public async Task UpdatePrice_WithInvalidData_ShouldFailValidation()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff4@restaurant.com", Testing.TestData.DefaultRestaurantId);

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(new UpdateMenuItemPriceCommand(
            RestaurantId: Guid.Empty,
            MenuItemId: Guid.Empty,
            Price: -1.0m,
            Currency: "bad")))
            .Should().ThrowAsync<YummyZoom.Application.Common.Exceptions.ValidationException>();
    }
}
