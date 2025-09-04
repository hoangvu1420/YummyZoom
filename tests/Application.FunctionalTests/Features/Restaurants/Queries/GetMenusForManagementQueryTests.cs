using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.Restaurants.Queries.Management.GetMenusForManagement;
using YummyZoom.Domain.MenuEntity;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Restaurants.Queries;

[TestFixture]
public class GetMenusForManagementQueryTests : BaseTestFixture
{
    [Test]
    public async Task GetMenusForManagement_WithDefaultData_ShouldReturnMenuWithCounts()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var restaurantId = TestDataFactory.DefaultRestaurantId;

        var result = await SendAsync(new GetMenusForManagementQuery(restaurantId));

        result.ShouldBeSuccessful();
        var list = result.Value;
        list.Should().NotBeNull();
        list.Should().NotBeEmpty();

        var defaultMenu = list.FirstOrDefault(m => m.Name == DefaultTestData.Menu.Name);
        defaultMenu.Should().NotBeNull();
        defaultMenu!.Description.Should().Be(DefaultTestData.Menu.Description);
        defaultMenu.IsEnabled.Should().BeTrue();
        defaultMenu.CategoryCount.Should().Be(TestDataFactory.MenuCategoryIds.Count);
        defaultMenu.ItemCount.Should().Be(TestDataFactory.MenuItemIds.Count);
    }

    [Test]
    public async Task GetMenusForManagement_WithSoftDeletedCategoryAndItem_ShouldExcludeFromCounts()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        // Arrange: create a new menu with 2 categories and 2 items each, soft-delete one category and one item
        var scenario = await MenuTestDataFactory.CreateRestaurantWithMenuAsync(new MenuScenarioOptions
        {
            RestaurantId = TestDataFactory.DefaultRestaurantId,
            EnabledMenu = true,
            CategoryCount = 2,
            ItemGenerator = (categoryId, index) => new[]
            {
                new ItemOptions { Name = $"C{index+1}-ItemA", PriceAmount = 10m },
                new ItemOptions { Name = $"C{index+1}-ItemB", PriceAmount = 12m }
            },
            SoftDeleteCategoryIndexes = new[] { 1 }, // delete second category
            SoftDeleteItemIndexes = new[] { 0 }      // delete first created item overall
        });

        // Act
        var result = await SendAsync(new GetMenusForManagementQuery(scenario.RestaurantId));

        // Assert
        result.ShouldBeSuccessful();
        var list = result.Value;
        var target = list.FirstOrDefault(m => m.MenuId == scenario.MenuId);
        target.Should().NotBeNull();

        // Originally: 2 categories, 4 items. After soft-deletes: 1 category, 1 item.
        // Rationale: we exclude soft-deleted categories from both category and item counts,
        // and we also exclude individually soft-deleted items.
        target!.CategoryCount.Should().Be(1);
        target.ItemCount.Should().Be(1);
    }

    [Test]
    public async Task GetMenusForManagement_MultipleRestaurants_ShouldReturnOnlyRequestedRestaurant()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        // Arrange: create another restaurant and a menu there
        var otherRestaurantId = await TestDataFactory.CreateInactiveRestaurantAsync();
        var createMenu = Menu.Create(RestaurantId.Create(otherRestaurantId), "Other Menu", "Other Desc");
        createMenu.IsSuccess.Should().BeTrue();
        var otherMenu = createMenu.Value;
        otherMenu.ClearDomainEvents();
        await AddAsync(otherMenu);

        // Act: query for default restaurant
        var result = await SendAsync(new GetMenusForManagementQuery(TestDataFactory.DefaultRestaurantId));

        // Assert: should not contain the other menu
        result.ShouldBeSuccessful();
        result.Value.Any(m => m.MenuId == otherMenu.Id.Value || m.Name == "Other Menu").Should().BeFalse();
    }

    [Test]
    public async Task GetMenusForManagement_EmptyRestaurantId_ShouldThrowValidationException()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var act = async () => await SendAsync(new GetMenusForManagementQuery(Guid.Empty));
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task GetMenusForManagement_NonStaffUser_ShouldThrowForbiddenException()
    {
        await RunAsDefaultUserAsync();
        var act = async () => await SendAsync(new GetMenusForManagementQuery(TestDataFactory.DefaultRestaurantId));
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task GetMenusForManagement_WithoutAuthentication_ShouldThrowUnauthorizedException()
    {
        var act = async () => await SendAsync(new GetMenusForManagementQuery(TestDataFactory.DefaultRestaurantId));
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
