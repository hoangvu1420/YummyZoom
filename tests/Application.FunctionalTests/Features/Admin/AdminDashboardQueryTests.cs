using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Admin.Queries.GetPlatformMetricsSummary;
using YummyZoom.Application.Admin.Queries.GetPlatformTrends;
using YummyZoom.Application.Admin.Queries.ListRestaurantsForAdmin;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using static YummyZoom.Application.FunctionalTests.Testing;

namespace YummyZoom.Application.FunctionalTests.Features.Admin;

[TestFixture]
public sealed class AdminDashboardQueryTests : BaseTestFixture
{
    [Test]
    public async Task GetPlatformMetricsSummaryQuery_ShouldReturnPersistedSnapshot()
    {
        var updatedAt = DateTime.UtcNow.TruncateToSeconds();
        var lastOrderAt = updatedAt.AddMinutes(-15);

        await TestDatabaseManager.ExecuteInScopeAsync(async db =>
        {
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"AdminPlatformMetricsSnapshots\";");
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "AdminPlatformMetricsSnapshots"
                ("SnapshotId","TotalOrders","ActiveOrders","DeliveredOrders","GrossMerchandiseVolume","TotalRefunds","ActiveRestaurants","ActiveCustomers","OpenSupportTickets","TotalReviews","LastOrderAtUtc","UpdatedAtUtc")
                VALUES ('platform', 120, 5, 90, {5500.25m}, {320.10m}, 45, 1800, 6, 4200, {lastOrderAt}, {updatedAt});
            """);
        });

        var result = await SendAsync(new GetPlatformMetricsSummaryQuery());

        result.ShouldBeSuccessful();
        var snapshot = result.Value;
        snapshot.TotalOrders.Should().Be(120);
        snapshot.ActiveOrders.Should().Be(5);
        snapshot.DeliveredOrders.Should().Be(90);
        snapshot.GrossMerchandiseVolume.Should().Be(5500.25m);
        snapshot.TotalRefunds.Should().Be(320.10m);
        snapshot.ActiveRestaurants.Should().Be(45);
        snapshot.ActiveCustomers.Should().Be(1800);
        snapshot.OpenSupportTickets.Should().Be(6);
        snapshot.TotalReviews.Should().Be(4200);
        snapshot.LastOrderAtUtc.Should().BeCloseTo(lastOrderAt, TimeSpan.FromSeconds(1));
        snapshot.UpdatedAtUtc.Should().BeCloseTo(updatedAt, TimeSpan.FromSeconds(1));
    }

    [Test]
    public async Task GetPlatformTrendsQuery_ShouldRespectRequestedDateWindow()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dayMinus1 = today.AddDays(-1);
        var dayMinus2 = today.AddDays(-2);
        var staleDay = today.AddDays(-5);
        var updatedAt = DateTime.UtcNow.TruncateToSeconds();

        await TestDatabaseManager.ExecuteInScopeAsync(async db =>
        {
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"AdminDailyPerformanceSeries\";");
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "AdminDailyPerformanceSeries"
                ("BucketDate","TotalOrders","DeliveredOrders","GrossMerchandiseVolume","TotalRefunds","NewCustomers","NewRestaurants","UpdatedAtUtc")
                VALUES
                ({AsUtcDateTime(today)}, 5, 4, {250m}, {20m}, 3, 1, {updatedAt}),
                ({AsUtcDateTime(dayMinus1)}, 8, 7, {420m}, {0m}, 2, 0, {updatedAt}),
                ({AsUtcDateTime(dayMinus2)}, 2, 1, {110m}, {5m}, 1, 1, {updatedAt}),
                ({AsUtcDateTime(staleDay)}, 30, 29, {999m}, {0m}, 0, 0, {updatedAt});
            """);
        });

        var query = new GetPlatformTrendsQuery(dayMinus2, today);
        var result = await SendAsync(query);

        result.ShouldBeSuccessful();
        var series = result.Value;
        series.Should().HaveCount(3);
        series.Select(p => p.BucketDate).Should().Contain(new[] { dayMinus2, dayMinus1, today });
        series.Single(p => p.BucketDate == today).TotalOrders.Should().Be(5);
        series.Single(p => p.BucketDate == dayMinus1).GrossMerchandiseVolume.Should().Be(420m);
        foreach (var point in series)
        {
            point.UpdatedAtUtc.Should().BeCloseTo(updatedAt, TimeSpan.FromSeconds(1));
        }
    }

    [Test]
    public async Task ListRestaurantsForAdminQuery_ShouldApplyFiltersAndSorting()
    {
        var updatedAt = DateTime.UtcNow.TruncateToSeconds();
        var now = DateTime.UtcNow;
        var restaurantA = Guid.NewGuid();
        var restaurantB = Guid.NewGuid();
        var restaurantC = Guid.NewGuid();

        await TestDatabaseManager.ExecuteInScopeAsync(async db =>
        {
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"AdminRestaurantHealthSummaries\";");
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO "AdminRestaurantHealthSummaries"
                ("RestaurantId","RestaurantName","IsVerified","IsAcceptingOrders","OrdersLast7Days","OrdersLast30Days","RevenueLast30Days","AverageRating","TotalReviews","CouponRedemptionsLast30Days","OutstandingBalance","LastOrderAtUtc","UpdatedAtUtc")
                VALUES
                ({restaurantA}, 'Alpha Bites', TRUE, TRUE, 12, 55, {870m}, 4.7, 320, 12, {150.25m}, {now.AddHours(-2)}, {updatedAt}),
                ({restaurantB}, 'Bravo Kitchen', FALSE, TRUE, 6, 24, {310m}, 4.0, 120, 4, {20.00m}, {now.AddHours(-10)}, {updatedAt}),
                ({restaurantC}, 'Chroma Cafe', TRUE, FALSE, 2, 10, {95m}, 4.1, 44, 0, {410.00m}, NULL, {updatedAt});
            """);
        });

        var result = await SendAsync(new ListRestaurantsForAdminQuery(
            PageNumber: 1,
            PageSize: 10,
            IsVerified: true,
            IsAcceptingOrders: null,
            MinAverageRating: 3.5,
            MinOrdersLast30Days: null,
            MaxOutstandingBalance: null,
            Search: string.Empty,
            SortBy: AdminRestaurantListSort.RevenueDescending));

        result.ShouldBeSuccessful();
        var page = result.Value;
        page.TotalCount.Should().Be(2);
        page.Items.Should().HaveCount(2);
        page.Items.First().RestaurantName.Should().Be("Alpha Bites");
        page.Items.Last().RestaurantName.Should().Be("Chroma Cafe");
        page.Items.First().RevenueLast30Days.Should().Be(870m);

        var balanceFiltered = await SendAsync(new ListRestaurantsForAdminQuery(
            PageNumber: 1,
            PageSize: 10,
            IsVerified: null,
            IsAcceptingOrders: null,
            MinAverageRating: null,
            MinOrdersLast30Days: null,
            MaxOutstandingBalance: 200m,
            Search: "Kitchen",
            SortBy: AdminRestaurantListSort.OutstandingBalanceAscending));

        balanceFiltered.ShouldBeSuccessful();
        balanceFiltered.Value.TotalCount.Should().Be(1);
        balanceFiltered.Value.Items.Should().ContainSingle();
        balanceFiltered.Value.Items.Single().RestaurantId.Should().Be(restaurantB);
    }

    private static DateTime AsUtcDateTime(DateOnly date) =>
        DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
}

internal static class DateTimeExtensions
{
    public static DateTime TruncateToSeconds(this DateTime value)
        => value.AddTicks(-(value.Ticks % TimeSpan.TicksPerSecond));
}
