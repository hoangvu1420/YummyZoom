using System.Net;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Application.Admin.Queries.GetPlatformMetricsSummary;
using YummyZoom.Application.Admin.Queries.GetPlatformTrends;
using YummyZoom.Application.Admin.Queries.ListRestaurantsForAdmin;
using YummyZoom.Application.Common.Models;
using YummyZoom.Web.ApiContractTests.Infrastructure;
using SharedResult = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Web.ApiContractTests.Admin;

public sealed class AdminDashboardContractTests
{
    [Test]
    public async Task GetSummary_WhenAuthorized_ReturnsSnapshot()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "admin-1");
        client.DefaultRequestHeaders.Add("x-test-roles", "Administrator");

        var expected = new AdminPlatformMetricsSummaryDto(
            TotalOrders: 123,
            ActiveOrders: 7,
            DeliveredOrders: 95,
            GrossMerchandiseVolume: 7420.55m,
            TotalRefunds: 210.10m,
            ActiveRestaurants: 52,
            ActiveCustomers: 3400,
            OpenSupportTickets: 9,
            TotalReviews: 8800,
            LastOrderAtUtc: DateTime.UtcNow.AddMinutes(-5),
            UpdatedAtUtc: DateTime.UtcNow);

        factory.Sender.RespondWith(request =>
        {
            request.Should().BeOfType<GetPlatformMetricsSummaryQuery>();
            return SharedResult.Success(expected);
        });

        var response = await client.GetAsync("/api/v1/admin/dashboard/summary");
        var payload = await response.Content.ReadAsStringAsync();

        TestContext.WriteLine($"RESPONSE {(int)response.StatusCode} {response.StatusCode}\n{payload}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;
        root.GetProperty("totalOrders").GetInt32().Should().Be(123);
        root.GetProperty("grossMerchandiseVolume").GetDecimal().Should().Be(7420.55m);
        root.GetProperty("openSupportTickets").GetInt32().Should().Be(9);
        root.GetProperty("lastOrderAtUtc").GetDateTime().Should().BeCloseTo(expected.LastOrderAtUtc!.Value, TimeSpan.FromSeconds(1));
    }

    [Test]
    public async Task GetSummary_WhenMissingAdminRole_Returns403()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-2");

        var response = await client.GetAsync("/api/v1/admin/dashboard/summary");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        factory.Sender.LastRequest.Should().BeNull();
    }

    [Test]
    public async Task GetTrends_PassesQueryParametersAndReturnsSeries()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "admin-2");
        client.DefaultRequestHeaders.Add("x-test-roles", "Administrator");

        var expectedList = new List<AdminDailyPerformancePointDto>
        {
            new(DateOnly.Parse("2025-09-01"), 5, 4, 320.10m, 20m, 3, 1, DateTime.UtcNow),
            new(DateOnly.Parse("2025-09-02"), 8, 7, 410.00m, 10m, 2, 0, DateTime.UtcNow)
        };

        factory.Sender.RespondWith(request =>
        {
            request.Should().BeOfType<GetPlatformTrendsQuery>();
            var query = (GetPlatformTrendsQuery)request;
            query.StartDate.Should().Be(DateOnly.Parse("2025-09-01"));
            query.EndDate.Should().Be(DateOnly.Parse("2025-09-02"));
            return SharedResult.Success<IReadOnlyList<AdminDailyPerformancePointDto>>(expectedList);
        });

        var response = await client.GetAsync("/api/v1/admin/dashboard/trends?startDate=2025-09-01&endDate=2025-09-02");
        var payload = await response.Content.ReadAsStringAsync();

        TestContext.WriteLine($"RESPONSE {(int)response.StatusCode} {response.StatusCode}\n{payload}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(payload);
        doc.RootElement.GetArrayLength().Should().Be(2);
        doc.RootElement[0].GetProperty("totalOrders").GetInt32().Should().Be(5);
        doc.RootElement[1].GetProperty("grossMerchandiseVolume").GetDecimal().Should().Be(410.00m);
    }

    [Test]
    public async Task ListRestaurants_ReturnsPagedResponseAndBindsFilters()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "admin-3");
        client.DefaultRequestHeaders.Add("x-test-roles", "Administrator");

        var items = new List<AdminRestaurantHealthSummaryDto>
        {
            new(Guid.NewGuid(), "Alpha", true, true, 10, 45, 600m, 4.5, 320, 12, 120m, DateTime.UtcNow, DateTime.UtcNow),
            new(Guid.NewGuid(), "Beta", true, false, 3, 15, 210m, 3.9, 80, 0, 50m, null, DateTime.UtcNow)
        };
        var page = new PaginatedList<AdminRestaurantHealthSummaryDto>(items, count: 12, pageNumber: 2, pageSize: 5);

        factory.Sender.RespondWith(request =>
        {
            request.Should().BeOfType<ListRestaurantsForAdminQuery>();
            var query = (ListRestaurantsForAdminQuery)request;
            query.PageNumber.Should().Be(2);
            query.PageSize.Should().Be(5);
            query.IsVerified.Should().BeTrue();
            query.IsAcceptingOrders.Should().BeFalse();
            query.MinAverageRating.Should().Be(4);
            query.MaxOutstandingBalance.Should().Be(200m);
            query.Search.Should().Be("Al");
            query.SortBy.Should().Be(AdminRestaurantListSort.LastOrderAscending);
            return SharedResult.Success(page);
        });

        var response = await client.GetAsync("/api/v1/admin/dashboard/restaurants?pageNumber=2&pageSize=5&isVerified=true&isAcceptingOrders=false&minAverageRating=4&maxOutstandingBalance=200&search=Al&sortBy=LastOrderAscending");
        var payload = await response.Content.ReadAsStringAsync();

        TestContext.WriteLine($"RESPONSE {(int)response.StatusCode} {response.StatusCode}\n{payload}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(payload);
        doc.RootElement.GetProperty("totalCount").GetInt32().Should().Be(12);
        doc.RootElement.GetProperty("pageNumber").GetInt32().Should().Be(2);
        doc.RootElement.GetProperty("items").GetArrayLength().Should().Be(2);
        doc.RootElement.GetProperty("items")[0].GetProperty("restaurantName").GetString().Should().Be("Alpha");
    }
}
