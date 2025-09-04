using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.Restaurants.Queries.Management.GetMenuCategoryDetails;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Restaurants.Queries;

[TestFixture]
public class GetMenuCategoryDetailsQueryTests : BaseTestFixture
{
    [Test]
    public async Task GetMenuCategoryDetails_WithDefaultCategory_ShouldReturnDetailsAndItemCount()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var restaurantId = TestDataFactory.DefaultRestaurantId;
        var categoryId = TestDataFactory.GetMenuCategoryId("Main Dishes");

        var result = await SendAsync(new GetMenuCategoryDetailsQuery(restaurantId, categoryId));

        result.ShouldBeSuccessful();
        var dto = result.Value;
        dto.CategoryId.Should().Be(categoryId);
        dto.MenuId.Should().Be(TestDataFactory.DefaultMenuId);
        dto.MenuName.Should().Be(DefaultTestData.Menu.Name);
        dto.Name.Should().Be("Main Dishes");
        dto.ItemCount.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task GetMenuCategoryDetails_WithSoftDeletedItems_ShouldExcludeFromCount()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var restaurantId = TestDataFactory.DefaultRestaurantId;
        var categoryId = TestDataFactory.GetMenuCategoryId("Main Dishes");

        // Soft delete one known item in Main Dishes
        var burgerId = TestDataFactory.GetMenuItemId(DefaultTestData.MenuItems.MainDishes.ClassicBurger.Name);
        await MenuTestDataFactory.SoftDeleteItemAsync(burgerId);

        var result = await SendAsync(new GetMenuCategoryDetailsQuery(restaurantId, categoryId));

        result.ShouldBeSuccessful();
        var dto = result.Value;
        // Default Main Dishes has 3 items; after soft-delete expect 2
        dto.ItemCount.Should().Be(2);
    }

    [Test]
    public async Task GetMenuCategoryDetails_WithSoftDeletedCategory_ShouldReturnNotFound()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var restaurantId = TestDataFactory.DefaultRestaurantId;
        var categoryId = TestDataFactory.GetMenuCategoryId("Desserts");

        await MenuTestDataFactory.SoftDeleteCategoryAsync(categoryId);

        var result = await SendAsync(new GetMenuCategoryDetailsQuery(restaurantId, categoryId));
        result.ShouldBeFailure();
        result.Error.Should().Be(GetMenuCategoryDetailsErrors.NotFound);
    }

    [Test]
    public async Task GetMenuCategoryDetails_CategoryFromAnotherRestaurant_ShouldReturnNotFound()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        // Create second restaurant and its own category
        var otherRestaurantId = await TestDataFactory.CreateInactiveRestaurantAsync();

        var createOtherCategory = Domain.MenuEntity.MenuCategory.Create(
            Domain.MenuEntity.ValueObjects.MenuId.CreateUnique(),
            "OtherCat",
            1);
        createOtherCategory.IsSuccess.Should().BeTrue();
        var otherCategory = createOtherCategory.Value;
        otherCategory.ClearDomainEvents();
        await AddAsync(otherCategory);

        var result = await SendAsync(new GetMenuCategoryDetailsQuery(TestDataFactory.DefaultRestaurantId, otherCategory.Id.Value));
        result.ShouldBeFailure();
        result.Error.Should().Be(GetMenuCategoryDetailsErrors.NotFound);
    }

    [Test]
    public async Task GetMenuCategoryDetails_EmptyIds_ShouldThrowValidationException()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var act1 = async () => await SendAsync(new GetMenuCategoryDetailsQuery(Guid.Empty, Guid.NewGuid()));
        await act1.Should().ThrowAsync<ValidationException>();

        var act2 = async () => await SendAsync(new GetMenuCategoryDetailsQuery(Guid.NewGuid(), Guid.Empty));
        await act2.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task GetMenuCategoryDetails_NonStaffUser_ShouldThrowForbiddenException()
    {
        await RunAsDefaultUserAsync();
        var categoryId = TestDataFactory.GetMenuCategoryId("Main Dishes");
        var act = async () => await SendAsync(new GetMenuCategoryDetailsQuery(Testing.TestData.DefaultRestaurantId, categoryId));
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task GetMenuCategoryDetails_WithoutAuthentication_ShouldThrowUnauthorizedException()
    {
        var categoryId = TestDataFactory.GetMenuCategoryId("Main Dishes");
        var act = async () => await SendAsync(new GetMenuCategoryDetailsQuery(Testing.TestData.DefaultRestaurantId, categoryId));
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
