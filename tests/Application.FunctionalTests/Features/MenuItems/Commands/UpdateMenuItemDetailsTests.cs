using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.MenuItems.Commands.UpdateMenuItemDetails;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;

namespace YummyZoom.Application.FunctionalTests.Features.MenuItems.Commands;

using static Testing;

public class UpdateMenuItemDetailsTests : BaseTestFixture
{
    [Test]
    public async Task UpdateDetails_WithValidData_ShouldUpdateAndRaiseEvents()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var burgerId = Testing.TestData.GetMenuItemId("Classic Beef Burger");

        var cmd = new UpdateMenuItemDetailsCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuItemId: burgerId,
            Name: "Updated Burger",
            Description: "Updated description",
            Price: 16.50m,
            Currency: "USD",
            ImageUrl: "https://example.com/burger.jpg");

        // Act
        var result = await SendAsync(cmd);

        // Assert
        result.ShouldBeSuccessful();

        var item = await FindAsync<MenuItem>(MenuItemId.Create(burgerId));
        item!.Name.Should().Be("Updated Burger");
        item.Description.Should().Be("Updated description");
        item.BasePrice.Amount.Should().Be(16.50m);
        item.BasePrice.Currency.Should().Be("USD");
        item.ImageUrl.Should().Be("https://example.com/burger.jpg");
    }

    [Test]
    public async Task UpdateDetails_NonExistentItem_ShouldReturnNotFound()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff2@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var cmd = new UpdateMenuItemDetailsCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuItemId: Guid.NewGuid(),
            Name: "Name",
            Description: "Desc",
            Price: 10.00m,
            Currency: "USD");

        // Act
        var result = await SendAsync(cmd);

        // Assert
        result.ShouldBeFailure("MenuItem.MenuItemNotFound");
    }

    [Test]
    public async Task UpdateDetails_WrongRestaurantScope_ShouldThrowForbidden()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff3@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var burgerId = Testing.TestData.GetMenuItemId("Classic Beef Burger");

        // Create second restaurant and login as its staff
        var secondRestaurantId = await CreateSecondRestaurantAsync();
        await RunAsRestaurantStaffAsync("staff-second@restaurant.com", secondRestaurantId);

        var cmd = new UpdateMenuItemDetailsCommand(
            RestaurantId: secondRestaurantId,
            MenuItemId: burgerId,
            Name: "Name",
            Description: "Desc",
            Price: 11.00m,
            Currency: "USD");

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(cmd))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task UpdateDetails_WithInvalidData_ShouldFailValidation()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff4@restaurant.com", Testing.TestData.DefaultRestaurantId);

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(new UpdateMenuItemDetailsCommand(
            RestaurantId: Guid.Empty,
            MenuItemId: Guid.Empty,
            Name: "",
            Description: "",
            Price: -1.00m,
            Currency: "INVALID")))
            .Should().ThrowAsync<ValidationException>();
    }

    private static async Task<Guid> CreateSecondRestaurantAsync()
    {
        // Minimal second restaurant
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

