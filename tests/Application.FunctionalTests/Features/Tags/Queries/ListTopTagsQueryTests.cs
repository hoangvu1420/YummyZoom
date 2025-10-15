using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Tags.Queries.ListTopTags;
using YummyZoom.Domain.TagEntity;
using YummyZoom.Domain.TagEntity.Enums;
using YummyZoom.Domain.TagEntity.ValueObjects;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.Common.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Tags.Queries;

[TestFixture]
public class ListTopTagsQueryTests : BaseTestFixture
{
    [Test]
    public async Task ListTopTags_WithDefaultLimit_ShouldReturnOrderedByUsageCount()
    {
        // Arrange
        var (tagIds, restaurantId) = await CreateTestTagsAndRestaurantAsync();
        await CreateMenuItemsWithTagsAsync(restaurantId, tagIds);

        var query = new ListTopTagsQuery(null, 0); // 0 means use default limit (10)

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeSuccessful();
        var topTags = result.Value;
        
        topTags.Should().NotBeEmpty();
        topTags.Should().BeInDescendingOrder(t => t.UsageCount);
        
        // Verify the most used tag is first
        var mostUsedTag = topTags.First();
        mostUsedTag.TagName.Should().Be("Vegetarian");
        mostUsedTag.UsageCount.Should().Be(3);
    }

    [Test]
    public async Task ListTopTags_WithCategoryFilter_ShouldReturnOnlyDietaryTags()
    {
        // Arrange
        var (tagIds, restaurantId) = await CreateTestTagsAndRestaurantAsync();
        await CreateMenuItemsWithTagsAsync(restaurantId, tagIds);

        var query = new ListTopTagsQuery([TagCategory.Dietary], 10);

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeSuccessful();
        var topTags = result.Value;
        
        topTags.Should().NotBeEmpty();
        topTags.Should().OnlyContain(t => t.TagCategory == TagCategory.Dietary);
        
        // Should not contain cuisine tags
        topTags.Should().NotContain(t => t.TagName == "Italian");
    }

    [Test]
    public async Task ListTopTags_WithCustomLimit_ShouldRespectLimit()
    {
        // Arrange
        var (tagIds, restaurantId) = await CreateTestTagsAndRestaurantAsync();
        await CreateMenuItemsWithTagsAsync(restaurantId, tagIds);

        var query = new ListTopTagsQuery(null, 2);

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeSuccessful();
        var topTags = result.Value;
        
        topTags.Should().HaveCount(2);
        topTags.Should().BeInDescendingOrder(t => t.UsageCount);
    }

    [Test]
    public async Task ListTopTags_WithInvalidLimit_ShouldReturnFailure()
    {
        // Arrange
        var query = new ListTopTagsQuery(null, 150); // Over the max limit of 100

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeFailure(ListTopTagsErrors.InvalidLimit.Code);
    }

    [Test]
    public async Task ListTopTags_WithNegativeLimit_ShouldReturnFailure()
    {
        // Arrange
        var query = new ListTopTagsQuery(null, -5);

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeFailure(ListTopTagsErrors.InvalidLimit.Code);
    }

    [Test]
    public async Task ListTopTags_WithDeletedTags_ShouldExcludeDeletedTags()
    {
        // Arrange
        var (tagIds, restaurantId) = await CreateTestTagsAndRestaurantAsync();
        await CreateMenuItemsWithTagsAsync(restaurantId, tagIds);

        // Soft delete one of the tags
        var vegetarianTag = await FindAsync<Tag>(TagId.Create(tagIds["Vegetarian"]));
        vegetarianTag!.MarkAsDeleted(DateTimeOffset.UtcNow);
        await UpdateAsync(vegetarianTag);

        var query = new ListTopTagsQuery(null, 10);

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeSuccessful();
        var topTags = result.Value;
        
        // Should not contain the deleted vegetarian tag
        topTags.Should().NotContain(t => t.TagName == "Vegetarian");
    }

    [Test]
    public async Task ListTopTags_WithUnverifiedRestaurant_ShouldExcludeItemsFromUnverifiedRestaurant()
    {
        // Arrange
        var (tagIds, restaurantId) = await CreateTestTagsAndRestaurantAsync();
        await CreateMenuItemsWithTagsAsync(restaurantId, tagIds);

        // Create an unverified restaurant with items
        var unverifiedRestaurantId = await CreateUnverifiedRestaurantAsync();
        await CreateMenuItemsWithTagsAsync(unverifiedRestaurantId, tagIds);

        var query = new ListTopTagsQuery(null, 10);

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeSuccessful();
        var topTags = result.Value;
        
        // Usage counts should only reflect items from verified restaurants
        var vegetarianTag = topTags.First(t => t.TagName == "Vegetarian");
        vegetarianTag.UsageCount.Should().Be(3); // Only from verified restaurant
    }

    [Test]
    public async Task ListTopTags_WithDeletedMenuItems_ShouldExcludeDeletedItems()
    {
        // Arrange
        var (tagIds, restaurantId) = await CreateTestTagsAndRestaurantAsync();
        var menuItemIds = await CreateMenuItemsWithTagsAsync(restaurantId, tagIds);

        // Soft delete one menu item
        var menuItem = await FindAsync<MenuItem>(MenuItemId.Create(menuItemIds.First()));
        menuItem!.MarkAsDeleted(DateTimeOffset.UtcNow);
        await UpdateAsync(menuItem);

        var query = new ListTopTagsQuery(null, 10);

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeSuccessful();
        var topTags = result.Value;
        
        // Usage counts should be reduced by 1 for tags that were on the deleted item
        var vegetarianTag = topTags.FirstOrDefault(t => t.TagName == "Vegetarian");
        vegetarianTag?.UsageCount.Should().Be(2); // Reduced from 3 to 2
    }

    [Test]
    public async Task ListTopTags_WithEmptyDatabase_ShouldReturnEmptyList()
    {
        // Arrange - no test data created
        var query = new ListTopTagsQuery(null, 10);

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeSuccessful();
        var topTags = result.Value;
        
        topTags.Should().BeEmpty();
    }

    [Test]
    public async Task ListTopTags_OrderingByUsageCountThenByName_ShouldBeDeterministic()
    {
        // Arrange - create tags with same usage count to test secondary ordering
        var (tagIds, restaurantId) = await CreateTestTagsAndRestaurantAsync();
        
        // Create menu items where two tags have the same usage count
        var menuItems = new[]
        {
            await CreateMenuItemWithTagsAsync(restaurantId, [tagIds["Vegan"]]),
            await CreateMenuItemWithTagsAsync(restaurantId, [tagIds["GlutenFree"]])
        };

        var query = new ListTopTagsQuery(null, 10);

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeSuccessful();
        var topTags = result.Value;
        
        topTags.Should().NotBeEmpty();
        
        // Tags with same usage count should be ordered by name
        var sameUsageTags = topTags.Where(t => t.UsageCount == 1).ToList();
        if (sameUsageTags.Count > 1)
        {
            sameUsageTags.Should().BeInAscendingOrder(t => t.TagName);
        }
    }

    [Test]
    public async Task ListTopTags_WithMultipleCategories_ShouldReturnTagsFromAllSpecifiedCategories()
    {
        // Arrange
        var (tagIds, restaurantId) = await CreateTestTagsAndRestaurantAsync();
        await CreateMenuItemsWithTagsAsync(restaurantId, tagIds);

        var query = new ListTopTagsQuery([TagCategory.Dietary, TagCategory.Cuisine], 10);

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeSuccessful();
        var topTags = result.Value;
        
        topTags.Should().NotBeEmpty();
        topTags.Should().OnlyContain(t => t.TagCategory == TagCategory.Dietary || t.TagCategory == TagCategory.Cuisine);
        
        // Should contain both dietary and cuisine tags
        topTags.Should().Contain(t => t.TagCategory == TagCategory.Dietary);
        topTags.Should().Contain(t => t.TagCategory == TagCategory.Cuisine);
        
        // Should not contain other category tags
        topTags.Should().NotContain(t => t.TagCategory == TagCategory.SpiceLevel);
    }

    [Test]
    public async Task ListTopTags_WithSingleCategoryInList_ShouldReturnOnlyThatCategory()
    {
        // Arrange
        var (tagIds, restaurantId) = await CreateTestTagsAndRestaurantAsync();
        await CreateMenuItemsWithTagsAsync(restaurantId, tagIds);

        var query = new ListTopTagsQuery([TagCategory.SpiceLevel], 10);

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeSuccessful();
        var topTags = result.Value;
        
        topTags.Should().NotBeEmpty();
        topTags.Should().OnlyContain(t => t.TagCategory == TagCategory.SpiceLevel);
        
        var spicyTag = topTags.First(t => t.TagName == "Spicy");
        spicyTag.UsageCount.Should().Be(1);
    }

    [Test]
    public async Task ListTopTags_WithEmptyCategories_ShouldReturnAllCategories()
    {
        // Arrange
        var (tagIds, restaurantId) = await CreateTestTagsAndRestaurantAsync();
        await CreateMenuItemsWithTagsAsync(restaurantId, tagIds);

        var query = new ListTopTagsQuery([], 10);

        // Act
        var result = await SendAsync(query);

        // Assert
        result.ShouldBeSuccessful();
        var topTags = result.Value;
        
        topTags.Should().NotBeEmpty();
        
        // Should contain tags from all categories that have usage
        var categories = topTags.Select(t => t.TagCategory).Distinct().ToList();
        categories.Should().Contain(TagCategory.Dietary);
        categories.Should().Contain(TagCategory.Cuisine);
        categories.Should().Contain(TagCategory.SpiceLevel);
    }

    private async Task<(Dictionary<string, Guid> tagIds, Guid restaurantId)> CreateTestTagsAndRestaurantAsync()
    {
        // Create tags
        var vegetarianTag = Tag.Create("Vegetarian", TagCategory.Dietary, "Suitable for vegetarians").Value;
        var veganTag = Tag.Create("Vegan", TagCategory.Dietary, "No animal products").Value;
        var glutenFreeTag = Tag.Create("GlutenFree", TagCategory.Dietary, "Contains no gluten").Value;
        var italianTag = Tag.Create("Italian", TagCategory.Cuisine, "Italian cuisine").Value;
        var spicyTag = Tag.Create("Spicy", TagCategory.SpiceLevel, "Hot and spicy").Value;

        await AddAsync(vegetarianTag);
        await AddAsync(veganTag);
        await AddAsync(glutenFreeTag);
        await AddAsync(italianTag);
        await AddAsync(spicyTag);

        var tagIds = new Dictionary<string, Guid>
        {
            ["Vegetarian"] = vegetarianTag.Id.Value,
            ["Vegan"] = veganTag.Id.Value,
            ["GlutenFree"] = glutenFreeTag.Id.Value,
            ["Italian"] = italianTag.Id.Value,
            ["Spicy"] = spicyTag.Id.Value
        };

        // Create a verified restaurant
        var addressResult = Domain.RestaurantAggregate.ValueObjects.Address.Create(
            "123 Test St", "Test City", "Test State", "12345", "Test Country");
        var contactInfoResult = ContactInfo.Create("+1234567890", "test@restaurant.com");
        var businessHoursResult = BusinessHours.Create("09:00-22:00");
        var geoCoordinatesResult = GeoCoordinates.Create(40.7128, -74.0060);
        
        var restaurant = Restaurant.Create(
            "Test Restaurant",
            logoUrl: null,
            backgroundImageUrl: null,
            "A test restaurant for tag testing",
            "Test Cuisine",
            addressResult.Value,
            contactInfoResult.Value,
            businessHoursResult.Value,
            geoCoordinatesResult.Value).Value;

        restaurant.Verify(); // Make sure the restaurant is verified
        await AddAsync(restaurant);

        return (tagIds, restaurant.Id.Value);
    }

    private async Task<List<Guid>> CreateMenuItemsWithTagsAsync(Guid restaurantId, Dictionary<string, Guid> tagIds)
    {
        var menuItemIds = new List<Guid>();

        // Create menu items with different tag combinations to test usage counts
        // Vegetarian will be used 3 times (most frequent)
        // Vegan, Italian will be used 2 times each
        // GlutenFree, Spicy will be used 1 time each

        var items = new[]
        {
            ("Vegetarian Pasta", new[] { "Vegetarian", "Italian" }),
            ("Vegan Salad", new[] { "Vegan", "Vegetarian" }),
            ("Gluten Free Pizza", new[] { "GlutenFree", "Italian" }),
            ("Spicy Tofu", new[] { "Spicy", "Vegan" }),
            ("Vegetarian Burger", new[] { "Vegetarian" })
        };

        foreach (var (itemName, tagNames) in items)
        {
            var itemId = await CreateMenuItemWithTagsAsync(restaurantId, tagNames.Select(name => tagIds[name]).ToList());
            menuItemIds.Add(itemId);
        }

        return menuItemIds;
    }

    private async Task<Guid> CreateMenuItemWithTagsAsync(Guid restaurantId, List<Guid> tagIds)
    {
        // Create a simple menu category first
        var menu = Domain.MenuEntity.Menu.Create(
            RestaurantId.Create(restaurantId),
            "Test Menu",
            "A test menu for testing").Value;
        await AddAsync(menu);

        var category = Domain.MenuEntity.MenuCategory.Create(
            menu.Id,
            "Test Category",
            1).Value;
        await AddAsync(category);

        var price = new Money(15.99m, "USD");
        var menuItem = MenuItem.Create(
            RestaurantId.Create(restaurantId),
            category.Id,
            $"Test Item {Guid.NewGuid().ToString()[..8]}",
            "A test menu item",
            price,
            imageUrl: null,
            isAvailable: true,
            dietaryTagIds: tagIds.Select(TagId.Create).ToList()).Value;

        await AddAsync(menuItem);
        return menuItem.Id.Value;
    }

    private async Task<Guid> CreateUnverifiedRestaurantAsync()
    {
        var addressResult = Domain.RestaurantAggregate.ValueObjects.Address.Create(
            "456 Unverified St", "Test City", "Test State", "12345", "Test Country");
        var contactInfoResult = ContactInfo.Create("+1234567891", "unverified@restaurant.com");
        var businessHoursResult = BusinessHours.Create("09:00-22:00");
        var geoCoordinatesResult = GeoCoordinates.Create(40.7128, -74.0060);
        
        var restaurant = Restaurant.Create(
            "Unverified Restaurant",
            logoUrl: null,
            backgroundImageUrl: null,
            "An unverified test restaurant",
            "Test Cuisine",
            addressResult.Value,
            contactInfoResult.Value,
            businessHoursResult.Value,
            geoCoordinatesResult.Value).Value;

        // Don't verify this restaurant
        await AddAsync(restaurant);
        return restaurant.Id.Value;
    }
}
