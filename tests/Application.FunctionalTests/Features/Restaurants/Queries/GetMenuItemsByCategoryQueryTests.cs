using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.Restaurants.Queries.Management.GetMenuItemsByCategory;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Restaurants.Queries;

[TestFixture]
public class GetMenuItemsByCategoryQueryTests : BaseTestFixture
{
    [Test]
    public async Task GetMenuItemsByCategory_WithDefaultCategory_ShouldReturnDeterministicOrder()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var restaurantId = TestDataFactory.DefaultRestaurantId;
        var categoryId = TestDataFactory.GetMenuCategoryId("Beverages"); // has 2 items

        var result = await SendAsync(new GetMenuItemsByCategoryQuery(restaurantId, categoryId, null, null, 1, 10));

        result.ShouldBeSuccessful();
        var page = result.Value;
        page.Should().NotBeNull();
        page.TotalCount.Should().BeGreaterThan(0);
        page.Items.Count.Should().BeGreaterThan(0);

        // Verify sorted by Name ASC
        var names = page.Items.Select(i => i.Name).ToList();
        names.Should().BeInAscendingOrder();
    }

    [Test]
    public async Task GetMenuItemsByCategory_WithNameFilter_ShouldReturnSubset()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var restaurantId = TestDataFactory.DefaultRestaurantId;
        var categoryId = TestDataFactory.GetMenuCategoryId("Main Dishes");

        var result = await SendAsync(new GetMenuItemsByCategoryQuery(restaurantId, categoryId, "Pizza", null, 1, 10));

        result.ShouldBeSuccessful();
        var page = result.Value;
        page.Items.Should().OnlyContain(i => i.Name.Contains("Pizza", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public async Task GetMenuItemsByCategory_WithAvailabilityFilter_ShouldReturnMatchingItems()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var restaurantId = TestDataFactory.DefaultRestaurantId;
        var categoryId = TestDataFactory.GetMenuCategoryId("Main Dishes");

        // Make one item unavailable
        var burgerId = TestDataFactory.GetMenuItemId(DefaultTestData.MenuItems.MainDishes.ClassicBurger.Name);
        var burger = await FindAsync<MenuItem>(MenuItemId.Create(burgerId));
        burger!.MarkAsUnavailable();
        burger.ClearDomainEvents();
        await UpdateAsync(burger);

        var availableResult = await SendAsync(new GetMenuItemsByCategoryQuery(restaurantId, categoryId, null, true, 1, 10));
        availableResult.ShouldBeSuccessful();
        availableResult.Value.Items.Should().NotContain(i => i.ItemId == burgerId);

        var unavailableResult = await SendAsync(new GetMenuItemsByCategoryQuery(restaurantId, categoryId, null, false, 1, 10));
        unavailableResult.ShouldBeSuccessful();
        unavailableResult.Value.Items.Should().OnlyContain(i => i.ItemId == burgerId || i.IsAvailable == false);
    }

    [Test]
    public async Task GetMenuItemsByCategory_WithSoftDeletedCategory_ShouldReturnNotFound()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var restaurantId = TestDataFactory.DefaultRestaurantId;
        var categoryId = TestDataFactory.GetMenuCategoryId("Desserts");

        await MenuTestDataFactory.SoftDeleteCategoryAsync(categoryId);

        var result = await SendAsync(new GetMenuItemsByCategoryQuery(restaurantId, categoryId, null, null, 1, 10));
        result.ShouldBeFailure();
        result.Error.Should().Be(GetMenuItemsByCategoryErrors.NotFound);
    }

    [Test]
    public async Task GetMenuItemsByCategory_CategoryFromAnotherRestaurant_ShouldReturnNotFound()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var otherRestaurantId = await TestDataFactory.CreateInactiveRestaurantAsync();

        var createOtherCategory = Domain.MenuEntity.MenuCategory.Create(
            Domain.MenuEntity.ValueObjects.MenuId.CreateUnique(),
            "OtherCat",
            1);
        createOtherCategory.IsSuccess.Should().BeTrue();
        var otherCategory = createOtherCategory.Value;
        otherCategory.ClearDomainEvents();
        await AddAsync(otherCategory);

        var result = await SendAsync(new GetMenuItemsByCategoryQuery(TestDataFactory.DefaultRestaurantId, otherCategory.Id.Value, null, null, 1, 10));
        result.ShouldBeFailure();
        result.Error.Should().Be(GetMenuItemsByCategoryErrors.NotFound);
    }

    [Test]
    public async Task GetMenuItemsByCategory_InvalidPagingOrIds_ShouldThrowValidationException()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var category = TestDataFactory.GetMenuCategoryId("Main Dishes");

        var act1 = async () => await SendAsync(new GetMenuItemsByCategoryQuery(Guid.Empty, category, null, null, 1, 10));
        await act1.Should().ThrowAsync<ValidationException>();

        var act2 = async () => await SendAsync(new GetMenuItemsByCategoryQuery(Guid.NewGuid(), Guid.Empty, null, null, 1, 10));
        await act2.Should().ThrowAsync<ValidationException>();

        var act3 = async () => await SendAsync(new GetMenuItemsByCategoryQuery(Guid.NewGuid(), Guid.NewGuid(), null, null, 0, 10));
        await act3.Should().ThrowAsync<ValidationException>();

        var act4 = async () => await SendAsync(new GetMenuItemsByCategoryQuery(Guid.NewGuid(), Guid.NewGuid(), null, null, 1, 0));
        await act4.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task GetMenuItemsByCategory_NonStaffUser_ShouldThrowForbiddenException()
    {
        await RunAsDefaultUserAsync();
        var categoryId = TestDataFactory.GetMenuCategoryId("Main Dishes");
        var act = async () => await SendAsync(new GetMenuItemsByCategoryQuery(Testing.TestData.DefaultRestaurantId, categoryId, null, null, 1, 10));
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task GetMenuItemsByCategory_WithoutAuthentication_ShouldThrowUnauthorizedException()
    {
        var categoryId = TestDataFactory.GetMenuCategoryId("Main Dishes");
        var act = async () => await SendAsync(new GetMenuItemsByCategoryQuery(Testing.TestData.DefaultRestaurantId, categoryId, null, null, 1, 10));
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
