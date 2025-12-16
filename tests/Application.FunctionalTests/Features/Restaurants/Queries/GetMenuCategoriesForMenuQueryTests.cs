using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.Restaurants.Queries.Management.GetMenuCategoriesForMenu;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Restaurants.Queries;

[TestFixture]
public class GetMenuCategoriesForMenuQueryTests : BaseTestFixture
{
    [Test]
    public async Task GetMenuCategoriesForMenu_WithDefaultMenu_ShouldReturnOrderedCategoriesAndCounts()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var result = await SendAsync(new GetMenuCategoriesForMenuQuery(
            TestDataFactory.DefaultRestaurantId,
            TestDataFactory.DefaultMenuId));

        result.ShouldBeSuccessful();
        var categories = result.Value;

        categories.Select(c => c.Name).Should().ContainInOrder("Main Dishes", "Appetizers", "Desserts", "Beverages");
        categories.Select(c => c.DisplayOrder).Should().BeInAscendingOrder();

        categories.First(c => c.Name == "Main Dishes").ItemCount.Should().Be(3);
        categories.First(c => c.Name == "Appetizers").ItemCount.Should().Be(2);
        categories.First(c => c.Name == "Desserts").ItemCount.Should().Be(1);
        categories.First(c => c.Name == "Beverages").ItemCount.Should().Be(2);
    }

    [Test]
    public async Task GetMenuCategoriesForMenu_WithSoftDeletedCategory_ShouldExcludeFromResults()
    {
        var scenario = await MenuTestDataFactory.CreateRestaurantWithMenuAsync(new MenuScenarioOptions
        {
            RestaurantId = Guid.NewGuid(),
            CategoryCount = 2,
            ItemGenerator = (_, idx) => new[]
            {
                new ItemOptions { Name = $"Item-{idx}-1", Description = "desc", PriceAmount = 10m, PriceCurrency = "USD" }
            },
            SoftDeleteCategoryIndexes = new[] { 1 }
        });

        await RunAsRestaurantStaffAsync("staff@restaurant.com", scenario.RestaurantId);

        var result = await SendAsync(new GetMenuCategoriesForMenuQuery(scenario.RestaurantId, scenario.MenuId));

        result.ShouldBeSuccessful();
        var categories = result.Value;
        categories.Should().HaveCount(1);
        categories[0].CategoryId.Should().Be(scenario.CategoryIds[0]);
        categories[0].ItemCount.Should().Be(1);
    }

    [Test]
    public async Task GetMenuCategoriesForMenu_MenuFromAnotherRestaurant_ShouldReturnNotFound()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var otherScenario = await MenuTestDataFactory.CreateRestaurantWithMenuAsync(new MenuScenarioOptions
        {
            RestaurantId = Guid.NewGuid(),
            CategoryCount = 1
        });

        var result = await SendAsync(new GetMenuCategoriesForMenuQuery(TestDataFactory.DefaultRestaurantId, otherScenario.MenuId));

        result.ShouldBeFailure();
        result.Error.Should().Be(GetMenuCategoriesForMenuErrors.MenuNotFound);
    }

    [Test]
    public async Task GetMenuCategoriesForMenu_EmptyIds_ShouldThrowValidationException()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var act1 = async () => await SendAsync(new GetMenuCategoriesForMenuQuery(Guid.Empty, Guid.NewGuid()));
        await act1.Should().ThrowAsync<ValidationException>();

        var act2 = async () => await SendAsync(new GetMenuCategoriesForMenuQuery(Guid.NewGuid(), Guid.Empty));
        await act2.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task GetMenuCategoriesForMenu_NonStaffUser_ShouldThrowForbidden()
    {
        await RunAsDefaultUserAsync();

        var act = async () => await SendAsync(new GetMenuCategoriesForMenuQuery(TestDataFactory.DefaultRestaurantId, TestDataFactory.DefaultMenuId));
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task GetMenuCategoriesForMenu_WithoutAuthentication_ShouldThrowUnauthorized()
    {
        var act = async () => await SendAsync(new GetMenuCategoriesForMenuQuery(TestDataFactory.DefaultRestaurantId, TestDataFactory.DefaultMenuId));
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
