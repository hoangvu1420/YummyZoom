using System.Net;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Web.ApiContractTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using YummyZoom.Application.Restaurants.Queries.SearchRestaurants;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Application.Common.Models;
using System.Text.Json;
using Result = YummyZoom.SharedKernel.Result;
using Error = YummyZoom.SharedKernel.Error;

namespace YummyZoom.Web.ApiContractTests.Restaurants;

/// <summary>
/// Contract tests for GET /api/v1/restaurants/search
/// Tests pagination, query parameter binding, and validation error handling without executing real domain logic.
/// </summary>
public class SearchContractTests
{
    private static RestaurantSearchResultDto CreateSearchResult(Guid restaurantId)
        => new(
            restaurantId,
            "Test Restaurant",
            "https://example.com/logo.png",
            new[] { "Italian", "Fast Food" },
            4.5m,
            120,
            "Metro City");

    [Test]
    public async Task SearchRestaurants_WithValidParams_Returns200WithPagination()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        // Note: No authentication header needed - this is a public endpoint
        
        var searchResults = new List<RestaurantSearchResultDto>
        {
            CreateSearchResult(Guid.NewGuid()),
            CreateSearchResult(Guid.NewGuid())
        };
        var paginatedList = new PaginatedList<RestaurantSearchResultDto>(searchResults, 25, 1, 10);
        
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<SearchRestaurantsQuery>();
            var query = (SearchRestaurantsQuery)req;
            query.Q.Should().Be("pizza");
            query.Cuisine.Should().Be("Italian");
            query.PageNumber.Should().Be(1);
            query.PageSize.Should().Be(10);
            query.Lat.Should().Be(40.7128);
            query.Lng.Should().Be(-74.0060);
            query.RadiusKm.Should().Be(5.0);
            return Result.Success(paginatedList);
        });

        var path = "/api/v1/restaurants/search?q=pizza&cuisine=Italian&lat=40.7128&lng=-74.0060&radiusKm=5.0&pageNumber=1&pageSize=10";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");

        // Assert status and content type
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Headers.ContentType?.MediaType.Should().StartWith("application/json");
        
        // Parse and assert pagination structure
        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;
        
        // Assert pagination metadata
        root.GetProperty("pageNumber").GetInt32().Should().Be(1);
        root.GetProperty("totalPages").GetInt32().Should().Be(3); // 25 total / 10 per page = 3 pages
        root.GetProperty("totalCount").GetInt32().Should().Be(25);
        root.GetProperty("hasPreviousPage").GetBoolean().Should().BeFalse();
        root.GetProperty("hasNextPage").GetBoolean().Should().BeTrue();
        
        // Assert items array
        var items = root.GetProperty("items");
        items.ValueKind.Should().Be(JsonValueKind.Array);
        items.GetArrayLength().Should().Be(2);
        
        // Assert item structure with primitive GUIDs
        var firstItem = items[0];
        firstItem.GetProperty("restaurantId").ValueKind.Should().Be(JsonValueKind.String);
        firstItem.GetProperty("name").GetString().Should().Be("Test Restaurant");
        firstItem.GetProperty("logoUrl").GetString().Should().Be("https://example.com/logo.png");
        firstItem.GetProperty("avgRating").GetDecimal().Should().Be(4.5m);
        firstItem.GetProperty("ratingCount").GetInt32().Should().Be(120);
        firstItem.GetProperty("city").GetString().Should().Be("Metro City");
        
        // Assert cuisineTags array
        var cuisineTags = firstItem.GetProperty("cuisineTags");
        cuisineTags.ValueKind.Should().Be(JsonValueKind.Array);
        cuisineTags.GetArrayLength().Should().Be(2);
        cuisineTags[0].GetString().Should().Be("Italian");
        cuisineTags[1].GetString().Should().Be("Fast Food");
        
        // Verify the request mapping
        factory.Sender.LastRequest.Should().BeOfType<SearchRestaurantsQuery>();
    }

    [Test]
    public async Task SearchRestaurants_WithMinimalParams_Returns200()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        // Note: No authentication header needed - this is a public endpoint
        
        var emptyList = new PaginatedList<RestaurantSearchResultDto>(new List<RestaurantSearchResultDto>(), 0, 1, 25);
        
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<SearchRestaurantsQuery>();
            var query = (SearchRestaurantsQuery)req;
            query.Q.Should().BeNull();
            query.Cuisine.Should().BeNull();
            query.Lat.Should().BeNull();
            query.Lng.Should().BeNull();
            query.RadiusKm.Should().BeNull();
            query.PageNumber.Should().Be(1);
            query.PageSize.Should().Be(25);
            return Result.Success(emptyList);
        });

        var path = "/api/v1/restaurants/search?pageNumber=1&pageSize=25";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        
        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;
        root.GetProperty("totalCount").GetInt32().Should().Be(0);
        root.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Test]
    public async Task SearchRestaurants_WithValidationFailure_Returns400Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        
        factory.Sender.RespondWith(_ => 
            Result.Failure<PaginatedList<RestaurantSearchResultDto>>(Error.Validation("Public.Search.Invalid", "Invalid search parameters")));

        var path = "/api/v1/restaurants/search?pageNumber=1&pageSize=25";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var prob = JsonSerializer.Deserialize<ProblemDetails>(raw);
        prob!.Status.Should().Be(400);
        prob.Title.Should().Be("Public");
    }

    [Test]
    public async Task SearchRestaurants_WithInvalidPageSize_Returns400Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        
        factory.Sender.RespondWith(_ => 
            Result.Failure<PaginatedList<RestaurantSearchResultDto>>(Error.Validation("Public.Search.PageSizeInvalid", "Page size must be between 1 and 50")));

        var path = "/api/v1/restaurants/search?pageNumber=1&pageSize=100";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var prob = JsonSerializer.Deserialize<ProblemDetails>(raw);
        prob!.Status.Should().Be(400);
        prob.Title.Should().Be("Public");
    }

    [Test]
    public async Task SearchRestaurants_WithInvalidGeoParams_Returns400Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        
        factory.Sender.RespondWith(_ => 
            Result.Failure<PaginatedList<RestaurantSearchResultDto>>(Error.Validation("Public.Search.GeoInvalid", "Geo parameters must be provided together")));

        // Only providing lat without lng and radiusKm
        var path = "/api/v1/restaurants/search?lat=40.7128&pageNumber=1&pageSize=25";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var prob = JsonSerializer.Deserialize<ProblemDetails>(raw);
        prob!.Status.Should().Be(400);
        prob.Title.Should().Be("Public");
    }
}
