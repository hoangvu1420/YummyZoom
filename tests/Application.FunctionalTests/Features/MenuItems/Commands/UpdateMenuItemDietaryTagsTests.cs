using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.MenuItems.Commands.UpdateMenuItemDietaryTags;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.TagEntity.ValueObjects;

namespace YummyZoom.Application.FunctionalTests.Features.MenuItems.Commands;

using static Testing;

public class UpdateMenuItemDietaryTagsTests : BaseTestFixture
{
    [Test]
    public async Task UpdateDietaryTags_WithTags_ShouldReplaceAndPersist()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var burgerId = Testing.TestData.GetMenuItemId("Classic Beef Burger");
        var tag1 = Guid.NewGuid();
        var tag2 = Guid.NewGuid();

        var cmd = new UpdateMenuItemDietaryTagsCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuItemId: burgerId,
            DietaryTagIds: new List<Guid> { tag1, tag2 });

        // Act
        var result = await SendAsync(cmd);

        // Assert
        result.ShouldBeSuccessful();
        var item = await FindAsync<MenuItem>(MenuItemId.Create(burgerId));
        item!.DietaryTagIds.Should().BeEquivalentTo(new List<TagId> { TagId.Create(tag1), TagId.Create(tag2) });
    }

    [Test]
    public async Task UpdateDietaryTags_EmptyList_ShouldClear()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff2@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var burgerId = Testing.TestData.GetMenuItemId("Classic Beef Burger");

        var cmd = new UpdateMenuItemDietaryTagsCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuItemId: burgerId,
            DietaryTagIds: new List<Guid>());

        // Act
        var result = await SendAsync(cmd);

        // Assert
        result.ShouldBeSuccessful();
        var item = await FindAsync<MenuItem>(MenuItemId.Create(burgerId));
        item!.DietaryTagIds.Should().BeEmpty();
    }

    [Test]
    public async Task UpdateDietaryTags_NullList_ShouldClear()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff3@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var burgerId = Testing.TestData.GetMenuItemId("Classic Beef Burger");

        var cmd = new UpdateMenuItemDietaryTagsCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuItemId: burgerId,
            DietaryTagIds: null);

        // Act
        var result = await SendAsync(cmd);

        // Assert
        result.ShouldBeSuccessful();
        var item = await FindAsync<MenuItem>(MenuItemId.Create(burgerId));
        item!.DietaryTagIds.Should().BeEmpty();
    }

    [Test]
    public async Task UpdateDietaryTags_NonExistentItem_ShouldReturnNotFound()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff4@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var cmd = new UpdateMenuItemDietaryTagsCommand(
            RestaurantId: Testing.TestData.DefaultRestaurantId,
            MenuItemId: Guid.NewGuid(),
            DietaryTagIds: new List<Guid> { Guid.NewGuid() });

        // Act
        var result = await SendAsync(cmd);

        // Assert
        result.ShouldBeFailure("MenuItem.MenuItemNotFound");
    }

    [Test]
    public async Task UpdateDietaryTags_WrongRestaurantScope_ShouldThrowForbidden()
    {
        // Arrange: switch to staff of another restaurant
        var secondRestaurantId = await CreateSecondRestaurantAsync();
        await RunAsRestaurantStaffAsync("staff-second@restaurant.com", secondRestaurantId);
        var burgerId = Testing.TestData.GetMenuItemId("Classic Beef Burger");

        var cmd = new UpdateMenuItemDietaryTagsCommand(
            RestaurantId: secondRestaurantId,
            MenuItemId: burgerId,
            DietaryTagIds: new List<Guid> { Guid.NewGuid() });

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(cmd))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task UpdateDietaryTags_InvalidIds_ShouldFailValidation()
    {
        // Arrange
        await RunAsRestaurantStaffAsync("staff5@restaurant.com", Testing.TestData.DefaultRestaurantId);

        // Act & Assert
        await FluentActions.Invoking(() => SendAsync(new UpdateMenuItemDietaryTagsCommand(
            RestaurantId: Guid.Empty,
            MenuItemId: Guid.Empty,
            DietaryTagIds: new List<Guid> { Guid.Empty, Guid.NewGuid(), Guid.NewGuid() })))
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

