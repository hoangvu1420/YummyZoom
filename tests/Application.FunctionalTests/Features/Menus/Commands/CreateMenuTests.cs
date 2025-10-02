using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Menus.Commands.CreateMenu;
using YummyZoom.Domain.MenuEntity;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Menus.Commands;

public class CreateMenuTests : BaseTestFixture
{
    [Test]
    public async Task CreateMenu_WithValidData_ShouldSucceedAndCreateMenu()
    {
        // Arrange
        await RunAsRestaurantOwnerAsync("owner@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var command = new CreateMenuCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Name: "Test Menu",
            Description: "A test menu for our restaurant",
            IsEnabled: true);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        result.Value.MenuId.Should().NotBe(Guid.Empty);

        // Verify the menu was created in the database
        var menu = await FindAsync<Menu>(MenuId.Create(result.Value.MenuId));

        menu.Should().NotBeNull();
        menu!.Name.Should().Be("Test Menu");
        menu.Description.Should().Be("A test menu for our restaurant");
        menu.IsEnabled.Should().BeTrue();
        menu.RestaurantId.Value.Should().Be(Testing.TestData.DefaultRestaurantId);
    }

    [Test]
    public async Task CreateMenu_WithMinimalData_ShouldSucceed()
    {
        // Arrange
        await RunAsRestaurantOwnerAsync("owner@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var command = new CreateMenuCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Name: "Simple Menu",
            Description: "Simple description");

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();

        var menu = await FindAsync<Menu>(MenuId.Create(result.Value.MenuId));

        menu.Should().NotBeNull();
        menu!.IsEnabled.Should().BeTrue(); // Default value
    }

    [Test]
    public async Task CreateMenu_WithDisabledMenu_ShouldSucceed()
    {
        // Arrange
        await RunAsRestaurantOwnerAsync("owner@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var command = new CreateMenuCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Name: "Disabled Menu",
            Description: "A disabled menu",
            IsEnabled: false);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();

        var menu = await FindAsync<Menu>(MenuId.Create(result.Value.MenuId));

        menu.Should().NotBeNull();
        menu!.IsEnabled.Should().BeFalse();
    }

    [Test]
    public async Task CreateMenu_WithoutRestaurantOwnerRole_ShouldFailWithForbidden()
    {
        // Arrange - Login as restaurant staff (not owner)
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var command = new CreateMenuCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Name: "Test Menu",
            Description: "Test description");

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task CreateMenu_WithoutAuthentication_ShouldFailWithForbidden()
    {
        // Arrange - Login as regular user without restaurant staff role
        await RunAsDefaultUserAsync();

        var command = new CreateMenuCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Name: "Test Menu",
            Description: "Test description");

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task CreateMenu_WithInvalidData_ShouldFailValidation()
    {
        // Arrange
        await RunAsRestaurantOwnerAsync("owner@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var command = new CreateMenuCommand(
            RestaurantId: Guid.Empty,
            Name: "",
            Description: "");

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task CreateMenu_WithTooLongName_ShouldFailValidation()
    {
        // Arrange
        await RunAsRestaurantOwnerAsync("owner@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var longName = new string('A', 201); // Exceeds 200 character limit
        var command = new CreateMenuCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Name: longName,
            Description: "Valid description");

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task CreateMenu_WithTooLongDescription_ShouldFailValidation()
    {
        // Arrange
        await RunAsRestaurantOwnerAsync("owner@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var longDescription = new string('A', 1001); // Exceeds 1000 character limit
        var command = new CreateMenuCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            Name: "Valid Name",
            Description: longDescription);

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(command))
            .Should().ThrowAsync<ValidationException>();
    }
}
