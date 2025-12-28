using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using YummyZoom.Application.Restaurants.Queries.GetFullMenu;
using YummyZoom.SharedKernel;
using YummyZoom.Web.ApiContractTests.Infrastructure;

namespace YummyZoom.Web.ApiContractTests.Restaurants;

/// <summary>
/// Contract tests for GET /api/v1/restaurants/{restaurantId}/menu
/// Tests HTTP caching behavior, status codes, and response format without executing real domain logic.
/// </summary>
public class MenuContractTests
{
    [Test]
    public async Task GetMenu_WhenFound_Returns200WithCachingHeaders()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        // Note: No authentication header needed - this is a public endpoint

        var restaurantId = Guid.NewGuid();
        var rebuiltAt = DateTimeOffset.UtcNow.AddMinutes(-30);
        var menuJson = """{"version":1,"categories":[{"id":"cat1","name":"Appetizers","items":[]}]}""";

        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<GetFullMenuQuery>();
            ((GetFullMenuQuery)req).RestaurantId.Should().Be(restaurantId);
            return Result.Success(new GetFullMenuResponse(menuJson, rebuiltAt));
        });

        var path = $"/api/v1/restaurants/{restaurantId}/menu";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");

        // Assert status and content type
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        // Assert body equals the raw JSON string (not double-encoded)
        raw.Should().Be(menuJson);

        // Assert caching headers are set
        resp.Headers.ETag.Should().NotBeNull();
        resp.Headers.ETag!.ToString().Should().StartWith("W/\"r:");
        resp.Headers.ETag.ToString().Should().Contain($"r:{restaurantId}:t:{rebuiltAt.UtcTicks}");

        resp.Content.Headers.LastModified.Should().NotBeNull();
        resp.Content.Headers.LastModified!.Value.Should().BeCloseTo(rebuiltAt, TimeSpan.FromSeconds(1));

        resp.Headers.CacheControl.Should().NotBeNull();
        resp.Headers.CacheControl!.ToString().Should().Be("public, max-age=300");
    }

    [Test]
    public async Task GetMenu_WithIfNoneMatchHeader_Returns304NotModified()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        // Note: No authentication header needed - this is a public endpoint

        var restaurantId = Guid.NewGuid();
        var rebuiltAt = DateTimeOffset.UtcNow.AddMinutes(-30);
        var menuJson = """{"version":1}""";

        factory.Sender.RespondWith(req =>
        {
            return Result.Success(new GetFullMenuResponse(menuJson, rebuiltAt));
        });

        // Compute expected ETag
        var expectedETag = $"W/\"r:{restaurantId}:t:{rebuiltAt.UtcTicks}\"";
        client.DefaultRequestHeaders.Add("If-None-Match", expectedETag);

        var path = $"/api/v1/restaurants/{restaurantId}/menu";
        TestContext.WriteLine($"REQUEST GET {path} with If-None-Match: {expectedETag}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");

        // Assert 304 with no body
        resp.StatusCode.Should().Be(HttpStatusCode.NotModified);
        raw.Should().BeEmpty();

        // Assert headers are still included
        resp.Headers.ETag.Should().NotBeNull();
        resp.Content.Headers.LastModified.Should().NotBeNull();
        resp.Headers.CacheControl.Should().NotBeNull();
        resp.Headers.CacheControl!.ToString().Should().Be("public, max-age=300");
    }

    [Test]
    public async Task GetMenu_WithIfModifiedSinceHeader_Returns304NotModified()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        // Note: No authentication header needed - this is a public endpoint

        var restaurantId = Guid.NewGuid();
        var rebuiltAt = DateTimeOffset.UtcNow.AddMinutes(-30);
        var menuJson = """{"version":1}""";

        factory.Sender.RespondWith(req =>
        {
            return Result.Success(new GetFullMenuResponse(menuJson, rebuiltAt));
        });

        // Set If-Modified-Since to a time after the rebuild time
        var ifModifiedSince = rebuiltAt.AddMinutes(10);
        client.DefaultRequestHeaders.Add("If-Modified-Since", ifModifiedSince.UtcDateTime.ToString("R"));

        var path = $"/api/v1/restaurants/{restaurantId}/menu";
        TestContext.WriteLine($"REQUEST GET {path} with If-Modified-Since: {ifModifiedSince:R}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");

        // Assert 304 with headers
        resp.StatusCode.Should().Be(HttpStatusCode.NotModified);
        raw.Should().BeEmpty();
        resp.Headers.ETag.Should().NotBeNull();
        resp.Content.Headers.LastModified.Should().NotBeNull();
        resp.Headers.CacheControl!.ToString().Should().Be("public, max-age=300");
    }

    [Test]
    public async Task GetMenu_WhenNotFound_Returns404Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        factory.Sender.RespondWith(_ =>
            Result.Failure<GetFullMenuResponse>(Error.NotFound("Public.GetFullMenu.NotFound", "Missing")));

        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/menu";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var prob = JsonSerializer.Deserialize<ProblemDetails>(raw);
        prob!.Status.Should().Be(404);
        prob.Title.Should().Be("Public.GetFullMenu.NotFound");
    }
}
