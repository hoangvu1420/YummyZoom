using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.Restaurants.Queries.Management.GetMenuItemDetails;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Restaurants.Queries;

[TestFixture]
public class GetMenuItemDetailsQueryTests : BaseTestFixture
{
    [Test]
    public async Task GetMenuItemDetails_WithDefaultItem_ShouldReturnDetailsAndCollections()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var restaurantId = TestDataFactory.DefaultRestaurantId;
        var itemId = TestDataFactory.GetMenuItemId(DefaultTestData.MenuItems.MainDishes.ClassicBurger.Name);
        var categoryId = TestDataFactory.GetMenuCategoryId("Main Dishes");

        var result = await SendAsync(new GetMenuItemDetailsQuery(restaurantId, itemId));

        result.ShouldBeSuccessful();
        var dto = result.Value;
        dto.ItemId.Should().Be(itemId);
        dto.CategoryId.Should().Be(categoryId);
        dto.Name.Should().Be(DefaultTestData.MenuItems.MainDishes.ClassicBurger.Name);
        dto.PriceAmount.Should().Be(DefaultTestData.MenuItems.MainDishes.ClassicBurger.Price);
        dto.PriceCurrency.Should().Be(DefaultTestData.Currency.Default);
        dto.DietaryTagIds.Should().NotBeNull();
        dto.AppliedCustomizations.Should().NotBeNull();

        // Default test data assigns Burger Add-ons group as "Add-ons"
        dto.AppliedCustomizations.Should().Contain(c => c.GroupId == TestDataFactory.CustomizationGroup_BurgerAddOnsId && c.DisplayTitle == "Add-ons");
    }

    [Test]
    public async Task GetMenuItemDetails_WithSoftDeletedItem_ShouldReturnNotFound()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var restaurantId = TestDataFactory.DefaultRestaurantId;
        var itemId = TestDataFactory.GetMenuItemId(DefaultTestData.MenuItems.MainDishes.ClassicBurger.Name);

        await MenuTestDataFactory.SoftDeleteItemAsync(itemId);

        var result = await SendAsync(new GetMenuItemDetailsQuery(restaurantId, itemId));
        result.ShouldBeFailure();
        result.Error.Should().Be(GetMenuItemDetailsErrors.NotFound);
    }

    [Test]
    public async Task GetMenuItemDetails_ItemFromAnotherRestaurant_ShouldReturnNotFound()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var (otherRestaurantId, otherItemId) = await TestDataFactory.CreateSecondRestaurantWithMenuItemsAsync();

        var result = await SendAsync(new GetMenuItemDetailsQuery(TestDataFactory.DefaultRestaurantId, otherItemId));
        result.ShouldBeFailure();
        result.Error.Should().Be(GetMenuItemDetailsErrors.NotFound);
    }

    [Test]
    public async Task GetMenuItemDetails_EmptyIds_ShouldThrowValidationException()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);
        var someId = Guid.NewGuid();
        var act1 = async () => await SendAsync(new GetMenuItemDetailsQuery(Guid.Empty, someId));
        await act1.Should().ThrowAsync<ValidationException>();

        var act2 = async () => await SendAsync(new GetMenuItemDetailsQuery(someId, Guid.Empty));
        await act2.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task GetMenuItemDetails_NonStaffUser_ShouldThrowForbiddenException()
    {
        await RunAsDefaultUserAsync();
        var itemId = TestDataFactory.GetMenuItemId(DefaultTestData.MenuItems.MainDishes.ClassicBurger.Name);
        var act = async () => await SendAsync(new GetMenuItemDetailsQuery(Testing.TestData.DefaultRestaurantId, itemId));
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task GetMenuItemDetails_WithoutAuthentication_ShouldThrowUnauthorizedException()
    {
        var itemId = TestDataFactory.GetMenuItemId(DefaultTestData.MenuItems.MainDishes.ClassicBurger.Name);
        var act = async () => await SendAsync(new GetMenuItemDetailsQuery(Testing.TestData.DefaultRestaurantId, itemId));
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
