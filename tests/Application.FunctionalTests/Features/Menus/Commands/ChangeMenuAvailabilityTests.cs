using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Menus.Commands.ChangeMenuAvailability;
using YummyZoom.Domain.MenuEntity;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Menus.Commands;

public class ChangeMenuAvailabilityTests : BaseTestFixture
{
    [Test]
    public async Task ChangeMenuAvailability_ToEnabled_ShouldSucceedAndEnableMenu()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        // Ensure menu is disabled first
        var menu = await FindAsync<Menu>(MenuId.Create(Testing.TestData.DefaultMenuId));
        menu!.Disable();
        await UpdateAsync(menu);

        var command = new ChangeMenuAvailabilityCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuId: Testing.TestData.DefaultMenuId,
            IsEnabled: true);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();

        var updatedMenu = await FindAsync<Menu>(MenuId.Create(Testing.TestData.DefaultMenuId));
        updatedMenu!.IsEnabled.Should().BeTrue();
    }

    [Test]
    public async Task ChangeMenuAvailability_ToDisabled_ShouldSucceedAndDisableMenu()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        // Ensure menu is enabled first
        var menu = await FindAsync<Menu>(MenuId.Create(Testing.TestData.DefaultMenuId));
        menu!.Enable();
        await UpdateAsync(menu);

        var command = new ChangeMenuAvailabilityCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuId: Testing.TestData.DefaultMenuId,
            IsEnabled: false);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();

        var updatedMenu = await FindAsync<Menu>(MenuId.Create(Testing.TestData.DefaultMenuId));
        updatedMenu!.IsEnabled.Should().BeFalse();
    }

    [Test]
    public async Task ChangeMenuAvailability_ForNonExistentMenu_ShouldFail()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var command = new ChangeMenuAvailabilityCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuId: Guid.NewGuid(), // Non-existent menu
            IsEnabled: true);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure("Menu.InvalidMenuId");
    }

    [Test]
    public async Task ChangeMenuAvailability_WithoutAuthorization_ShouldFailWithForbidden()
    {
        // Arrange
        await RunAsDefaultUserAsync(); // Simulate a regular user

        var command = new ChangeMenuAvailabilityCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuId: Testing.TestData.DefaultMenuId,
            IsEnabled: true);

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task ChangeMenuAvailability_WithInvalidData_ShouldFailValidation()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var command = new ChangeMenuAvailabilityCommand(
            RestaurantId: Guid.Empty,
            MenuId: Guid.Empty,
            IsEnabled: true);

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }
}
