using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Menus.Commands.UpdateMenuDetails;
using YummyZoom.Domain.MenuEntity;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Menus.Commands;

public class UpdateMenuDetailsTests : BaseTestFixture
{
    [Test]
    public async Task UpdateMenuDetails_WithValidData_ShouldSucceedAndUpdateMenu()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        
        var command = new UpdateMenuDetailsCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuId: Testing.TestData.DefaultMenuId,
            Name: "Updated Menu Name",
            Description: "Updated menu description");

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();

        var menu = await FindAsync<Menu>(MenuId.Create(Testing.TestData.DefaultMenuId));
        
        menu.Should().NotBeNull();
        menu!.Name.Should().Be("Updated Menu Name");
        menu.Description.Should().Be("Updated menu description");
    }

    [Test]
    public async Task UpdateMenuDetails_ForNonExistentMenu_ShouldFail()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        
        var command = new UpdateMenuDetailsCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuId: Guid.NewGuid(), // Non-existent menu
            Name: "Updated Name",
            Description: "Updated description");

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure("Menu.InvalidMenuId");
    }

    [Test]
    public async Task UpdateMenuDetails_WithoutAuthorization_ShouldFailWithForbidden()
    {
        // Arrange
        await RunAsDefaultUserAsync(); // Simulate a regular user

        var command = new UpdateMenuDetailsCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuId: Testing.TestData.DefaultMenuId,
            Name: "Updated Name",
            Description: "Updated description");

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task UpdateMenuDetails_WithInvalidData_ShouldFailValidation()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        
        var command = new UpdateMenuDetailsCommand(
            RestaurantId: Guid.Empty,
            MenuId: Guid.Empty,
            Name: "",
            Description: "");

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }
}
