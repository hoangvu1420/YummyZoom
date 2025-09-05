using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.MenuItems.Commands.RemoveCustomizationGroupFromMenuItem;
using YummyZoom.Infrastructure.Data.Models;
using YummyZoom.Infrastructure.Data.ReadModels.FullMenu;

namespace YummyZoom.Application.FunctionalTests.Features.MenuItems.Commands;

using static Testing;

public class RemoveCustomizationGroupFromMenuItemTests : BaseTestFixture
{
    [Test]
    public async Task RemoveCustomization_FromItem_ShouldRebuildView_AndDisappearFromItem()
    {
        // Arrange: Classic Burger already has Burger Add-ons assigned by default test data
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var itemId = Testing.TestData.GetMenuItemId(DefaultTestData.MenuItems.MainDishes.ClassicBurger.Name);
        var groupId = TestDataFactory.CustomizationGroup_BurgerAddOnsId;

        var cmd = new RemoveCustomizationGroupFromMenuItemCommand(
            RestaurantId: restaurantId,
            MenuItemId: itemId,
            CustomizationGroupId: groupId);

        // Act
        var result = await SendAsync(cmd);
        await DrainOutboxAsync();

        // Assert
        result.ShouldBeSuccessful();
        var view = await FindAsync<FullMenuView>(restaurantId);
        view.Should().NotBeNull();

        var groupIds = FullMenuViewAssertions.GetItemCustomizationGroupIds(view!, itemId);
        groupIds.Should().NotContain(groupId);
    }

    [Test]
    public async Task RemoveCustomization_WhenNotAssigned_ShouldReturnNotFound()
    {
        // Arrange: Bun Type group is not assigned by default
        await RunAsRestaurantStaffAsync("staff2@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var itemId = Testing.TestData.GetMenuItemId(DefaultTestData.MenuItems.MainDishes.ClassicBurger.Name);
        var groupId = TestDataFactory.CustomizationGroup_RequiredBunTypeId!.Value;

        var cmd = new RemoveCustomizationGroupFromMenuItemCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuItemId: itemId,
            CustomizationGroupId: groupId);

        // Act
        var result = await SendAsync(cmd);

        // Assert
        result.ShouldBeFailure("MenuItem.CustomizationNotFound");
    }

    [Test]
    public async Task RemoveCustomization_WrongRestaurantScope_ShouldThrowForbidden()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff3@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var itemId = Testing.TestData.GetMenuItemId(DefaultTestData.MenuItems.MainDishes.ClassicBurger.Name);

        // Create second restaurant and login as its staff
        var second = await YummyZoom.Application.FunctionalTests.TestData.TestDataFactory.CreateSecondRestaurantWithMenuItemsAsync();
        await RunAsRestaurantStaffAsync("staff-second@restaurant.com", second.RestaurantId);

        var cmd = new RemoveCustomizationGroupFromMenuItemCommand(
            RestaurantId: second.RestaurantId,
            MenuItemId: itemId, // item belongs to default restaurant
            CustomizationGroupId: TestDataFactory.CustomizationGroup_BurgerAddOnsId);

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(cmd))
            .Should().ThrowAsync<YummyZoom.Application.Common.Exceptions.ForbiddenAccessException>();
    }
}
