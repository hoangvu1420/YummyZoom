using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.MenuItems.Commands.ChangeMenuItemAvailability;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Application.FunctionalTests.TestData;

namespace YummyZoom.Application.FunctionalTests.Features.MenuItems.Commands;

using static Testing;

public class ChangeMenuItemAvailabilityTests : BaseTestFixture
{
    [Test]
    public async Task ChangeAvailability_WithValidData_ShouldToggleAndPersist()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var burgerId = Testing.TestData.GetMenuItemId("Classic Beef Burger");

        // Act - set unavailable
        var disableResult = await SendAsync(new ChangeMenuItemAvailabilityCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuItemId: burgerId,
            IsAvailable: false));

        // Assert
        disableResult.ShouldBeSuccessful();
        var item = await FindAsync<MenuItem>(MenuItemId.Create(burgerId));
        item!.IsAvailable.Should().BeFalse();

        // Act - set available
        var enableResult = await SendAsync(new ChangeMenuItemAvailabilityCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuItemId: burgerId,
            IsAvailable: true));

        // Assert
        enableResult.ShouldBeSuccessful();
        item = await FindAsync<MenuItem>(MenuItemId.Create(burgerId));
        item!.IsAvailable.Should().BeTrue();
    }

    [Test]
    public async Task ChangeAvailability_NonExistentItem_ShouldReturnNotFound()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff2@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await SendAsync(new ChangeMenuItemAvailabilityCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuItemId: nonExistentId,
            IsAvailable: false));

        // Assert
        result.ShouldBeFailure("MenuItem.MenuItemNotFound");
    }

    [Test]
    public async Task ChangeAvailability_WrongRestaurantScope_ShouldThrowForbidden()
    {
        // Arrange: act as staff for default restaurant
        await RunAsRestaurantStaffAsync("staff3@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var burgerId = Testing.TestData.GetMenuItemId("Classic Beef Burger");

        // Create second restaurant and login as its staff
        var secondRestaurantId = await CreateSecondRestaurantAsync();
        await RunAsRestaurantStaffAsync("staff-second@restaurant.com", secondRestaurantId);

        var command = new ChangeMenuItemAvailabilityCommand(
            RestaurantId: secondRestaurantId,
            MenuItemId: burgerId,
            IsAvailable: false);

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task ChangeAvailability_WithInvalidIds_ShouldFailValidation()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff4@restaurant.com", Testing.TestData.DefaultRestaurantId);

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(new ChangeMenuItemAvailabilityCommand(
            RestaurantId: Guid.Empty,
            MenuItemId: Guid.Empty,
            IsAvailable: true)))
            .Should().ThrowAsync<ValidationException>();
    }

    private static async Task<Guid> CreateSecondRestaurantAsync()
    {
        // Use MenuTestDataFactory to create a second restaurant with a menu
        var scenario = await MenuTestDataFactory.CreateRestaurantWithMenuAsync(
            new MenuScenarioOptions
            {
                RestaurantId = Guid.NewGuid(),
                CategoryCount = 1,
                ItemGenerator = (_, index) => new[]
                {
                    new ItemOptions
                    {
                        Name = $"Item-{index}",
                        Description = "Desc",
                        PriceAmount = 9.99m
                    }
                }
            });
        return scenario.RestaurantId;
    }
}

