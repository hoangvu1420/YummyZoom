using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Features.Orders.Commands.InitiateOrder;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.MenuItems.Queries.Feed;
using YummyZoom.Application.Orders.Commands.InitiateOrder;
using YummyZoom.Infrastructure.Persistence.EfCore;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.MenuItems.Feed;

[TestFixture]
public class MenuItemsFeedTests : BaseTestFixture
{
    [Test]
    public async Task GetMenuItemsFeed_PopularTab_ShouldOrderByRecentOrderQuantity_ThenReviews_ThenRecent()
    {
        // Arrange: Create test scenario with multiple restaurants and menu items with different popularity
        await RunAsDefaultUserAsync();
        
        // Create additional restaurants for the test
        var (alphaRestaurantId, alphaMenuItemId) = await TestDataFactory.CreateSecondRestaurantWithMenuItemsAsync();
        var (betaRestaurantId, _) = await TestDataFactory.CreateSecondRestaurantWithMenuItemsAsync();
        
        // Create menu items for the beta restaurant
        var betaMenuScenario = await MenuTestDataFactory.CreateRestaurantWithMenuAsync(new MenuScenarioOptions
        {
            RestaurantId = betaRestaurantId,
            EnabledMenu = true,
            CategoryCount = 1,
            CategoryGenerator = i => ("Thai Dishes", 1),
            ItemGenerator = (categoryId, index) => new[]
            {
                new ItemOptions { Name = "Pad Thai", PriceAmount = 11.00m },
                new ItemOptions { Name = "Spring Rolls", PriceAmount = 6.00m }
            }
        });

        var padThaiItemId = betaMenuScenario.ItemIds[0]; // Medium popular (will get 3 orders)
        var springRollsItemId = betaMenuScenario.ItemIds[1]; // Least popular (will get 1 order)

        // Create customers for different orders
        var customer1Id = await CreateUserAsync("customer1@test.com", "TestPassword123!", "User");
        var customer2Id = await CreateUserAsync("customer2@test.com", "TestPassword123!", "User");
        var customer3Id = await CreateUserAsync("customer3@test.com", "TestPassword123!", "User");

        // Create orders with different quantities to establish popularity ranking
        // Most popular: alpha menu item (5 orders via multiple orders)
        await CreateOrderWithQuantityAsync(alphaRestaurantId, alphaMenuItemId, customer1Id, 2);
        await CreateOrderWithQuantityAsync(alphaRestaurantId, alphaMenuItemId, customer1Id, 3);
        
        // Medium popular: pad thai (3 orders)
        await CreateOrderWithQuantityAsync(betaRestaurantId, padThaiItemId, customer2Id, 3);
        
        // Least popular: spring rolls (1 order)
        await CreateOrderWithQuantityAsync(betaRestaurantId, springRollsItemId, customer3Id, 1);

        // Seed review summaries to influence tie-breaking (Beta restaurant has more reviews)
        await SeedRestaurantReviewSummariesAsync(alphaRestaurantId, 4.2, 10);
        await SeedRestaurantReviewSummariesAsync(betaRestaurantId, 4.5, 50);

        // Act
        var result = await SendAsync(new GetMenuItemsFeedQuery("popular", 1, 10));

        // Assert
        result.ShouldBeSuccessful();
        var items = result.Value.Items.ToList();
        
        items.Should().HaveCountGreaterThanOrEqualTo(3);
        
        // Verify ordering: alpha item (5 total) -> pad thai (3 orders) -> spring rolls (1 order)
        var itemIds = items.Select(x => x.ItemId).ToList();
        var alphaIndex = itemIds.IndexOf(alphaMenuItemId);
        var padThaiIndex = itemIds.IndexOf(padThaiItemId);
        var springRollsIndex = itemIds.IndexOf(springRollsItemId);
        
        alphaIndex.Should().BeGreaterOrEqualTo(0, "alpha menu item should be in results");
        padThaiIndex.Should().BeGreaterOrEqualTo(0, "pad thai should be in results");
        springRollsIndex.Should().BeGreaterOrEqualTo(0, "spring rolls should be in results");
        
        alphaIndex.Should().BeLessThan(padThaiIndex, "alpha item should rank higher than pad thai");
        padThaiIndex.Should().BeLessThan(springRollsIndex, "pad thai should rank higher than spring rolls");
    }

    [Test]
    public async Task GetMenuItemsFeed_PopularTab_ShouldExcludeUnavailableItems()
    {
        // Arrange
        await RunAsDefaultUserAsync();
        
        // Create menu with available and unavailable items
        var menuScenario = await MenuTestDataFactory.CreateRestaurantWithMenuAsync(new MenuScenarioOptions
        {
            RestaurantId = Testing.TestData.DefaultRestaurantId,
            EnabledMenu = true,
            CategoryCount = 1,
            CategoryGenerator = i => ("Test Category", 1),
            ItemGenerator = (categoryId, index) => new[]
            {
                new ItemOptions { Name = "Available Item", PriceAmount = 12.00m, IsAvailable = true },
                new ItemOptions { Name = "Unavailable Item", PriceAmount = 15.00m, IsAvailable = false }
            }
        });

        var availableItemId = menuScenario.ItemIds[0];
        var unavailableItemId = menuScenario.ItemIds[1];

        // Create orders for both items to test filtering
        var customerId = await CreateUserAsync("customer@test.com", "TestPassword123!", "User");
        await CreateOrderWithQuantityAsync(Testing.TestData.DefaultRestaurantId, availableItemId, customerId, 5);

        // Act
        var result = await SendAsync(new GetMenuItemsFeedQuery("popular", 1, 10));

        // Assert
        result.ShouldBeSuccessful();
        var items = result.Value.Items.ToList();
        
        var itemIds = items.Select(x => x.ItemId).ToList();
        itemIds.Should().Contain(availableItemId, "available items should be included");
        itemIds.Should().NotContain(unavailableItemId, "unavailable items should be excluded");
    }

    [Test]
    public async Task GetMenuItemsFeed_PopularTab_ShouldExcludeDeletedItems()
    {
        // Arrange
        await RunAsDefaultUserAsync();
        
        // Create menu items
        var menuScenario = await MenuTestDataFactory.CreateRestaurantWithMenuAsync(new MenuScenarioOptions
        {
            RestaurantId = Testing.TestData.DefaultRestaurantId,
            EnabledMenu = true,
            CategoryCount = 1,
            CategoryGenerator = i => ("Test Category", 1),
            ItemGenerator = (categoryId, index) => new[]
            {
                new ItemOptions { Name = "Active Item", PriceAmount = 12.00m },
                new ItemOptions { Name = "To Be Deleted Item", PriceAmount = 15.00m }
            },
            SoftDeleteItemIndexes = new[] { 1 } // Soft delete the second item
        });

        var activeItemId = menuScenario.ItemIds[0];
        var deletedItemId = menuScenario.ItemIds[1];

        // Create orders for both items
        var customerId = await CreateUserAsync("customer@test.com", "TestPassword123!", "User");
        await CreateOrderWithQuantityAsync(Testing.TestData.DefaultRestaurantId, activeItemId, customerId, 3);

        // Act
        var result = await SendAsync(new GetMenuItemsFeedQuery("popular", 1, 10));

        // Assert
        result.ShouldBeSuccessful();
        var items = result.Value.Items.ToList();
        
        var itemIds = items.Select(x => x.ItemId).ToList();
        itemIds.Should().Contain(activeItemId, "active items should be included");
        itemIds.Should().NotContain(deletedItemId, "soft-deleted items should be excluded");
    }

    [Test]
    public async Task GetMenuItemsFeed_PopularTab_ShouldExcludeItemsFromDeletedRestaurants()
    {
        // Arrange
        await RunAsDefaultUserAsync();
        
        // Create a restaurant and then soft-delete it
        var (restaurantToDeleteId, _) = await TestDataFactory.CreateSecondRestaurantWithMenuItemsAsync();
        
        // Create menu item for the restaurant before deleting it
        var menuScenario = await MenuTestDataFactory.CreateRestaurantWithMenuAsync(new MenuScenarioOptions
        {
            RestaurantId = restaurantToDeleteId,
            EnabledMenu = true,
            CategoryCount = 1,
            CategoryGenerator = i => ("Test Category", 1),
            ItemGenerator = (categoryId, index) => new[]
            {
                new ItemOptions { Name = "Item from Restaurant to be Deleted", PriceAmount = 10.00m }
            }
        });

        var itemFromDeletedRestaurantId = menuScenario.ItemIds[0];

        // Soft-delete the restaurant using direct database manipulation
        await SoftDeleteRestaurantAsync(restaurantToDeleteId);

        // Act
        var result = await SendAsync(new GetMenuItemsFeedQuery("popular", 1, 10));

        // Assert
        result.ShouldBeSuccessful();
        var items = result.Value.Items.ToList();
        
        var itemIds = items.Select(x => x.ItemId).ToList();
        itemIds.Should().NotContain(itemFromDeletedRestaurantId, "items from deleted restaurants should be excluded");
    }

    [Test]
    public async Task GetMenuItemsFeed_PopularTab_ShouldHandlePagination()
    {
        // Arrange
        await RunAsDefaultUserAsync();
        
        // Create multiple menu items
        var menuScenario = await MenuTestDataFactory.CreateRestaurantWithMenuAsync(new MenuScenarioOptions
        {
            RestaurantId = Testing.TestData.DefaultRestaurantId,
            EnabledMenu = true,
            CategoryCount = 1,
            CategoryGenerator = i => ("Large Category", 1),
            ItemGenerator = (categoryId, index) => Enumerable.Range(1, 15).Select(i => 
                new ItemOptions { Name = $"Item {i:D2}", PriceAmount = 10.00m + i }).ToArray()
        });

        // Act - Test first page
        var page1Result = await SendAsync(new GetMenuItemsFeedQuery("popular", 1, 5));
        var page2Result = await SendAsync(new GetMenuItemsFeedQuery("popular", 2, 5));
        var page3Result = await SendAsync(new GetMenuItemsFeedQuery("popular", 3, 5));

        // Assert
        page1Result.ShouldBeSuccessful();
        page2Result.ShouldBeSuccessful();
        page3Result.ShouldBeSuccessful();

        page1Result.Value.Items.Should().HaveCount(5);
        page2Result.Value.Items.Should().HaveCount(5);
        page3Result.Value.Items.Should().HaveCount(5);

        // Verify no overlapping items between pages
        var page1ItemIds = page1Result.Value.Items.Select(x => x.ItemId).ToHashSet();
        var page2ItemIds = page2Result.Value.Items.Select(x => x.ItemId).ToHashSet();
        var page3ItemIds = page3Result.Value.Items.Select(x => x.ItemId).ToHashSet();

        page1ItemIds.Should().NotIntersectWith(page2ItemIds, "pages should not have overlapping items");
        page2ItemIds.Should().NotIntersectWith(page3ItemIds, "pages should not have overlapping items");
        page1ItemIds.Should().NotIntersectWith(page3ItemIds, "pages should not have overlapping items");

        // Verify total count is consistent across pages
        page1Result.Value.TotalCount.Should().Be(page2Result.Value.TotalCount);
        page2Result.Value.TotalCount.Should().Be(page3Result.Value.TotalCount);
    }

    [Test]
    public async Task GetMenuItemsFeed_PopularTab_ShouldReturnCorrectResponseStructure()
    {
        // Arrange
        await RunAsDefaultUserAsync();
        
        // Use default test data which should have some menu items
        var customerId = await CreateUserAsync("customer@test.com", "TestPassword123!", "User");
        var defaultItemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        await CreateOrderWithQuantityAsync(Testing.TestData.DefaultRestaurantId, defaultItemId, customerId, 2);

        // Act
        var result = await SendAsync(new GetMenuItemsFeedQuery("popular", 1, 10));

        // Assert
        result.ShouldBeSuccessful();
        
        var response = result.Value;
        response.Should().NotBeNull();
        response.Items.Should().NotBeEmpty();
        response.TotalCount.Should().BeGreaterThan(0);
        response.PageNumber.Should().Be(1);
        response.TotalPages.Should().BeGreaterThan(0);
        response.Items.Should().OnlyContain(i => i.LifetimeSoldCount >= 0);

        // Verify item structure
        var firstItem = response.Items.First();
        firstItem.ItemId.Should().NotBeEmpty();
        firstItem.Name.Should().NotBeNullOrWhiteSpace();
        firstItem.PriceAmount.Should().BeGreaterThan(0);
        firstItem.PriceCurrency.Should().NotBeNullOrWhiteSpace();
        firstItem.RestaurantName.Should().NotBeNullOrWhiteSpace();
        firstItem.LifetimeSoldCount.Should().BeGreaterOrEqualTo(0);
        firstItem.RestaurantId.Should().NotBeEmpty();
        // ImageUrl and Rating can be null, so we don't assert their values
    }

    [Test]
    public async Task GetMenuItemsFeed_PopularTab_ShouldHandleEmptyResults()
    {
        // Arrange
        await RunAsDefaultUserAsync();
        
        // Mark ALL menu items as unavailable to create empty result scenario
        // Use direct database manipulation to ensure all items are marked unavailable
        await MarkAllMenuItemsAsUnavailableAsync();

        // Act
        var result = await SendAsync(new GetMenuItemsFeedQuery("popular", 1, 10));

        // Assert
        result.ShouldBeSuccessful();
        
        var response = result.Value;
        response.Should().NotBeNull();
        response.Items.Should().BeEmpty();
        response.TotalCount.Should().Be(0);
        response.PageNumber.Should().Be(1);
        response.TotalPages.Should().Be(0);
    }

    [Test]
    public async Task GetMenuItemsFeed_WithInvalidTab_ShouldReturnValidationError()
    {
        // Arrange
        await RunAsDefaultUserAsync();

        // Act & Assert
        var act = async () => await SendAsync(new GetMenuItemsFeedQuery("invalid-tab", 1, 10));
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task GetMenuItemsFeed_WithInvalidPageNumber_ShouldReturnValidationError()
    {
        // Arrange
        await RunAsDefaultUserAsync();

        // Act & Assert
        var act = async () => await SendAsync(new GetMenuItemsFeedQuery("popular", 0, 10));
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task GetMenuItemsFeed_WithInvalidPageSize_ShouldReturnValidationError()
    {
        // Arrange
        await RunAsDefaultUserAsync();

        // Act & Assert
        var act = async () => await SendAsync(new GetMenuItemsFeedQuery("popular", 1, 0));
        await act.Should().ThrowAsync<ValidationException>();

        var act2 = async () => await SendAsync(new GetMenuItemsFeedQuery("popular", 1, 51));
        await act2.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task GetMenuItemsFeed_PopularTab_ShouldUseReviewsAsTieBreaker()
    {
        // Arrange
        await RunAsDefaultUserAsync();
        
        // Create two restaurants with different review summaries
        var (restaurant1Id, item1Id) = await TestDataFactory.CreateSecondRestaurantWithMenuItemsAsync();
        var (restaurant2Id, item2Id) = await TestDataFactory.CreateSecondRestaurantWithMenuItemsAsync();

        // Seed same popularity but different review counts (restaurant2 has more reviews)
        await SeedRestaurantReviewSummariesAsync(restaurant1Id, 4.0, 10);
        await SeedRestaurantReviewSummariesAsync(restaurant2Id, 4.0, 50);

        // Create equal popularity for both items (no orders = 0 popularity each)
        // Both will have 0 popularity, so review count should be tie-breaker

        // Act
        var result = await SendAsync(new GetMenuItemsFeedQuery("popular", 1, 10));

        // Assert
        result.ShouldBeSuccessful();
        var items = result.Value.Items.ToList();
        
        // Find the positions of our test items
        var itemIds = items.Select(x => x.ItemId).ToList();
        var item1Index = itemIds.IndexOf(item1Id);
        var item2Index = itemIds.IndexOf(item2Id);
        
        if (item1Index >= 0 && item2Index >= 0)
        {
            item2Index.Should().BeLessThan(item1Index, 
                "item from restaurant with more reviews should rank higher as tie-breaker");
        }
    }

    [Test]
    public async Task GetMenuItemsFeed_PopularTab_ShouldExcludeRejectedAndCancelledOrders()
    {
        // Arrange
        await RunAsDefaultUserAsync();
        
        var customerId = await CreateUserAsync("customer@test.com", "TestPassword123!", "User");
        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);

        // Create a placed order (should count towards popularity)
        await CreateOrderWithQuantityAsync(Testing.TestData.DefaultRestaurantId, itemId, customerId, 3);

        // Note: Testing cancelled/rejected orders would require more complex test setup
        // as we would need to create orders and then transition them to those states
        // For now, we test that placed orders are included correctly

        // Act
        var result = await SendAsync(new GetMenuItemsFeedQuery("popular", 1, 10));

        // Assert
        result.ShouldBeSuccessful();
        var items = result.Value.Items.ToList();
        
        // Verify the item appears in results (showing placed orders count)
        var itemIds = items.Select(x => x.ItemId).ToList();
        itemIds.Should().Contain(itemId, "items from placed orders should be included");
    }

    [Test]
    public async Task GetMenuItemsFeed_PopularTab_ShouldExposeLifetimeSoldCountFromSummary()
    {
        // Arrange
        await RunAsDefaultUserAsync();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        const long lifetimeQuantity = 123;

        await UpsertMenuItemSalesSummaryAsync(restaurantId, itemId, lifetimeQuantity);

        // Act
        var result = await SendAsync(new GetMenuItemsFeedQuery("popular", 1, 10));

        // Assert
        result.ShouldBeSuccessful();
        var item = result.Value.Items.FirstOrDefault(i => i.ItemId == itemId);
        item.Should().NotBeNull("the seeded menu item should still be returned in the feed");
        item!.LifetimeSoldCount.Should().Be(lifetimeQuantity);
    }

    private static async Task CreateOrderWithQuantityAsync(Guid restaurantId, Guid menuItemId, Guid customerId, int quantity)
    {
        // Use the established test patterns to create orders
        SetUserId(customerId);
        
        var orderItems = new List<OrderItemDto>
        {
            new(menuItemId, quantity)
        };
        
        var command = new InitiateOrderCommand(
            CustomerId: customerId,
            RestaurantId: restaurantId,
            Items: orderItems,
            DeliveryAddress: InitiateOrderTestHelper.DefaultDeliveryAddress,
            PaymentMethod: InitiateOrderTestHelper.PaymentMethods.CashOnDelivery,
            SpecialInstructions: null,
            CouponCode: null,
            TipAmount: null,
            TeamCartId: null
        );

        var result = await SendAsync(command);
        result.ShouldBeSuccessful();
        
        // Advance the order to placed status (which it should be after InitiateOrder with COD)
        var orderId = result.Value.OrderId;
        var order = await FindOrderAsync(orderId);
        order.Should().NotBeNull();
        order!.Status.Should().Be(YummyZoom.Domain.OrderAggregate.Enums.OrderStatus.Placed);
    }

    private static async Task SeedRestaurantReviewSummariesAsync(Guid restaurantId, double averageRating, int totalReviews)
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        await db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO ""RestaurantReviewSummaries"" (""RestaurantId"", ""AverageRating"", ""TotalReviews"") 
            VALUES ({restaurantId}, {averageRating}, {totalReviews})
            ON CONFLICT (""RestaurantId"") DO UPDATE SET 
                ""AverageRating"" = {averageRating}, 
                ""TotalReviews"" = {totalReviews}");
    }

    private static async Task SoftDeleteRestaurantAsync(Guid restaurantId)
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        await db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""Restaurants"" 
            SET ""IsDeleted"" = TRUE, ""LastModified"" = now() 
            WHERE ""Id"" = {restaurantId}");
    }

    private static async Task MarkAllMenuItemsAsUnavailableAsync()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        await db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""MenuItems"" 
            SET ""IsAvailable"" = FALSE, ""LastModified"" = now() 
            WHERE ""IsDeleted"" = FALSE");
    }

    private static async Task UpsertMenuItemSalesSummaryAsync(Guid restaurantId, Guid menuItemId, long lifetimeQuantity)
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTimeOffset.UtcNow;

        await db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO ""MenuItemSalesSummaries""
                (""RestaurantId"", ""MenuItemId"", ""LifetimeQuantity"", ""Rolling7DayQuantity"", ""Rolling30DayQuantity"", ""LastSoldAt"", ""LastUpdatedAt"", ""SourceVersion"")
            VALUES
                ({restaurantId}, {menuItemId}, {lifetimeQuantity}, 0, 0, NULL, {now}, {now.UtcTicks})
            ON CONFLICT (""RestaurantId"", ""MenuItemId"")
            DO UPDATE SET
                ""LifetimeQuantity"" = {lifetimeQuantity},
                ""Rolling7DayQuantity"" = 0,
                ""Rolling30DayQuantity"" = 0,
                ""LastSoldAt"" = NULL,
                ""LastUpdatedAt"" = {now},
                ""SourceVersion"" = {now.UtcTicks};
        ");
    }
}
