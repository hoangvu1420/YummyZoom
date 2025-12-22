using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.FunctionalTests.TestData;
using YummyZoom.Application.Restaurants.Queries.Management.GetRestaurantDashboardSummary;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Restaurants.Queries;

[TestFixture]
public class GetRestaurantDashboardSummaryQueryTests : BaseTestFixture
{
    [Test]
    public async Task Summary_ShouldReturnAggregatesAndTopItems()
    {
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        await RunAsRestaurantStaffAsync("dashboard@restaurant.com", restaurantId);

        var now = DateTime.UtcNow.TruncateToSeconds();
        var menuItemA = TestDataFactory.GetMenuItemId(DefaultTestData.MenuItems.MainDishes.ClassicBurger.Name);
        var menuItemB = TestDataFactory.GetMenuItemId(DefaultTestData.MenuItems.MainDishes.MargheritaPizza.Name);

        await TestDatabaseManager.ExecuteInScopeAsync(async db =>
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM \"Orders\" WHERE \"RestaurantId\" = {restaurantId};");
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM \"MenuItemSalesSummaries\" WHERE \"RestaurantId\" = {restaurantId};");
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM \"RestaurantReviewSummaries\" WHERE \"RestaurantId\" = {restaurantId};");
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM \"RestaurantAccounts\" WHERE \"RestaurantId\" = {restaurantId};");

            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "Orders" (
                    "Id",
                    "Created",
                    "CreatedBy",
                    "OrderNumber",
                    "Status",
                    "PlacementTimestamp",
                    "LastUpdateTimestamp",
                    "EstimatedDeliveryTime",
                    "ActualDeliveryTime",
                    "SpecialInstructions",
                    "DeliveryAddress_Street",
                    "DeliveryAddress_City",
                    "DeliveryAddress_State",
                    "DeliveryAddress_ZipCode",
                    "DeliveryAddress_Country",
                    "Subtotal_Amount",
                    "Subtotal_Currency",
                    "DiscountAmount_Amount",
                    "DiscountAmount_Currency",
                    "DeliveryFee_Amount",
                    "DeliveryFee_Currency",
                    "TipAmount_Amount",
                    "TipAmount_Currency",
                    "TaxAmount_Amount",
                    "TaxAmount_Currency",
                    "TotalAmount_Amount",
                    "TotalAmount_Currency",
                    "CustomerId",
                    "RestaurantId",
                    "SourceTeamCartId",
                    "AppliedCouponId",
                    "Version")
                VALUES
                    ({Guid.NewGuid()}, {now.AddHours(-1)}, 'seed', 'ORD-TEST-001', 'Placed', {now.AddHours(-1)}, {now.AddHours(-1)}, NULL, NULL, NULL,
                     '123 Road', 'City', 'ST', '00000', 'US',
                     {50m}, 'VND', {0m}, 'VND', {0m}, 'VND', {0m}, 'VND', {0m}, 'VND', {50m}, 'VND',
                     {Testing.TestData.DefaultCustomerId}, {restaurantId}, NULL, NULL, 1),
                    ({Guid.NewGuid()}, {now.AddHours(-2)}, 'seed', 'ORD-TEST-002', 'Accepted', {now.AddHours(-2)}, {now.AddHours(-2)}, NULL, NULL, NULL,
                     '123 Road', 'City', 'ST', '00000', 'US',
                     {75m}, 'VND', {0m}, 'VND', {0m}, 'VND', {0m}, 'VND', {0m}, 'VND', {75m}, 'VND',
                     {Testing.TestData.DefaultCustomerId}, {restaurantId}, NULL, NULL, 1),
                    ({Guid.NewGuid()}, {now.AddDays(-3)}, 'seed', 'ORD-TEST-003', 'Delivered', {now.AddDays(-3)}, {now.AddDays(-3)}, NULL, {now.AddDays(-3)}, NULL,
                     '123 Road', 'City', 'ST', '00000', 'US',
                     {120m}, 'VND', {0m}, 'VND', {0m}, 'VND', {0m}, 'VND', {0m}, 'VND', {120m}, 'VND',
                     {Testing.TestData.DefaultCustomerId}, {restaurantId}, NULL, NULL, 1);
            """);

            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "RestaurantReviewSummaries" (
                    "RestaurantId",
                    "AverageRating",
                    "TotalReviews",
                    "Ratings1",
                    "Ratings2",
                    "Ratings3",
                    "Ratings4",
                    "Ratings5",
                    "TotalWithText",
                    "LastReviewAtUtc",
                    "UpdatedAtUtc")
                VALUES (
                    {restaurantId},
                    {4.6},
                    {120},
                    {1},
                    {3},
                    {10},
                    {25},
                    {81},
                    {60},
                    {now.AddDays(-1)},
                    {now});
            """);

            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "RestaurantAccounts" (
                    "Id",
                    "RestaurantId",
                    "CurrentBalance_Amount",
                    "CurrentBalance_Currency",
                    "PayoutMethod_Details",
                    "Created",
                    "CreatedBy")
                VALUES (
                    {Guid.NewGuid()},
                    {restaurantId},
                    {250000m},
                    'VND',
                    NULL,
                    {now},
                    'seed');
            """);

            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "MenuItemSalesSummaries" (
                    "RestaurantId",
                    "MenuItemId",
                    "LifetimeQuantity",
                    "Rolling7DayQuantity",
                    "Rolling30DayQuantity",
                    "LastSoldAt",
                    "LastUpdatedAt",
                    "SourceVersion")
                VALUES
                    ({restaurantId}, {menuItemA}, 50, 8, 20, {now.AddDays(-2)}, {now}, 1),
                    ({restaurantId}, {menuItemB}, 20, 4, 12, {now.AddDays(-4)}, {now}, 1);
            """);
        });

        var result = await SendAsync(new GetRestaurantDashboardSummaryQuery(restaurantId, TopItemsLimit: 1));

        result.ShouldBeSuccessful();
        result.Value.Restaurant.Id.Should().Be(restaurantId);
        result.Value.Orders.NewCount.Should().Be(1);
        result.Value.Orders.ActiveCount.Should().Be(2);
        result.Value.Orders.LastOrderAtUtc.Should().BeCloseTo(now.AddHours(-1), TimeSpan.FromSeconds(1));
        result.Value.Sales.OrdersLast7Days.Should().Be(3);
        result.Value.Sales.OrdersLast30Days.Should().Be(3);
        result.Value.Sales.RevenueLast7Days.Should().Be(120m);
        result.Value.Sales.RevenueLast30Days.Should().Be(120m);
        result.Value.Reviews.AverageRating.Should().Be(4.6);
        result.Value.Reviews.TotalReviews.Should().Be(120);
        result.Value.Balance.CurrentBalance.Should().Be(250000m);
        result.Value.Balance.Currency.Should().Be("VND");
        result.Value.TopItems.Should().HaveCount(1);
        result.Value.TopItems[0].MenuItemId.Should().Be(menuItemA);
    }

    [Test]
    public async Task Summary_ShouldFallbackWhenOptionalRowsMissing()
    {
        var restaurantId = Testing.TestData.DefaultRestaurantId;
        await RunAsRestaurantStaffAsync("dashboard-missing@restaurant.com", restaurantId);

        await TestDatabaseManager.ExecuteInScopeAsync(async db =>
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM \"Orders\" WHERE \"RestaurantId\" = {restaurantId};");
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM \"MenuItemSalesSummaries\" WHERE \"RestaurantId\" = {restaurantId};");
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM \"RestaurantReviewSummaries\" WHERE \"RestaurantId\" = {restaurantId};");
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM \"RestaurantAccounts\" WHERE \"RestaurantId\" = {restaurantId};");
        });

        var result = await SendAsync(new GetRestaurantDashboardSummaryQuery(restaurantId));

        result.ShouldBeSuccessful();
        result.Value.Orders.NewCount.Should().Be(0);
        result.Value.Orders.ActiveCount.Should().Be(0);
        result.Value.Orders.LastOrderAtUtc.Should().BeNull();
        result.Value.Sales.OrdersLast7Days.Should().Be(0);
        result.Value.Sales.RevenueLast30Days.Should().Be(0);
        result.Value.Reviews.TotalReviews.Should().Be(0);
        result.Value.Reviews.AverageRating.Should().Be(0);
        result.Value.TopItems.Should().BeEmpty();
        result.Value.Balance.CurrentBalance.Should().Be(0);
        result.Value.Balance.Currency.Should().Be("USD");
    }
}

internal static class DateTimeTestExtensions
{
    public static DateTime TruncateToSeconds(this DateTime value)
        => value.AddTicks(-(value.Ticks % TimeSpan.TicksPerSecond));
}
