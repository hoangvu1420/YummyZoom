using System.Net;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Reviews.Queries.Common;
using YummyZoom.Application.Reviews.Queries.GetMyReviews;
using YummyZoom.Web.ApiContractTests.Infrastructure;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Web.ApiContractTests.Users;

public class MyReviewsContractTests
{
    [Test]
    public async Task GetMyReviews_WithAuth_Returns200_AndMapsQuery()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", Guid.NewGuid().ToString());

        var items = new List<ReviewDto> { new(Guid.NewGuid(), Guid.NewGuid(), 5, null, "ok", DateTime.UtcNow) };
        var page = new PaginatedList<ReviewDto>(items, 1, 1, 10);

        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<GetMyReviewsQuery>();
            return Result.Success(page);
        });

        var resp = await client.GetAsync("/api/v1/users/me/reviews?pageNumber=1&pageSize=10");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine(raw);
        using var doc = JsonDocument.Parse(raw);
        doc.RootElement.GetProperty("items").GetArrayLength().Should().Be(1);
    }

    [Test]
    public async Task GetMyReviews_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/users/me/reviews?pageNumber=1&pageSize=10");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
