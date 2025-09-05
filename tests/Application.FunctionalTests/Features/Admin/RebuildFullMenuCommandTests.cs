using System.Text.Json;
using YummyZoom.Application.Admin.Commands.RebuildFullMenu;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Infrastructure.Data.Models;
using YummyZoom.Infrastructure.Data.ReadModels.FullMenu;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Admin;

[TestFixture]
public class RebuildFullMenuCommandTests : BaseTestFixture
{
    [Test]
    public async Task RebuildFullMenu_WithDefaultData_ShouldPopulateViewAndBeIdempotent()
    {
        // Arrange
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var cmd = new RebuildFullMenuCommand(restaurantId);

        // Act
        var result = await SendAsync(cmd);

        // Assert
        result.ShouldBeSuccessful();
        var view = await FindAsync<FullMenuView>(restaurantId);
        view.Should().NotBeNull();
        view!.MenuJson.Should().NotBeNullOrWhiteSpace();
        view.LastRebuiltAt.Should().BeAfter(DateTimeOffset.UtcNow.AddMinutes(-5));

        // Act - Run again to test idempotency
        await Task.Delay(10);
        var result2 = await SendAsync(cmd);

        // Assert - Should be idempotent
        result2.ShouldBeSuccessful();
        var view2 = await FindAsync<FullMenuView>(restaurantId);
        view2!.LastRebuiltAt.Should().BeOnOrAfter(view.LastRebuiltAt);
        view2.MenuJson.Should().Contain("\"version\": 1");
    }

    [Test]
    public async Task RebuildFullMenu_ShouldFail_WhenNoEnabledMenuExists()
    {
        // Arrange
        var scenario = await MenuTestDataFactory.CreateRestaurantWithMenuAsync(new MenuScenarioOptions
        {
            RestaurantId = Guid.NewGuid(), // Use unique restaurant ID to avoid conflicts with default test data
            EnabledMenu = false,
            CategoryCount = 0
        });
        var cmd = new RebuildFullMenuCommand(scenario.RestaurantId);

        // Act
        var result = await SendAsync(cmd);

        // Assert
        result.IsFailure.Should().BeTrue();
        var view = await FindAsync<FullMenuView>(scenario.RestaurantId);
        view.Should().BeNull();
    }

    [Test]
    public async Task RebuildFullMenu_ShouldExcludeSoftDeletedEntities()
    {
        // Arrange
        var scenario = await MenuTestDataFactory.CreateRestaurantWithMenuAsync(new MenuScenarioOptions
        {
            RestaurantId = Guid.NewGuid(), // Use unique restaurant ID to avoid conflicts with default test data
            EnabledMenu = true,
            CategoryCount = 2,
            CategoryGenerator = i => ($"Cat-{i}", i + 1),
            ItemGenerator = (catId, i) => new[]
            {
                new ItemOptions { Name = $"Item-{i}-A", PriceAmount = 10m },
                new ItemOptions { Name = $"Item-{i}-B", PriceAmount = 12m }
            },
            SoftDeleteCategoryIndexes = new[] { 1 },
            SoftDeleteItemIndexes = new[] { 0 }
        });
        var cmd = new RebuildFullMenuCommand(scenario.RestaurantId);

        // Act
        var result = await SendAsync(cmd);

        // Assert
        result.ShouldBeSuccessful();
        var view = await FindAsync<FullMenuView>(scenario.RestaurantId);
        view.Should().NotBeNull();
        var json = view!.MenuJson;
        json.Should().Contain("\"categories\"");
        json.Should().NotContain("Cat-1");
        json.Should().NotContain("Item-0-A");
    }

    [Test]
    public async Task RebuildFullMenu_ShouldOrderCategoriesByDisplayOrderThenName()
    {
        // Arrange
        var scenario = await MenuTestDataFactory.CreateRestaurantWithMenuAsync(new MenuScenarioOptions
        {
            RestaurantId = Guid.NewGuid(), // Use unique restaurant ID to avoid conflicts with default test data
            EnabledMenu = true,
            CategoryCount = 3,
            CategoryGenerator = i => i switch
            {
                0 => ("B", 1),
                1 => ("A", 1),
                _ => ("Z", 2)
            },
            ItemGenerator = (cat, i) => Array.Empty<ItemOptions>()
        });
        var cmd = new RebuildFullMenuCommand(scenario.RestaurantId);

        // Act
        var result = await SendAsync(cmd);

        // Assert
        result.ShouldBeSuccessful();
        var view = await FindAsync<FullMenuView>(scenario.RestaurantId);
        view.Should().NotBeNull();
        var json = view!.MenuJson;

        // Parse the JSON to check the category order
        using var document = JsonDocument.Parse(json);
        var categoriesOrder = document.RootElement
            .GetProperty("categories")
            .GetProperty("order")
            .EnumerateArray()
            .Select(id => id.GetGuid())
            .ToList();

        var categoriesById = document.RootElement
            .GetProperty("categories")
            .GetProperty("byId");

        // Get the names in the order they appear in the order array
        var orderedNames = categoriesOrder
            .Select(id => categoriesById.GetProperty(id.ToString()).GetProperty("name").GetString())
            .ToList();

        // Verify the order: A (display order 1), B (display order 1), Z (display order 2)
        orderedNames.Should().Equal("A", "B", "Z");
    }

    [Test]
    public async Task RebuildFullMenu_ShouldOrderItemsAlphabeticallyWithinCategories()
    {
        // Arrange
        var scenario = await MenuTestDataFactory.CreateRestaurantWithMenuAsync(new MenuScenarioOptions
        {
            RestaurantId = Guid.NewGuid(), // Use unique restaurant ID to avoid conflicts with default test data
            EnabledMenu = true,
            CategoryCount = 2,
            CategoryGenerator = i => ($"C{i}", 1),
            ItemGenerator = (catId, idx) => new[]
            {
                new ItemOptions { Name = "Banana", PriceAmount = 5m },
                new ItemOptions { Name = "Apple", PriceAmount = 6m },
                new ItemOptions { Name = "Cherry", PriceAmount = 7m }
            }
        });
        var cmd = new RebuildFullMenuCommand(scenario.RestaurantId);

        // Act
        var result = await SendAsync(cmd);

        // Assert
        result.ShouldBeSuccessful();
        var view = await FindAsync<FullMenuView>(scenario.RestaurantId);
        view.Should().NotBeNull();
        var json = view!.MenuJson;

        // Parse the JSON to check item ordering within categories
        using var document = JsonDocument.Parse(json);
        var categoriesById = document.RootElement
            .GetProperty("categories")
            .GetProperty("byId");

        // Check that items are ordered alphabetically within each category
        foreach (var categoryProperty in categoriesById.EnumerateObject())
        {
            var itemOrder = categoryProperty.Value
                .GetProperty("itemOrder")
                .EnumerateArray()
                .Select(id => id.GetGuid())
                .ToList();

            var itemsById = document.RootElement
                .GetProperty("items")
                .GetProperty("byId");

            var orderedItemNames = itemOrder
                .Select(id => itemsById.GetProperty(id.ToString()).GetProperty("name").GetString())
                .ToList();

            // Verify items are ordered alphabetically: Apple, Banana, Cherry
            orderedItemNames.Should().Equal("Apple", "Banana", "Cherry");
        }
    }

    [Test]
    public async Task RebuildFullMenu_WithDefaultData_ShouldIncludeCustomizationGroups()
    {
        // Arrange
        var restaurantId = TestDataFactory.DefaultRestaurantId;
        // Leverage default customization setup by TestDataFactory (Burger Add-ons with choices)
        var cmd = new RebuildFullMenuCommand(restaurantId);

        // Act
        var result = await SendAsync(cmd);

        // Assert
        result.ShouldBeSuccessful();
        var view = await FindAsync<FullMenuView>(restaurantId);
        view.Should().NotBeNull();
        var json = view!.MenuJson;
        json.Should().Contain("\"customizationGroups\"");
        json.Should().Contain("\"options\"");
        // Expect known choice names from default data
        json.Should().Contain("Extra Cheese");
        json.Should().Contain("Bacon");
    }

    [Test]
    public async Task RebuildFullMenu_WithDietaryTags_ShouldIncludeTagLegend()
    {
        // Arrange
        var scenario = await MenuTestDataFactory.CreateRestaurantWithMenuAsync(new MenuScenarioOptions
        {
            RestaurantId = Guid.NewGuid(), // Use unique restaurant ID to avoid conflicts with default test data
            EnabledMenu = true,
            CategoryCount = 1,
            CategoryGenerator = i => ($"C{i}", 1),
            ItemGenerator = (catId, idx) => new[]
            {
                new ItemOptions { Name = "Veggie", PriceAmount = 8m, TagIds = new List<Guid> { Guid.NewGuid() } }
            }
        });
        var cmd = new RebuildFullMenuCommand(scenario.RestaurantId);

        // Act
        var result = await SendAsync(cmd);

        // Assert
        result.ShouldBeSuccessful();
        var view = await FindAsync<FullMenuView>(scenario.RestaurantId);
        view.Should().NotBeNull();
        var json = view!.MenuJson;
        json.Should().Contain("\"tagLegend\"");
    }

    [Test]
    public async Task RebuildFullMenu_ShouldUpdateExistingView_WhenDataChanges()
    {
        // Arrange
        var scenario = await MenuTestDataFactory.CreateRestaurantWithMenuAsync(new MenuScenarioOptions
        {
            RestaurantId = Guid.NewGuid(), // Use unique restaurant ID to avoid conflicts with default test data
            EnabledMenu = true,
            CategoryCount = 1,
            CategoryGenerator = i => ($"OldName", 1),
            ItemGenerator = (catId, idx) => new[]
            {
                new ItemOptions { Name = "Foo", PriceAmount = 9m }
            }
        });
        var cmd = new RebuildFullMenuCommand(scenario.RestaurantId);

        // Act - Initial rebuild
        var result1 = await SendAsync(cmd);
        result1.ShouldBeSuccessful();
        var before = await FindAsync<FullMenuView>(scenario.RestaurantId);
        before.Should().NotBeNull();

        // Arrange - Change category name
        await MenuTestDataFactory.RenameCategoryAsync(scenario.CategoryIds.First(), "NewName");
        await Task.Delay(10);

        // Act - Rebuild after change
        var result2 = await SendAsync(cmd);

        // Assert
        result2.ShouldBeSuccessful();
        var after = await FindAsync<FullMenuView>(scenario.RestaurantId);
        after!.LastRebuiltAt.Should().BeOnOrAfter(before!.LastRebuiltAt);
        after.MenuJson.Should().Contain("NewName");
        after.MenuJson.Should().NotContain("OldName");
    }

    [Test]
    public async Task RebuildFullMenu_WithEmptyMenu_ShouldHandleEmptyCollections()
    {
        // Arrange
        var scenario = await MenuTestDataFactory.CreateRestaurantWithMenuAsync(new MenuScenarioOptions
        {
            RestaurantId = Guid.NewGuid(), // Use unique restaurant ID to avoid conflicts with default test data
            EnabledMenu = true,
            CategoryCount = 0
        });
        var cmd = new RebuildFullMenuCommand(scenario.RestaurantId);

        // Act
        var result = await SendAsync(cmd);

        // Assert
        result.ShouldBeSuccessful();
        var view = await FindAsync<FullMenuView>(scenario.RestaurantId);
        view.Should().NotBeNull();
        var json = view!.MenuJson;
        json.Should().Contain("\"categories\":");
    }
}
