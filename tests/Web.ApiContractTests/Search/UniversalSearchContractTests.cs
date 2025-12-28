using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Search.Queries.UniversalSearch;
using YummyZoom.Web.ApiContractTests.Infrastructure;
using Error = YummyZoom.SharedKernel.Error;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Web.ApiContractTests.Search;

/// <summary>
/// Contract tests for GET /api/v1/search and facets behavior.
/// Verifies query binding, response envelope, pagination and ProblemDetails mapping.
/// </summary>
public class UniversalSearchContractTests
{
    private static SearchResultDto CreateResult(Guid id, string type = "restaurant")
        => new(
            Id: id,
            Type: type,
            RestaurantId: type == "restaurant" ? id : Guid.NewGuid(),
            Name: type == "restaurant" ? "Cafe Aroma" : "Margherita Pizza",
            DescriptionSnippet: "Tasty and fresh",
            Cuisine: "italian",
            Score: 0.89,
            DistanceKm: 1.2,
            Badges: new List<SearchBadgeDto> { new("open-now", "Open Now"), new("top-rated", "Top Rated") },
            Reason: "High relevance and nearby");

    [Test]
    public async Task UniversalSearch_WithMinimalParams_Returns200_WithPageAndEmptyFacets()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        var items = new List<SearchResultDto> { CreateResult(Guid.NewGuid()) };
        var page = new PaginatedList<SearchResultDto>(items, count: 1, pageNumber: 1, pageSize: 10);
        var response = new UniversalSearchResponseDto(page, new FacetBlock(
            Array.Empty<FacetCount<string>>(),
            Array.Empty<FacetCount<string>>(),
            Array.Empty<FacetCount<short>>(),
            0));

        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<UniversalSearchQuery>();
            var q = (UniversalSearchQuery)req;
            q.Term.Should().BeNull();
            q.Latitude.Should().BeNull();
            q.Longitude.Should().BeNull();
            q.OpenNow.Should().BeNull();
            (q.Cuisines ?? Array.Empty<string>()).Should().BeEmpty();
            (q.Tags ?? Array.Empty<string>()).Should().BeEmpty();
            (q.PriceBands ?? Array.Empty<short>()).Should().BeEmpty();
            q.IncludeFacets.Should().BeFalse();
            q.PageNumber.Should().Be(1);
            q.PageSize.Should().Be(10);
            return Result.Success(response);
        });

        var path = "/api/v1/search"; // defaults apply
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        resp.Content.Headers.ContentType?.MediaType.Should().StartWith("application/json");

        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;
        var pageEl = root.GetProperty("page");
        pageEl.GetProperty("pageNumber").GetInt32().Should().Be(1);
        pageEl.GetProperty("items").GetArrayLength().Should().Be(1);

        // facets omitted => empty arrays and zero count
        var facets = root.GetProperty("facets");
        facets.GetProperty("cuisines").GetArrayLength().Should().Be(0);
        facets.GetProperty("tags").GetArrayLength().Should().Be(0);
        facets.GetProperty("priceBands").GetArrayLength().Should().Be(0);
        facets.GetProperty("openNowCount").GetInt32().Should().Be(0);

        factory.Sender.LastRequest.Should().BeOfType<UniversalSearchQuery>();
    }

    [Test]
    public async Task UniversalSearch_WithAllParams_IncludeFacets_Returns200_WithFacets()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        var items = new List<SearchResultDto>
        {
            CreateResult(Guid.NewGuid(), "menu-item"),
            CreateResult(Guid.NewGuid(), "restaurant")
        };
        var page = new PaginatedList<SearchResultDto>(items, count: 2, pageNumber: 2, pageSize: 2);
        var response = new UniversalSearchResponseDto(
            page,
            new FacetBlock(
                new List<FacetCount<string>> { new("italian", 5), new("thai", 3) },
                new List<FacetCount<string>> { new("spicy", 4), new("vegan", 2) },
                new List<FacetCount<short>> { new(1, 7), new(2, 1) },
                9));

        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<UniversalSearchQuery>();
            var q = (UniversalSearchQuery)req;
            q.Term.Should().Be("piz");
            q.Latitude.Should().Be(40.0);
            q.Longitude.Should().Be(-74.0);
            q.OpenNow.Should().BeTrue();
            q.Cuisines.Should().BeEquivalentTo(new[] { "italian", "thai" });
            q.Tags.Should().BeEquivalentTo(new[] { "spicy", "vegan" });
            q.PriceBands.Should().BeEquivalentTo(new short[] { 1, 2 });
            q.IncludeFacets.Should().BeTrue();
            q.PageNumber.Should().Be(2);
            q.PageSize.Should().Be(2);
            return Result.Success(response);
        });

        var path = "/api/v1/search?term=piz&lat=40&lon=-74&openNow=true"
                 + "&cuisines=italian&cuisines=thai"
                 + "&tags=spicy&tags=vegan"
                 + "&priceBands=1&priceBands=2"
                 + "&includeFacets=true&pageNumber=2&pageSize=2";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;
        root.GetProperty("page").GetProperty("items").GetArrayLength().Should().Be(2);
        var facets = root.GetProperty("facets");
        facets.GetProperty("cuisines").GetArrayLength().Should().Be(2);
        facets.GetProperty("tags").GetArrayLength().Should().Be(2);
        facets.GetProperty("priceBands").GetArrayLength().Should().Be(2);
        facets.GetProperty("openNowCount").GetInt32().Should().Be(9);
    }

    [Test]
    public async Task DEBUG_UniversalSearch_SuccessCase()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        bool senderCalled = false;

        // Test with a successful case 
        factory.Sender.RespondWith(req =>
        {
            senderCalled = true;
            TestContext.WriteLine($"Sender called with request: {req?.GetType()?.Name}");

            var items = new List<SearchResultDto> { CreateResult(Guid.NewGuid()) };
            var page = new PaginatedList<SearchResultDto>(items, count: 1, pageNumber: 1, pageSize: 10);
            var response = new UniversalSearchResponseDto(page, new FacetBlock(
                Array.Empty<FacetCount<string>>(),
                Array.Empty<FacetCount<string>>(),
                Array.Empty<FacetCount<short>>(),
                0));

            return Result.Success(response);
        });

        var path = "/api/v1/search"; // minimal request
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine($"Content Length: {raw.Length}");
        TestContext.WriteLine($"Sender was called: {senderCalled}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        senderCalled.Should().BeTrue();
    }

    [Test]
    public async Task DEBUG_UniversalSearch_SimpleFailureCase()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        bool senderCalled = false;

        // Test with a simple failure case that should definitely work
        factory.Sender.RespondWith(req =>
        {
            senderCalled = true;
            TestContext.WriteLine($"Sender called with request: {req?.GetType()?.Name}");
            return Result.Failure<UniversalSearchResponseDto>(Error.Validation("Test.Code", "Test message"));
        });

        var path = "/api/v1/search"; // minimal request
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine($"Content: '{raw}'");
        TestContext.WriteLine($"Content Length: {raw.Length}");
        TestContext.WriteLine($"Content Type: {resp.Content.Headers.ContentType}");
        TestContext.WriteLine($"Headers: {string.Join(", ", resp.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}"))}");
        TestContext.WriteLine($"Sender was called: {senderCalled}");

        // Just verify we get a 400 for now
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UniversalSearch_WhenValidationFails_Returns400Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        factory.Sender.RespondWith(_ =>
            Result.Failure<UniversalSearchResponseDto>(Error.Validation("Search.Invalid", "Bad query")));

        var path = "/api/v1/search?pageNumber=0&pageSize=1000"; // intentionally invalid but enforcement simulated by stub
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");
        TestContext.WriteLine($"Content Length: {raw.Length}");
        TestContext.WriteLine($"Content Type: {resp.Content.Headers.ContentType}");

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Debug: Check if response is empty before trying to deserialize
        if (string.IsNullOrEmpty(raw))
        {
            Assert.Fail("Response body is empty - expected ProblemDetails JSON");
        }

        var prob = JsonSerializer.Deserialize<ProblemDetails>(raw);
        prob!.Status.Should().Be(400);
        prob.Title.Should().Be("Search.Invalid");
    }
}
