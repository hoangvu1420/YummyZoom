using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Web.ApiContractTests.Infrastructure;
using YummyZoom.Application.Reviews.Commands.CreateReview;
using YummyZoom.Application.Reviews.Commands.DeleteReview;
using YummyZoom.Application.Reviews.Queries.GetRestaurantReviews;
using YummyZoom.Application.Reviews.Queries.GetRestaurantReviewSummary;
using YummyZoom.Application.Reviews.Queries.Common;
using YummyZoom.Application.Common.Models;
using Result = YummyZoom.SharedKernel.Result;
using System.Text.Json;

namespace YummyZoom.Web.ApiContractTests.Restaurants;

public class RestaurantReviewsContractTests
{
    [Test]
    public async Task CreateReview_WithAuth_MapsRequest_AndReturns201()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", Guid.NewGuid().ToString());

        var expectedReviewId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<CreateReviewCommand>();
            var cmd = (CreateReviewCommand)req;
            cmd.RestaurantId.Should().NotBeEmpty();
            cmd.Rating.Should().Be(5);
            return Result.Success(new CreateReviewResponse(expectedReviewId));
        });

        var restaurantId = Guid.NewGuid();
        var body = new YummyZoom.Web.Endpoints.Restaurants.CreateReviewRequest(Guid.NewGuid(), 5, "Great", "Loved it");

        var path = $"/api/v1/restaurants/{restaurantId}/reviews";
        TestContext.WriteLine($"REQUEST POST {path}");
        var resp = await client.PostAsJsonAsync(path, body);

        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine(json);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("reviewId").GetGuid().Should().Be(expectedReviewId);
    }

    [Test]
    public async Task CreateReview_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        var path = $"/api/v1/restaurants/{Guid.NewGuid()}/reviews";
        var body = new YummyZoom.Web.Endpoints.Restaurants.CreateReviewRequest(Guid.NewGuid(), 5, null, null);
        var resp = await client.PostAsJsonAsync(path, body);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetRestaurantReviews_Returns200_AndMapsQuery()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        var items = new List<ReviewDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), 5, null, "good", DateTime.UtcNow),
            new(Guid.NewGuid(), Guid.NewGuid(), 4, null, "ok", DateTime.UtcNow.AddMinutes(-1))
        };
        var page = new PaginatedList<ReviewDto>(items, 2, 1, 10);

        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<GetRestaurantReviewsQuery>();
            return Result.Success(page);
        });

        var restaurantId = Guid.NewGuid();
        var path = $"/api/v1/restaurants/{restaurantId}/reviews?pageNumber=1&pageSize=10";
        var resp = await client.GetAsync(path);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine(raw);
        using var doc = JsonDocument.Parse(raw);
        doc.RootElement.GetProperty("items").GetArrayLength().Should().Be(2);
    }

    [Test]
    public async Task GetRestaurantReviewSummary_Returns200_AndMapsQuery()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        var dto = new RestaurantReviewSummaryDto(4.2, 12, 1, 2, 3, 4, 2, 8, DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow);
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<GetRestaurantReviewSummaryQuery>();
            return Result.Success(dto);
        });

        var restaurantId = Guid.NewGuid();
        var resp = await client.GetAsync($"/api/v1/restaurants/{restaurantId}/reviews/summary");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine(raw);
        using var doc = JsonDocument.Parse(raw);
        doc.RootElement.GetProperty("averageRating").GetDouble().Should().BeGreaterThan(0);
        doc.RootElement.GetProperty("totalReviews").GetInt32().Should().Be(12);
    }

    [Test]
    public async Task DeleteReview_WithAuth_Returns200()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", Guid.NewGuid().ToString());

        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<DeleteReviewCommand>();
            return Result.Success();
        });

        var resp = await client.DeleteAsync($"/api/v1/restaurants/{Guid.NewGuid()}/reviews/{Guid.NewGuid()}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task DeleteReview_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var resp = await client.DeleteAsync($"/api/v1/restaurants/{Guid.NewGuid()}/reviews/{Guid.NewGuid()}");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
