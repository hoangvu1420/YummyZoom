using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.MenuItems.Commands.DeleteMenuItem;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Application.FunctionalTests.TestData;

namespace YummyZoom.Application.FunctionalTests.Features.MenuItems.Commands;

using static Testing;

public class DeleteMenuItemTests : BaseTestFixture
{
    [Test]
    public async Task Delete_ValidItem_ShouldSoftDeleteAndPersist()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var burgerId = Testing.TestData.GetMenuItemId("Classic Beef Burger");

        var cmd = new DeleteMenuItemCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuItemId: burgerId);

        // Act
        var result = await SendAsync(cmd);

        // Assert
        result.ShouldBeSuccessful();

        var item = await FindAsync<MenuItem>(MenuItemId.Create(burgerId));
        item.Should().BeNull(); // filtered out by soft delete filter

        // Optionally verify soft-deleted state if needed by bypassing filter via a repository/infrastructure method.
    }

    [Test]
    public async Task Delete_NonExistentItem_ShouldReturnNotFound()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff2@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var cmd = new DeleteMenuItemCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuItemId: Guid.NewGuid());

        // Act
        var result = await SendAsync(cmd);

        // Assert
        result.ShouldBeFailure("MenuItem.MenuItemNotFound");
    }

    [Test]
    public async Task Delete_WrongRestaurantScope_ShouldThrowForbidden()
    {
        // Arrange: act as staff of second restaurant and try deleting item from default
        var secondRestaurantId = await CreateSecondRestaurantAsync();
        await RunAsRestaurantStaffAsync("staff-second@restaurant.com", secondRestaurantId);
        var burgerId = Testing.TestData.GetMenuItemId("Classic Beef Burger");

        var cmd = new DeleteMenuItemCommand(
            RestaurantId: secondRestaurantId,
            MenuItemId: burgerId);

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(cmd))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task Delete_InvalidIds_ShouldFailValidation()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff3@restaurant.com", Testing.TestData.DefaultRestaurantId);

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(new DeleteMenuItemCommand(
            RestaurantId: Guid.Empty,
            MenuItemId: Guid.Empty)))
            .Should().ThrowAsync<ValidationException>();
    }

    private static async Task<Guid> CreateSecondRestaurantAsync()
    {
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

