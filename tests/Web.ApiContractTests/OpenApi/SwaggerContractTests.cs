using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Web.ApiContractTests.Infrastructure;

namespace YummyZoom.Web.ApiContractTests.OpenApi;

public class SwaggerContractTests
{
    [Test]
    public async Task Swagger_IncludesJwtScheme()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/specification.json");
        resp.IsSuccessStatusCode.Should().BeTrue();
        var json = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine("REQUEST GET /api/v1/specification.json");
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        json.Should().Contain("JWT");
    }
}
