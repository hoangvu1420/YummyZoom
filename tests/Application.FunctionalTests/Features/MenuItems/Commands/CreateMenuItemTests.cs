using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.MenuItems.Commands.CreateMenuItem;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.MenuItems.Commands;

public class CreateMenuItemTests : BaseTestFixture
{
    [Test]
    public async Task CreateMenuItem_WithValidData_ShouldSucceedAndCreateMenuItem()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var command = new CreateMenuItemCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuCategoryId: Testing.TestData.GetMenuCategoryId("Main Dishes"),
            Name: "Test Menu Item",
            Description: "A delicious test item",
            Price: 12.99m,
            Currency: "USD",
            ImageUrl: "https://example.com/image.jpg",
            IsAvailable: true,
            DietaryTagIds: null);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.MenuItemId.Should().NotBe(Guid.Empty);

        // Verify the menu item was created in the database
        var menuItem = await FindAsync<MenuItem>(MenuItemId.Create(result.Value.MenuItemId));

        menuItem.Should().NotBeNull();
        menuItem!.Name.Should().Be("Test Menu Item");
        menuItem.Description.Should().Be("A delicious test item");
        menuItem.BasePrice.Amount.Should().Be(12.99m);
        menuItem.BasePrice.Currency.Should().Be("USD");
        menuItem.ImageUrl.Should().Be("https://example.com/image.jpg");
        menuItem.IsAvailable.Should().BeTrue();
        menuItem.RestaurantId.Value.Should().Be(Testing.TestData.DefaultRestaurantId);
        menuItem.MenuCategoryId.Value.Should().Be(Testing.TestData.GetMenuCategoryId("Main Dishes"));
    }

    [Test]
    public async Task CreateMenuItem_WithMinimalData_ShouldSucceed()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var command = new CreateMenuItemCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuCategoryId: Testing.TestData.GetMenuCategoryId("Main Dishes"),
            Name: "Simple Item",
            Description: "Simple description",
            Price: 5.00m,
            Currency: "USD");

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();

        var menuItem = await FindAsync<MenuItem>(MenuItemId.Create(result.Value.MenuItemId));

        menuItem.Should().NotBeNull();
        menuItem!.ImageUrl.Should().BeNull();
        menuItem.IsAvailable.Should().BeTrue(); // Default value
        menuItem.DietaryTagIds.Should().BeEmpty();
    }

    [Test]
    public async Task CreateMenuItem_WithInvalidCategory_ShouldFailWithCategoryNotFound()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var invalidCategoryId = Guid.NewGuid();
        var command = new CreateMenuItemCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuCategoryId: invalidCategoryId,
            Name: "Test Item",
            Description: "Test description",
            Price: 10.00m,
            Currency: "USD");

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure("MenuItem.CategoryNotFound");
    }

    [Test]
    public async Task CreateMenuItem_WithoutRestaurantStaffRole_ShouldFailWithForbidden()
    {
        // Arrange - Login as regular user without restaurant staff role
        await RunAsDefaultUserAsync();

        var command = new CreateMenuItemCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuCategoryId: Testing.TestData.GetMenuCategoryId("Main Dishes"),
            Name: "Test Item",
            Description: "Test description",
            Price: 10.00m,
            Currency: "USD");

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task CreateMenuItem_WithInvalidData_ShouldFailValidation()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var command = new CreateMenuItemCommand(
            RestaurantId: Guid.Empty,
            MenuCategoryId: Guid.Empty,
            Name: "",
            Description: "",
            Price: -1.00m,
            Currency: "INVALID");

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task CreateMenuItem_WithDietaryTags_ShouldSucceed()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var tagId1 = Guid.NewGuid();
        var tagId2 = Guid.NewGuid();

        var command = new CreateMenuItemCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuCategoryId: Testing.TestData.GetMenuCategoryId("Main Dishes"),
            Name: "Healthy Item",
            Description: "A healthy menu item",
            Price: 15.00m,
            Currency: "USD",
            DietaryTagIds: new List<Guid> { tagId1, tagId2 });

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();

        var menuItem = await FindAsync<MenuItem>(MenuItemId.Create(result.Value.MenuItemId));

        menuItem.Should().NotBeNull();
        menuItem!.DietaryTagIds.Should().HaveCount(2);
        menuItem.DietaryTagIds.Select(t => t.Value).Should().Contain([tagId1, tagId2]);
    }
}
