using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;

using YummyZoom.Application.MenuCategories.Commands.AddMenuCategory;
using YummyZoom.Domain.MenuEntity;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.MenuCategories.Commands;

public class AddMenuCategoryTests : BaseTestFixture
{
    [Test]
    public async Task AddMenuCategory_WithValidData_ShouldSucceedAndCreateCategory()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var command = new AddMenuCategoryCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuId: Testing.TestData.DefaultMenuId,
            Name: "New Category");

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.MenuCategoryId.Should().NotBe(Guid.Empty);

        var category = await FindAsync<MenuCategory>(MenuCategoryId.Create(result.Value.MenuCategoryId));
        category.Should().NotBeNull();
        category!.Name.Should().Be("New Category");
        category.MenuId.Value.Should().Be(Testing.TestData.DefaultMenuId);
        category.DisplayOrder.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task AddMenuCategory_ToNonExistentMenu_ShouldFail()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var command = new AddMenuCategoryCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuId: Guid.NewGuid(), // Non-existent menu
            Name: "New Category");

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure("Menu.InvalidMenuId");
    }

    [Test]
    public async Task AddMenuCategory_ToMenuOfDifferentRestaurant_ShouldFail()
    {
        // Arrange: Create a second restaurant and menu
        var scenario = await MenuTestDataFactory.CreateRestaurantWithMenuAsync(new MenuScenarioOptions
        {
            RestaurantId = Guid.NewGuid(), // Explicitly create a different restaurant ID
            EnabledMenu = true
        });
        var otherMenuId = scenario.MenuId;

        // Run as a staff member of the *default* restaurant
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        // Act: Attempt to add a category to the other restaurant's menu
        // while claiming the operation is for the default restaurant.
        var command = new AddMenuCategoryCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId, // User has access to this restaurant
            MenuId: otherMenuId, // But this menu belongs to a different restaurant
            Name: "New Category");

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure("Menu.InvalidMenuId");
    }

    [Test]
    public async Task AddMenuCategory_WithoutAuthorization_ShouldFailWithForbidden()
    {
        // Arrange
        await RunAsDefaultUserAsync();

        var command = new AddMenuCategoryCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuId: Testing.TestData.DefaultMenuId,
            Name: "New Category");

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task AddMenuCategory_WithInvalidData_ShouldFailValidation()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var command = new AddMenuCategoryCommand(
            RestaurantId: Guid.Empty,
            MenuId: Guid.Empty,
            Name: "");

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }
}
