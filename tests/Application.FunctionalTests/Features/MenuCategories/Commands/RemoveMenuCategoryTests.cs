using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.MenuCategories.Commands.RemoveMenuCategory;
using YummyZoom.Domain.MenuEntity;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Infrastructure.Data.Extensions;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.MenuCategories.Commands;

public class RemoveMenuCategoryTests : BaseTestFixture
{
    [Test]
    public async Task RemoveMenuCategory_WithValidData_ShouldSucceedAndMarkCategoryAsDeleted()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        // Create a new category to delete (so we don't affect other tests)
        var scenario = await MenuTestDataFactory.CreateRestaurantWithMenuAsync(new MenuScenarioOptions
        {
            RestaurantId = Testing.TestData.DefaultRestaurantId,
            EnabledMenu = true,
            CategoryCount = 1,
            CategoryGenerator = i => ($"Category to Delete", 1)
        });
        var categoryToDeleteId = scenario.CategoryIds.First();

        var command = new RemoveMenuCategoryCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuCategoryId: categoryToDeleteId);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();

        // Use ExecuteInScopeAsync to bypass soft-delete filter and verify the category is soft-deleted
        var category = await TestDatabaseManager.ExecuteInScopeAsync(async context =>
        {
            return await context.MenuCategories
                .IncludeSoftDeleted()
                .FirstOrDefaultAsync(c => c.Id == MenuCategoryId.Create(categoryToDeleteId));
        });

        category.Should().NotBeNull("because the category should exist but be soft-deleted");
        category!.IsDeleted.Should().BeTrue();
        category.DeletedOn.Should().NotBeNull();
    }

    [Test]
    public async Task RemoveMenuCategory_ForNonExistentCategory_ShouldFail()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var command = new RemoveMenuCategoryCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuCategoryId: Guid.NewGuid()); // Non-existent category

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure("Menu.CategoryNotFound");
    }

    [Test]
    public async Task RemoveMenuCategory_ForCategoryOfDifferentRestaurant_ShouldFail()
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

        // Act: Attempt to remove a category from the other restaurant
        var command = new RemoveMenuCategoryCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId, // User has access to this restaurant
            MenuCategoryId: otherCategoryId); // But this category belongs to a different restaurant

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure("Menu.CategoryNotFound");
    }

    [Test]
    public async Task RemoveMenuCategory_WithoutAuthorization_ShouldFailWithForbidden()
    {
        // Arrange
        await RunAsDefaultUserAsync();

        var categoryId = Testing.TestData.GetMenuCategoryId("Main Dishes");
        var command = new RemoveMenuCategoryCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuCategoryId: categoryId);

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task RemoveMenuCategory_WithInvalidData_ShouldFailValidation()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var command = new RemoveMenuCategoryCommand(
            RestaurantId: Guid.Empty,
            MenuCategoryId: Guid.Empty);

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task RemoveMenuCategory_AlreadyDeleted_ShouldSucceed()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        // Create a new category and delete it first
        var scenario = await MenuTestDataFactory.CreateRestaurantWithMenuAsync(new MenuScenarioOptions
        {
            RestaurantId = Testing.TestData.DefaultRestaurantId,
            EnabledMenu = true,
            CategoryCount = 1,
            CategoryGenerator = i => ($"Already Deleted Category", 1)
        });
        var categoryId = scenario.CategoryIds.First();

        // First deletion
        var firstCommand = new RemoveMenuCategoryCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuCategoryId: categoryId);

        await SendAsync(firstCommand);

        // Second deletion attempt
        var secondCommand = new RemoveMenuCategoryCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuCategoryId: categoryId);

        // Act
        var result = await SendAsync(secondCommand);

        // Assert - Should succeed (idempotent operation)
        result.ShouldBeSuccessful();

        // Note: FindAsync applies soft-delete filter, so deleted categories won't be found
        // This is the expected behavior - the category should be soft-deleted and filtered out
        var category = await FindAsync<MenuCategory>(MenuCategoryId.Create(categoryId));
        category.Should().BeNull("because the category should be soft-deleted and filtered out by the query filter");
    }
}
