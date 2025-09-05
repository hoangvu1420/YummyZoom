using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.MenuItems.Commands.AssignCustomizationGroupToMenuItem;
using YummyZoom.Infrastructure.Data.Models;
using YummyZoom.Infrastructure.Data.ReadModels.FullMenu;

namespace YummyZoom.Application.FunctionalTests.Features.MenuItems.Commands;

using static Testing;

public class AssignCustomizationGroupToMenuItemTests : BaseTestFixture
{
    [Test]
    public async Task AssignCustomization_ToItem_ShouldRebuildView_AndAppearUnderItem()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var itemId = Testing.TestData.GetMenuItemId(DefaultTestData.MenuItems.MainDishes.ClassicBurger.Name);
        var groupId = TestDataFactory.CustomizationGroup_RequiredBunTypeId!.Value; // exists but not assigned by default

        var cmd = new AssignCustomizationGroupToMenuItemCommand(
            RestaurantId: restaurantId,
            MenuItemId: itemId,
            CustomizationGroupId: groupId,
            DisplayTitle: "Choose your bun",
            DisplayOrder: 2);

        // Act
        var result = await SendAsync(cmd);
        await DrainOutboxAsync();

        // Assert
        result.ShouldBeSuccessful();
        var view = await FindAsync<FullMenuView>(restaurantId);
        view.Should().NotBeNull();

        var groups = FullMenuViewAssertions.GetItemCustomizationGroups(view!, itemId);
        groups.Select(g => g.GroupId).Should().Contain(groupId);
        groups.Should().Contain(g => g.GroupId == groupId && g.DisplayTitle == "Choose your bun" && g.DisplayOrder == 2);
    }

    [Test]
    public async Task AssignCustomization_NonexistentGroup_ShouldReturnNotFound()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff2@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var itemId = Testing.TestData.GetMenuItemId(DefaultTestData.MenuItems.MainDishes.ClassicBurger.Name);

        var cmd = new AssignCustomizationGroupToMenuItemCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuItemId: itemId,
            CustomizationGroupId: Guid.NewGuid(),
            DisplayTitle: "X",
            DisplayOrder: 1);

        // Act
        var result = await SendAsync(cmd);

        // Assert
        result.ShouldBeFailure("CustomizationGroup.NotFound");
    }

    [Test]
    public async Task AssignCustomization_WrongRestaurantScope_ShouldThrowForbidden()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff3@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var itemId = Testing.TestData.GetMenuItemId(DefaultTestData.MenuItems.MainDishes.ClassicBurger.Name);

        // Create second restaurant and login as its staff
        var secondRestaurantId = await TestDataFactory.CreateSecondRestaurantWithMenuItemsAsync();
        await RunAsRestaurantStaffAsync("staff-second@restaurant.com", secondRestaurantId.RestaurantId);

        var cmd = new AssignCustomizationGroupToMenuItemCommand(
            RestaurantId: secondRestaurantId.RestaurantId,
            MenuItemId: itemId, // item belongs to default restaurant
            CustomizationGroupId: TestDataFactory.CustomizationGroup_RequiredBunTypeId!.Value,
            DisplayTitle: "Title",
            DisplayOrder: 1);

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(cmd))
            .Should().ThrowAsync<YummyZoom.Application.Common.Exceptions.ForbiddenAccessException>();
    }
}
