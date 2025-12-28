using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.MenuCategories.Commands.ReorderMenuCategories;
using YummyZoom.Domain.MenuEntity;
using YummyZoom.Domain.MenuEntity.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.MenuCategories.Commands;

public class ReorderMenuCategoriesTests : BaseTestFixture
{
    [Test]
    public async Task ReorderMenuCategories_WithValidOrders_ShouldUpdateDisplayOrder()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var categories = await GetDefaultMenuCategoriesAsync();
        categories.Should().NotBeEmpty();

        var appetizersId = Testing.TestData.GetMenuCategoryId("Appetizers");
        var mainsId = Testing.TestData.GetMenuCategoryId("Main Dishes");
        var appetizersCategory = categories.SingleOrDefault(c => c.Id == MenuCategoryId.Create(appetizersId));
        var mainsCategory = categories.SingleOrDefault(c => c.Id == MenuCategoryId.Create(mainsId));
        appetizersCategory.Should().NotBeNull();
        mainsCategory.Should().NotBeNull();

        var reordered = new List<MenuCategory> { mainsCategory!, appetizersCategory! };
        reordered.AddRange(categories.Where(c => c.Id != mainsCategory!.Id && c.Id != appetizersCategory!.Id));

        var command = new ReorderMenuCategoriesCommand(
            Testing.TestData.DefaultRestaurantId,
            reordered.Select((category, index) => new CategoryOrderDto(category.Id.Value, index + 1)).ToList());

        var result = await SendAsync(command);

        result.ShouldBeSuccessful();

        var appetizers = await FindAsync<MenuCategory>(MenuCategoryId.Create(appetizersId));
        var mains = await FindAsync<MenuCategory>(MenuCategoryId.Create(mainsId));

        appetizers!.DisplayOrder.Should().Be(2);
        mains!.DisplayOrder.Should().Be(1);
    }

    [Test]
    public async Task ReorderMenuCategories_WithUnknownCategory_ShouldFail()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var categories = await GetDefaultMenuCategoriesAsync();
        categories.Should().NotBeEmpty();

        var orders = categories
            .Select((category, index) => new CategoryOrderDto(category.Id.Value, index + 1))
            .ToList();
        orders[0] = orders[0] with { CategoryId = Guid.NewGuid() };

        var command = new ReorderMenuCategoriesCommand(
            Testing.TestData.DefaultRestaurantId,
            orders);

        var result = await SendAsync(command);

        result.ShouldBeFailure("Menu.Reorder.CategoryNotFound");
    }

    [Test]
    public async Task ReorderMenuCategories_WithIncompleteList_ShouldFailValidation()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var command = new ReorderMenuCategoriesCommand(
            Testing.TestData.DefaultRestaurantId,
            new List<CategoryOrderDto>
            {
                // Only one category provided; restaurant has more
                new(Testing.TestData.GetMenuCategoryId("Appetizers"), 1)
            });

        var result = await SendAsync(command);

        result.ShouldBeFailure("Menu.Reorder.IncompleteCategoryList");
    }

    [Test]
    public async Task ReorderMenuCategories_WithDuplicateCategoryIds_ShouldFailValidation()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var categoryId = Testing.TestData.GetMenuCategoryId("Appetizers");

        var command = new ReorderMenuCategoriesCommand(
            Testing.TestData.DefaultRestaurantId,
            new List<CategoryOrderDto>
            {
                new(categoryId, 1),
                new(categoryId, 2)
            });

        var act = async () => await SendAsync(command);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ReorderMenuCategories_WithDuplicateDisplayOrders_ShouldFailValidation()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var command = new ReorderMenuCategoriesCommand(
            Testing.TestData.DefaultRestaurantId,
            new List<CategoryOrderDto>
            {
                new(Testing.TestData.GetMenuCategoryId("Appetizers"), 1),
                new(Testing.TestData.GetMenuCategoryId("Main Dishes"), 1)
            });

        var act = async () => await SendAsync(command);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ReorderMenuCategories_WithNonContiguousDisplayOrders_ShouldFail()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var categories = await GetDefaultMenuCategoriesAsync();
        categories.Should().NotBeEmpty();

        var orders = categories
            .Select((category, index) => new CategoryOrderDto(category.Id.Value, index + 1))
            .ToList();
        var lastIndex = orders.Count - 1;
        orders[lastIndex] = orders[lastIndex] with { DisplayOrder = orders.Count + 1 };

        var command = new ReorderMenuCategoriesCommand(
            Testing.TestData.DefaultRestaurantId,
            orders);

        var result = await SendAsync(command);

        result.ShouldBeFailure("Menu.Reorder.InvalidDisplayOrderRange");
    }

    [Test]
    public async Task ReorderMenuCategories_WithEmptyOrders_ShouldFailValidation()
    {
        await RunAsRestaurantStaffAsync("staff@restaurant.com", Testing.TestData.DefaultRestaurantId);

        var command = new ReorderMenuCategoriesCommand(
            Testing.TestData.DefaultRestaurantId,
            new List<CategoryOrderDto>());

        var act = async () => await SendAsync(command);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task ReorderMenuCategories_WithoutAuthorization_ShouldThrowForbidden()
    {
        await RunAsDefaultUserAsync();

        var command = new ReorderMenuCategoriesCommand(
            Testing.TestData.DefaultRestaurantId,
            new List<CategoryOrderDto>
            {
                new(Testing.TestData.GetMenuCategoryId("Appetizers"), 1)
            });

        var act = async () => await SendAsync(command);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task ReorderMenuCategories_WithoutAuthentication_ShouldThrowUnauthorized()
    {
        var command = new ReorderMenuCategoriesCommand(
            Testing.TestData.DefaultRestaurantId,
            new List<CategoryOrderDto>
            {
                new(Testing.TestData.GetMenuCategoryId("Appetizers"), 1)
            });

        var act = async () => await SendAsync(command);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    private static async Task<List<MenuCategory>> GetDefaultMenuCategoriesAsync()
    {
        var menuId = MenuId.Create(Testing.TestData.DefaultMenuId);
        return await TestDatabaseManager.ExecuteInScopeAsync(async db =>
            await db.MenuCategories
                .AsNoTracking()
                .Where(c => c.MenuId == menuId && !c.IsDeleted)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync());
    }
}
