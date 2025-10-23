using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Authorization;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.TeamCarts.Commands.AddItemToTeamCart;
using YummyZoom.Application.TeamCarts.Queries.GetCouponSuggestions;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.CouponAggregate;
using YummyZoom.Domain.CouponAggregate.ValueObjects;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.TeamCarts.Queries;

[TestFixture]
public class TeamCartCouponSuggestionsTests : BaseTestFixture
{
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await TeamCartRoleTestHelper.SetupTeamCartAuthorizationTestsAsync();
    }

    [Test]
    public async Task GetTeamCartCouponSuggestions_WithValidTeamCartAndCoupons_ShouldReturnSuggestions()
    {
        // Arrange: Create team cart scenario with host and guest
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Alice Host")
            .WithGuest("Bob Guest")
            .BuildAsync();

        // Add items to the cart
        await scenario.ActAsHost();
        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, itemId, 2));

        await scenario.ActAsGuest("Bob Guest");
        await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, itemId, 1));

        // Create a coupon for the restaurant
        await CreateActiveCouponAsync(Testing.TestData.DefaultRestaurantId, "TEAM15", 15m, CouponScope.WholeOrder);

        // Process outbox events to ensure coupon is persisted
        await DrainOutboxAsync();

        // Refresh the materialized view to include the new coupon
        await RefreshMaterializedViewAsync();

        // Act: Get coupon suggestions as host
        await scenario.ActAsHost();
        var result = await SendAsync(new TeamCartCouponSuggestionsQuery(scenario.TeamCartId));

        // Assert
        result.ShouldBeSuccessful();
        var suggestions = result.Value;
        
        suggestions.Should().NotBeNull();
        suggestions.CartSummary.Should().NotBeNull();
        suggestions.CartSummary.ItemCount.Should().Be(3); // 2 + 1 items
        suggestions.CartSummary.Subtotal.Should().BeGreaterThan(0);
        suggestions.CartSummary.Currency.Should().Be("USD");
        
        suggestions.Suggestions.Should().NotBeEmpty();
        suggestions.Suggestions.Should().Contain(s => s.Code == "TEAM15");
        
        var teamCoupon = suggestions.Suggestions.First(s => s.Code == "TEAM15");
        teamCoupon.IsEligible.Should().BeTrue();
        teamCoupon.Savings.Should().BeGreaterThan(0);
        teamCoupon.Scope.Should().Be("WholeOrder");
    }

    [Test]
    public async Task GetTeamCartCouponSuggestions_WithEmptyTeamCart_ShouldReturnEmptyResponse()
    {
        // Arrange: Create empty team cart
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Alice Host")
            .BuildAsync();

        // Act: Get coupon suggestions for empty cart
        await scenario.ActAsHost();
        var result = await SendAsync(new TeamCartCouponSuggestionsQuery(scenario.TeamCartId));

        // Assert
        result.ShouldBeSuccessful();
        var suggestions = result.Value;
        
        suggestions.Should().NotBeNull();
        suggestions.CartSummary.ItemCount.Should().Be(0);
        suggestions.CartSummary.Subtotal.Should().Be(0);
        suggestions.BestDeal.Should().BeNull();
        suggestions.Suggestions.Should().BeEmpty();
    }

    [Test]
    public async Task GetTeamCartCouponSuggestions_WithMinOrderRequirement_ShouldShowIneligibleCoupon()
    {
        // Arrange: Create team cart with small order
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Alice Host")
            .BuildAsync();

        await scenario.ActAsHost();
        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, itemId, 1)); // Small order

        // Create coupon with high minimum order requirement
        await CreateActiveCouponWithMinOrderAsync(Testing.TestData.DefaultRestaurantId, "BIGORDER", 10m, 100m);

        // Process outbox events to ensure coupon is persisted
        await DrainOutboxAsync();

        // Refresh the materialized view to include the new coupon
        await RefreshMaterializedViewAsync();

        // Act
        var result = await SendAsync(new TeamCartCouponSuggestionsQuery(scenario.TeamCartId));

        // Assert
        result.ShouldBeSuccessful();
        var suggestions = result.Value;
        
        suggestions.Suggestions.Should().NotBeEmpty();
        var bigOrderCoupon = suggestions.Suggestions.FirstOrDefault(s => s.Code == "BIGORDER");
        bigOrderCoupon.Should().NotBeNull();
        bigOrderCoupon!.IsEligible.Should().BeFalse();
        bigOrderCoupon.MinOrderGap.Should().BeGreaterThan(0);
        bigOrderCoupon.EligibilityReason.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task GetTeamCartCouponSuggestions_AsNonMember_ShouldThrowForbidden()
    {
        // Arrange: Create team cart
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Alice Host")
            .BuildAsync();

        // Act as a different user who is not a member
        await RunAsUserAsync("outsider@yummyzoom.test", "Outsider User", Array.Empty<string>());

        // Act
        var result = await SendAsync(new TeamCartCouponSuggestionsQuery(scenario.TeamCartId));

        // Assert
        result.ShouldBeFailure("TeamCart.NotMember");
    }

    [Test]
    public async Task GetTeamCartCouponSuggestions_WithNonexistentTeamCart_ShouldReturnNotFound()
    {
        // Arrange
        var nonexistentTeamCartId = Guid.NewGuid();

        // Act
        var result = await SendAsync(new TeamCartCouponSuggestionsQuery(nonexistentTeamCartId));

        // Assert
        result.ShouldBeFailure("TeamCart.NotFound");
    }

    [Test]
    public async Task GetTeamCartCouponSuggestions_WithMultipleCoupons_ShouldReturnBestDeal()
    {
        // Arrange: Create team cart with items
        var scenario = await TeamCartTestBuilder
            .Create(Testing.TestData.DefaultRestaurantId)
            .WithHost("Alice Host")
            .BuildAsync();

        await scenario.ActAsHost();
        var itemId = Testing.TestData.GetMenuItemId(Testing.TestData.MenuItems.ClassicBurger);
        await SendAsync(new AddItemToTeamCartCommand(scenario.TeamCartId, itemId, 3)); // Larger order

        // Create multiple coupons with different values
        await CreateActiveCouponAsync(Testing.TestData.DefaultRestaurantId, "SMALL5", 5m, CouponScope.WholeOrder);
        await CreateActiveCouponAsync(Testing.TestData.DefaultRestaurantId, "MEDIUM10", 10m, CouponScope.WholeOrder);
        await CreateActiveCouponAsync(Testing.TestData.DefaultRestaurantId, "LARGE20", 20m, CouponScope.WholeOrder);

        // Process outbox events to ensure coupons are persisted
        await DrainOutboxAsync();

        // Refresh the materialized view to include the new coupons
        await RefreshMaterializedViewAsync();

        // Act
        var result = await SendAsync(new TeamCartCouponSuggestionsQuery(scenario.TeamCartId));

        // Assert
        result.ShouldBeSuccessful();
        var suggestions = result.Value;
        
        // Should have at least 3 coupons (may have more from test data setup)
        suggestions.Suggestions.Should().HaveCountGreaterOrEqualTo(3);
        suggestions.BestDeal.Should().NotBeNull();
        
        // Verify our test coupons are present
        suggestions.Suggestions.Should().Contain(s => s.Code == "SMALL5");
        suggestions.Suggestions.Should().Contain(s => s.Code == "MEDIUM10");
        suggestions.Suggestions.Should().Contain(s => s.Code == "LARGE20");
        
        // Best deal should be the one with highest savings
        var bestSavings = suggestions.Suggestions.Where(s => s.IsEligible).Max(s => s.Savings);
        suggestions.BestDeal!.Savings.Should().Be(bestSavings);
        suggestions.BestDeal.Code.Should().Be("LARGE20"); // Assuming 20% gives the highest savings
    }

    private static async Task CreateActiveCouponAsync(Guid restaurantId, string code, decimal percentageValue, CouponScope scope)
    {
        var restaurantIdVo = RestaurantId.Create(restaurantId);
        var valueResult = CouponValue.CreatePercentage(percentageValue);
        var appliesToResult = scope switch
        {
            CouponScope.WholeOrder => AppliesTo.CreateForWholeOrder(),
            _ => AppliesTo.CreateForWholeOrder() // Default to whole order for simplicity
        };

        var couponResult = Coupon.Create(
            restaurantIdVo,
            code,
            $"{percentageValue}% off entire order",
            valueResult.Value,
            appliesToResult.Value,
            validityStartDate: DateTime.UtcNow.AddDays(-1),
            validityEndDate: DateTime.UtcNow.AddDays(30),
            minOrderAmount: null,
            totalUsageLimit: null,
            usageLimitPerUser: null,
            isEnabled: true);

        couponResult.ShouldBeSuccessful();
        var coupon = couponResult.Value;
        coupon.ClearDomainEvents();
        await AddAsync(coupon);
    }

    private static async Task CreateActiveCouponWithMinOrderAsync(Guid restaurantId, string code, decimal percentageValue, decimal minOrderAmount)
    {
        var restaurantIdVo = RestaurantId.Create(restaurantId);
        var valueResult = CouponValue.CreatePercentage(percentageValue);
        var appliesToResult = AppliesTo.CreateForWholeOrder();
        var minOrder = new Money(minOrderAmount, "USD");

        var couponResult = Coupon.Create(
            restaurantIdVo,
            code,
            $"{percentageValue}% off orders over ${minOrderAmount}",
            valueResult.Value,
            appliesToResult.Value,
            validityStartDate: DateTime.UtcNow.AddDays(-1),
            validityEndDate: DateTime.UtcNow.AddDays(30),
            minOrderAmount: minOrder,
            totalUsageLimit: null,
            usageLimitPerUser: null,
            isEnabled: true);

        couponResult.ShouldBeSuccessful();
        var coupon = couponResult.Value;
        coupon.ClearDomainEvents();
        await AddAsync(coupon);
    }

    private static async Task RefreshMaterializedViewAsync()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<YummyZoom.Infrastructure.Persistence.EfCore.ApplicationDbContext>();
        await db.Database.ExecuteSqlRawAsync("REFRESH MATERIALIZED VIEW active_coupons_view;");
    }
}
