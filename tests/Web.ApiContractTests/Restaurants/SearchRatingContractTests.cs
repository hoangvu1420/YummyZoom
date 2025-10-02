using System.Net;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Application.Restaurants.Queries.SearchRestaurants;
using YummyZoom.Web.ApiContractTests.Infrastructure;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Web.ApiContractTests.Restaurants;

public class SearchRatingContractTests
{
    [Test]
    public async Task Search_WithMinRating_MapsParam_AndReturns200()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        var empty = new PaginatedList<RestaurantSearchResultDto>(new List<RestaurantSearchResultDto>(), 0, 1, 10);
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<SearchRestaurantsQuery>();
            var q = (SearchRestaurantsQuery)req;
            q.MinRating.Should().Be(4.2);
            q.PageNumber.Should().Be(1);
            q.PageSize.Should().Be(10);
            return Result.Success(empty);
        });

        var resp = await client.GetAsync("/api/v1/restaurants/search?pageNumber=1&pageSize=10&minRating=4.2");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine(raw);
        using var doc = JsonDocument.Parse(raw);
        doc.RootElement.GetProperty("totalCount").GetInt32().Should().Be(0);
    }
}
