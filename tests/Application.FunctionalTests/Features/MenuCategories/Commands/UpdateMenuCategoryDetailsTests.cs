using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.MenuCategories.Commands.UpdateMenuCategoryDetails;
using YummyZoom.Domain.MenuEntity;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.MenuCategories.Commands;

public class UpdateMenuCategoryDetailsTests : BaseTestFixture
{
    [Test]
    public async Task UpdateMenuCategoryDetails_WithValidData_ShouldSucceedAndUpdateCategory()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var categoryId = Testing.TestData.GetMenuCategoryId("Main Dishes");
        var command = new UpdateMenuCategoryDetailsCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuCategoryId: categoryId,
            Name: "Updated Main Dishes",
            DisplayOrder: 5);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();

        var category = await FindAsync<MenuCategory>(MenuCategoryId.Create(categoryId));
        category.Should().NotBeNull();
        category!.Name.Should().Be("Updated Main Dishes");
        category.DisplayOrder.Should().Be(5);
    }

    [Test]
    public async Task UpdateMenuCategoryDetails_ForNonExistentCategory_ShouldFail()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var command = new UpdateMenuCategoryDetailsCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuCategoryId: Guid.NewGuid(), // Non-existent category
            Name: "Updated Name",
            DisplayOrder: 1);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure("Menu.CategoryNotFound");
    }

    [Test]
    public async Task UpdateMenuCategoryDetails_ForCategoryOfDifferentRestaurant_ShouldFail()
    {
        // Arrange
        var scenario = await MenuTestDataFactory.CreateRestaurantWithMenuAsync(new MenuScenarioOptions
        {
            RestaurantId = Guid.NewGuid(), // Explicitly create a different restaurant ID
            EnabledMenu = true,
            CategoryCount = 1,
            CategoryGenerator = i => ($"Other Category", 1)
        });
        var otherCategoryId = scenario.CategoryIds.First();

        // Run as a staff member of the *default* restaurant
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        // Act: Attempt to update a category from the other restaurant
        var command = new UpdateMenuCategoryDetailsCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId, // User has access to this restaurant
            MenuCategoryId: otherCategoryId, // But this category belongs to a different restaurant
            Name: "Updated Name",
            DisplayOrder: 1);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure("Menu.CategoryNotFound");
    }

    [Test]
    public async Task UpdateMenuCategoryDetails_WithoutAuthorization_ShouldFailWithForbidden()
    {
        // Arrange
        await RunAsDefaultUserAsync();

        var categoryId = Testing.TestData.GetMenuCategoryId("Main Dishes");
        var command = new UpdateMenuCategoryDetailsCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuCategoryId: categoryId,
            Name: "Updated Name",
            DisplayOrder: 1);

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task UpdateMenuCategoryDetails_WithInvalidData_ShouldFailValidation()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var command = new UpdateMenuCategoryDetailsCommand(
            RestaurantId: Guid.Empty,
            MenuCategoryId: Guid.Empty,
            Name: "",
            DisplayOrder: 0);

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task UpdateMenuCategoryDetails_OnlyName_ShouldSucceed()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var categoryId = Testing.TestData.GetMenuCategoryId("Appetizers");
        var originalCategory = await FindAsync<MenuCategory>(MenuCategoryId.Create(categoryId));
        var originalDisplayOrder = originalCategory!.DisplayOrder;

        var command = new UpdateMenuCategoryDetailsCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuCategoryId: categoryId,
            Name: "Updated Appetizers",
            DisplayOrder: originalDisplayOrder); // Keep same display order

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();

        var updatedCategory = await FindAsync<MenuCategory>(MenuCategoryId.Create(categoryId));
        updatedCategory!.Name.Should().Be("Updated Appetizers");
        updatedCategory.DisplayOrder.Should().Be(originalDisplayOrder); // Should remain unchanged
    }

    [Test]
    public async Task UpdateMenuCategoryDetails_OnlyDisplayOrder_ShouldSucceed()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var categoryId = Testing.TestData.GetMenuCategoryId("Desserts");
        var originalCategory = await FindAsync<MenuCategory>(MenuCategoryId.Create(categoryId));
        var originalName = originalCategory!.Name;

        var command = new UpdateMenuCategoryDetailsCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuCategoryId: categoryId,
            Name: originalName, // Keep same name
            DisplayOrder: 10);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();

        var updatedCategory = await FindAsync<MenuCategory>(MenuCategoryId.Create(categoryId));
        updatedCategory!.Name.Should().Be(originalName); // Should remain unchanged
        updatedCategory.DisplayOrder.Should().Be(10);
    }
}
