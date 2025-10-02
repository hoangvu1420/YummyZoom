using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.MenuItems.Commands.AssignMenuItemToCategory;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;

namespace YummyZoom.Application.FunctionalTests.Features.MenuItems.Commands;

using static Testing;

public class AssignMenuItemToCategoryTests : BaseTestFixture
{
    [Test]
    public async Task Assign_ToCategoryWithinSameRestaurant_ShouldSucceedAndPersist()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var burgerId = Testing.TestData.GetMenuItemId("Classic Beef Burger");
        var appetizersId = Testing.TestData.GetMenuCategoryId("Appetizers");

        var cmd = new AssignMenuItemToCategoryCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuItemId: burgerId,
            NewCategoryId: appetizersId);

        // Act
        var result = await SendAsync(cmd);

        // Assert
        result.ShouldBeSuccessful();
        var item = await FindAsync<MenuItem>(MenuItemId.Create(burgerId));
        item!.MenuCategoryId.Should().Be(MenuCategoryId.Create(appetizersId));
    }

    [Test]
    public async Task Assign_NonExistentItem_ShouldReturnNotFound()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff2@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var appetizersId = Testing.TestData.GetMenuCategoryId("Appetizers");

        var cmd = new AssignMenuItemToCategoryCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuItemId: Guid.NewGuid(),
            NewCategoryId: appetizersId);

        // Act
        var result = await SendAsync(cmd);

        // Assert
        result.ShouldBeFailure("MenuItem.MenuItemNotFound");
    }

    [Test]
    public async Task Assign_ToNonExistentCategory_ShouldReturnCategoryNotFound()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff3@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var burgerId = Testing.TestData.GetMenuItemId("Classic Beef Burger");
        var invalidCategoryId = Guid.NewGuid();

        var cmd = new AssignMenuItemToCategoryCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuItemId: burgerId,
            NewCategoryId: invalidCategoryId);

        // Act
        var result = await SendAsync(cmd);

        // Assert
        result.ShouldBeFailure("MenuItem.CategoryNotFound");
    }

    [Test]
    public async Task Assign_CategoryFromAnotherRestaurant_ShouldFailValidation()
    {
        // Arrange: use burger from default, create second restaurant and its category
        await RunAsRestaurantStaffAsync("staff4@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var burgerId = Testing.TestData.GetMenuItemId("Classic Beef Burger");

        var secondRestaurantId = await CreateSecondRestaurantAsync();
        var secondCategoryId = await GetFirstCategoryForRestaurantAsync(secondRestaurantId);

        var cmd = new AssignMenuItemToCategoryCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuItemId: burgerId,
            NewCategoryId: secondCategoryId);

        // Act
        var result = await SendAsync(cmd);

        // Assert
        result.ShouldBeFailure("MenuItem.CategoryNotBelongsToRestaurant");
    }

    [Test]
    public async Task Assign_WrongRestaurantScope_ShouldThrowForbidden()
    {
        // Arrange: switch to staff of second restaurant and try to move default restaurant item
        var secondRestaurantId = await CreateSecondRestaurantAsync();
        await RunAsRestaurantStaffAsync("staff-second@restaurant.com", secondRestaurantId);
        var burgerId = Testing.TestData.GetMenuItemId("Classic Beef Burger");
        var appetizersId = Testing.TestData.GetMenuCategoryId("Appetizers");

        var cmd = new AssignMenuItemToCategoryCommand(
            RestaurantId: secondRestaurantId,
            MenuItemId: burgerId,
            NewCategoryId: appetizersId);

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(cmd))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task Assign_WithInvalidIds_ShouldFailValidation()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff5@restaurant.com", Testing.TestData.DefaultRestaurantId);

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(new AssignMenuItemToCategoryCommand(
            RestaurantId: Guid.Empty,
            MenuItemId: Guid.Empty,
            NewCategoryId: Guid.Empty)))
            .Should().ThrowAsync<ValidationException>();
    }

    private static async Task<Guid> CreateSecondRestaurantAsync()
    {
        // Minimal second restaurant with categories
        var scenario = await YummyZoom.Application.FunctionalTests.TestData.MenuTestDataFactory.CreateRestaurantWithMenuAsync(
            new YummyZoom.Application.FunctionalTests.TestData.MenuScenarioOptions
            {
                RestaurantId = Guid.NewGuid(),
                CategoryCount = 1,
                ItemGenerator = (_, index) => new[]
                {
                    new YummyZoom.Application.FunctionalTests.TestData.ItemOptions
                    {
                        Name = $"Item-{index}",
                        Description = "Desc",
                        PriceAmount = 9.99m
                    }
                }
            });
        return scenario.RestaurantId;
    }

    private static async Task<Guid> GetFirstCategoryForRestaurantAsync(Guid restaurantId)
    {
        // The scenario created at least one category; fetch it from the scenario helper if needed.
        // For simplicity, reuse TestDataFactory's default categories not available here, so
        // create another scenario and return its first category id.
        var scenario = await YummyZoom.Application.FunctionalTests.TestData.MenuTestDataFactory.CreateRestaurantWithMenuAsync(
            new YummyZoom.Application.FunctionalTests.TestData.MenuScenarioOptions
            {
                RestaurantId = restaurantId,
                CategoryCount = 1,
                ItemGenerator = (_, index) => Array.Empty<YummyZoom.Application.FunctionalTests.TestData.ItemOptions>()
            });
        return scenario.CategoryIds.First();
    }
}

