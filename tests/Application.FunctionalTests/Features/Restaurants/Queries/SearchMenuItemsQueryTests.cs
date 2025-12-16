using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.Restaurants.Queries.Management.SearchMenuItems;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Restaurants.Queries;

[TestFixture]
public class SearchMenuItemsQueryTests : BaseTestFixture
{
    [Test]
    public async Task SearchMenuItems_ShouldReturnDeterministicOrder()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var restaurantId = TestDataFactory.DefaultRestaurantId;

        var result = await SendAsync(new SearchMenuItemsQuery(restaurantId, null, null, null, 1, 20));

        result.ShouldBeSuccessful();
        var page = result.Value;
        page.TotalCount.Should().BeGreaterThan(0);
        page.Items.Should().NotBeEmpty();
        page.Items.Select(i => i.Name).ToList().Should().BeInAscendingOrder();
    }

    [Test]
    public async Task SearchMenuItems_WithNameFilter_ShouldReturnSubset()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var restaurantId = TestDataFactory.DefaultRestaurantId;

        var result = await SendAsync(new SearchMenuItemsQuery(restaurantId, null, "Pizza", null, 1, 10));

        result.ShouldBeSuccessful();
        result.Value.Items.Should().OnlyContain(i => i.Name.Contains("Pizza", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public async Task SearchMenuItems_WithCategoryFilter_ShouldLimitToCategory()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var restaurantId = TestDataFactory.DefaultRestaurantId;
        var categoryId = TestDataFactory.GetMenuCategoryId("Beverages");

        var result = await SendAsync(new SearchMenuItemsQuery(restaurantId, categoryId, null, null, 1, 10));

        result.ShouldBeSuccessful();
        result.Value.Items.Should().OnlyContain(i => i.MenuCategoryId == categoryId);
    }

    [Test]
    public async Task SearchMenuItems_WithAvailabilityFilter_ShouldReturnMatchingItems()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var restaurantId = TestDataFactory.DefaultRestaurantId;
        var categoryId = TestDataFactory.GetMenuCategoryId("Main Dishes");

        var burgerId = TestDataFactory.GetMenuItemId(DefaultTestData.MenuItems.MainDishes.ClassicBurger.Name);
        var burger = await FindAsync<MenuItem>(MenuItemId.Create(burgerId));
        burger!.MarkAsUnavailable();
        burger.ClearDomainEvents();
        await UpdateAsync(burger);

        var availableResult = await SendAsync(new SearchMenuItemsQuery(restaurantId, categoryId, null, true, 1, 10));
        availableResult.ShouldBeSuccessful();
        availableResult.Value.Items.Should().NotContain(i => i.ItemId == burgerId);

        var unavailableResult = await SendAsync(new SearchMenuItemsQuery(restaurantId, categoryId, null, false, 1, 10));
        unavailableResult.ShouldBeSuccessful();
        unavailableResult.Value.Items.Should().OnlyContain(i => i.ItemId == burgerId || i.IsAvailable == false);
    }

    [Test]
    public async Task SearchMenuItems_WithSoftDeletedOrForeignCategory_ShouldReturnNotFound()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var restaurantId = TestDataFactory.DefaultRestaurantId;
        var categoryId = TestDataFactory.GetMenuCategoryId("Desserts");

        await MenuTestDataFactory.SoftDeleteCategoryAsync(categoryId);

        var result = await SendAsync(new SearchMenuItemsQuery(restaurantId, categoryId, null, null, 1, 10));
        result.ShouldBeFailure();
        result.Error.Should().Be(SearchMenuItemsErrors.CategoryNotFound);

        var otherRestaurantId = await TestDataFactory.CreateInactiveRestaurantAsync();
        var createOtherCategory = Domain.MenuEntity.MenuCategory.Create(
            Domain.MenuEntity.ValueObjects.MenuId.CreateUnique(),
            "OtherCat",
            1);
        createOtherCategory.IsSuccess.Should().BeTrue();
        var otherCategory = createOtherCategory.Value;
        otherCategory.ClearDomainEvents();
        await AddAsync(otherCategory);

        var foreignResult = await SendAsync(new SearchMenuItemsQuery(restaurantId, otherCategory.Id.Value, null, null, 1, 10));
        foreignResult.ShouldBeFailure();
        foreignResult.Error.Should().Be(SearchMenuItemsErrors.CategoryNotFound);
    }

    [Test]
    public async Task SearchMenuItems_InvalidPagingOrIds_ShouldThrowValidationException()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var act1 = async () => await SendAsync(new SearchMenuItemsQuery(Guid.Empty, null, null, null, 1, 10));
        await act1.Should().ThrowAsync<ValidationException>();

        var act2 = async () => await SendAsync(new SearchMenuItemsQuery(Guid.NewGuid(), null, null, null, 0, 10));
        await act2.Should().ThrowAsync<ValidationException>();

        var act3 = async () => await SendAsync(new SearchMenuItemsQuery(Guid.NewGuid(), null, null, null, 1, 0));
        await act3.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task SearchMenuItems_NonStaffUser_ShouldThrowForbiddenException()
    {
        await RunAsDefaultUserAsync();
        var restaurantId = TestDataFactory.DefaultRestaurantId;

        var act = async () => await SendAsync(new SearchMenuItemsQuery(restaurantId, null, null, null, 1, 10));
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task SearchMenuItems_WithoutAuthentication_ShouldThrowUnauthorizedException()
    {
        var restaurantId = TestDataFactory.DefaultRestaurantId;

        var act = async () => await SendAsync(new SearchMenuItemsQuery(restaurantId, null, null, null, 1, 10));
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
