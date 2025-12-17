using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Application.Tags.Queries.GetDietaryTags;
using YummyZoom.Web.ApiContractTests.Infrastructure;
using Result = YummyZoom.SharedKernel.Result;

namespace YummyZoom.Web.ApiContractTests.Tags;

public class DietaryTagsContractTests
{
    [Test]
    public async Task GetDietaryTags_ShouldReturnTags()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<GetDietaryTagsQuery>();
            
            var tags = new List<DietaryTagDto>
            {
                new(Guid.NewGuid(), "Vegan"),
                new(Guid.NewGuid(), "Vegetarian")
            };
            
            return Result.Success<IReadOnlyList<DietaryTagDto>>(tags);
        });

        var resp = await client.GetAsync("/api/v1/dietary-tags");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await resp.Content.ReadFromJsonAsync<List<DietaryTagDto>>();
        result.Should().HaveCount(2);
        result![0].Name.Should().Be("Vegan");
    }
}
