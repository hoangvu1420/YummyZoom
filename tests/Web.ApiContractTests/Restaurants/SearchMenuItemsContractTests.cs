using System.Net;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.Restaurants.Queries.Management.SearchMenuItems;
using YummyZoom.Web.ApiContractTests.Infrastructure;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Web.ApiContractTests.Restaurants;

public class SearchMenuItemsContractTests
{
    [Test]
    public async Task SearchMenuItems_WithSearchParam_ShouldMapToQueryQ()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var restaurantId = Guid.NewGuid();

        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<SearchMenuItemsQuery>();
            var query = (SearchMenuItemsQuery)req;
            // This assertion is expected to fail if the bug exists
            query.Q.Should().Be("Banh"); 
            
            return Result.Success(new PaginatedList<MenuItemSearchResultDto>(new List<MenuItemSearchResultDto>(), 0, 1, 20));
        });

        var path = $"/api/v1/restaurants/{restaurantId}/menu-items/search?pageNumber=1&pageSize=20&q=Banh";
        var resp = await client.GetAsync(path);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
