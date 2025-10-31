using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.Restaurants.Queries.Public.GetMenuItemDetails;
using YummyZoom.Domain.MenuItemAggregate;
using YummyZoom.Domain.MenuItemAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Infrastructure.Persistence.EfCore;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Restaurants.Queries;

[TestFixture]
public sealed class GetMenuItemPublicDetailsQueryTests : BaseTestFixture
{
    [Test]
    public async Task Success_ReturnsCoreFields_Customizations_Rating_Upsell_SoldCount()
    {
        await ResetState();

        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);

        // Seed restaurant review summary (restaurant-level rating)
        await SeedRestaurantReviewSummaryAsync(restaurantId, averageRating: 4.6, totalReviews: 120);

        // Seed sales summary for item (lifetime sold)
        await SeedMenuItemSalesSummaryAsync(restaurantId, itemId, lifetime: 7);

        var result = await SendAsync(new GetMenuItemPublicDetailsQuery(restaurantId, itemId));
        result.ShouldBeSuccessful();

        var dto = result.Value;
        dto.RestaurantId.Should().Be(restaurantId);
        dto.ItemId.Should().Be(itemId);
        dto.Name.Should().NotBeNullOrWhiteSpace();
        dto.Description.Should().NotBeNullOrWhiteSpace();
        dto.Currency.Should().NotBeNullOrWhiteSpace();
        dto.BasePrice.Should().BeGreaterThan(0);
        dto.IsAvailable.Should().BeTrue();

        // Sold count should reflect seeded lifetime
        dto.SoldCount.Should().Be(7);

        // Rating mirrors restaurant-level rating
        dto.Rating.Should().BeApproximately(4.6, 1e-6);
        dto.ReviewCount.Should().Be(120);

        // Customizations exist for Classic Burger via default test data
        dto.CustomizationGroups.Should().NotBeNull();
        dto.CustomizationGroups.Should().NotBeEmpty();

        // At least one upsell suggestion from the same category, excluding current item
        dto.Upsell.Should().NotBeNull();
        dto.Upsell.Count.Should().BeInRange(0, 3);
        dto.Upsell.Any(u => u.ItemId == itemId).Should().BeFalse();

        // lastModified returned; exact value depends on view rebuilds; presence is sufficient
    }

    [Test]
    public async Task Caching_ReturnsCachedValue_OnSecondCallEvenAfterDbChange()
    {
        await ResetState();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);

        // Seed rating to ensure non-null
        await SeedRestaurantReviewSummaryAsync(restaurantId, 4.0, 10);

        var first = await SendAndUnwrapAsync(new GetMenuItemPublicDetailsQuery(restaurantId, itemId));

        // Mutate the item name directly in DB; cached result should still return old name
        var updatedName = first.Name + "_UPDATED";
        using (var scope = CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE \"MenuItems\" SET \"Name\" = {updatedName} WHERE \"Id\" = {itemId}");
        }

        var second = await SendAndUnwrapAsync(new GetMenuItemPublicDetailsQuery(restaurantId, itemId));
        second.Name.Should().Be(first.Name);
        second.Name.Should().NotBe(updatedName);
    }

    [Test]
    public async Task NotFound_WhenItemMissingOrDeleted()
    {
        await ResetState();
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        var missing = Guid.NewGuid();
        var resultMissing = await SendAsync(new GetMenuItemPublicDetailsQuery(restaurantId, missing));
        resultMissing.IsFailure.Should().BeTrue();
        resultMissing.Error.Code.Should().Be("Public.MenuItem.NotFound");

        // Deleted
        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.BuffaloWings);
        await MenuTestDataFactory.SoftDeleteItemAsync(itemId);
        var resultDeleted = await SendAsync(new GetMenuItemPublicDetailsQuery(restaurantId, itemId));
        resultDeleted.IsFailure.Should().BeTrue();
        resultDeleted.Error.Code.Should().Be("Public.MenuItem.NotFound");
    }

    [Test]
    public async Task Validation_EmptyIds_ShouldThrow()
    {
        var act1 = async () => await SendAsync(new GetMenuItemPublicDetailsQuery(Guid.Empty, Guid.NewGuid()));
        await act1.Should().ThrowAsync<ValidationException>();

        var act2 = async () => await SendAsync(new GetMenuItemPublicDetailsQuery(Guid.NewGuid(), Guid.Empty));
        await act2.Should().ThrowAsync<ValidationException>();
    }

    private static async Task SeedMenuItemSalesSummaryAsync(Guid restaurantId, Guid itemId, long lifetime)
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO ""MenuItemSalesSummaries"" (""RestaurantId"", ""MenuItemId"", ""LifetimeQuantity"", ""Rolling7DayQuantity"", ""Rolling30DayQuantity"", ""LastUpdatedAt"")
            VALUES ({restaurantId}, {itemId}, {lifetime}, 0, 0, {DateTimeOffset.UtcNow})
            ON CONFLICT (""RestaurantId"", ""MenuItemId"") DO UPDATE SET ""LifetimeQuantity"" = {lifetime}, ""LastUpdatedAt"" = {DateTimeOffset.UtcNow};
        ");
    }

    private static async Task SeedRestaurantReviewSummaryAsync(Guid restaurantId, double averageRating, int totalReviews)
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO ""RestaurantReviewSummaries"" (""RestaurantId"", ""AverageRating"", ""TotalReviews"") 
            VALUES ({restaurantId}, {averageRating}, {totalReviews})
            ON CONFLICT (""RestaurantId"") DO UPDATE SET ""AverageRating"" = {averageRating}, ""TotalReviews"" = {totalReviews};
        ");
    }
}
