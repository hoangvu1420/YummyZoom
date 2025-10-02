using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Application.Restaurants.Queries.GetRestaurantPublicInfo;
using YummyZoom.SharedKernel;
using YummyZoom.Web.ApiContractTests.Infrastructure;

namespace YummyZoom.Web.ApiContractTests.Restaurants;

/// <summary>
/// Contract tests for GET /api/v1/restaurants/{restaurantId}/info
/// Tests status codes, DTO shape, and error handling without executing real domain logic.
/// </summary>
public class InfoContractTests
{
    private static RestaurantPublicInfoDto CreateRestaurantInfo(Guid restaurantId)
        => new(
            restaurantId,
            "Test Restaurant",
            "https://example.com/logo.png",
            new[] { "Italian", "Vegan" },
            true,
            "Metro City");

    [Test]
    public async Task GetRestaurantInfo_WhenFound_Returns200WithDtoShape()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        // Note: No authentication header needed - this is a public endpoint

        var restaurantId = Guid.NewGuid();
        var expectedDto = CreateRestaurantInfo(restaurantId);

        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<GetRestaurantPublicInfoQuery>();
            ((GetRestaurantPublicInfoQuery)req).RestaurantId.Should().Be(restaurantId);
            return Result.Success(expectedDto);
        });

        var path = $"/api/v1/restaurants/{restaurantId}/info";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");

        // Assert status and content type
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Headers.ContentType?.MediaType.Should().StartWith("application/json");

        // Parse and assert DTO shape with primitive GUID
        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;

        // Assert primitive GUID for restaurantId (not wrapped in value object)
        root.GetProperty("restaurantId").ValueKind.Should().Be(JsonValueKind.String);
        root.GetProperty("restaurantId").GetGuid().Should().Be(restaurantId);

        // Assert other fields
        root.GetProperty("name").GetString().Should().Be("Test Restaurant");
        root.GetProperty("logoUrl").GetString().Should().Be("https://example.com/logo.png");
        root.GetProperty("isAcceptingOrders").GetBoolean().Should().BeTrue();
        root.GetProperty("city").GetString().Should().Be("Metro City");

        // Assert cuisineTags is an array
        var cuisineTags = root.GetProperty("cuisineTags");
        cuisineTags.ValueKind.Should().Be(JsonValueKind.Array);
        cuisineTags.GetArrayLength().Should().Be(2);
        cuisineTags[0].GetString().Should().Be("Italian");
        cuisineTags[1].GetString().Should().Be("Vegan");

        // Verify the request mapping
        factory.Sender.LastRequest.Should().BeOfType<GetRestaurantPublicInfoQuery>();
        var lastQuery = (GetRestaurantPublicInfoQuery)factory.Sender.LastRequest!;
        lastQuery.RestaurantId.Should().Be(restaurantId);
    }

    [Test]
    public async Task GetRestaurantInfo_WhenNotFound_Returns404Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        factory.Sender.RespondWith(_ =>
            Result.Failure<RestaurantPublicInfoDto>(Error.NotFound("Public.GetRestaurantPublicInfo.NotFound", "Restaurant info was not found.")));

        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/info";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var prob = JsonSerializer.Deserialize<ProblemDetails>(raw);
        prob!.Status.Should().Be(404);
        prob.Title.Should().Be("Public");
    }
}
