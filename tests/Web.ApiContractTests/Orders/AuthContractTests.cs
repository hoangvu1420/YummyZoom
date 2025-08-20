using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NUnit.Framework;
using YummyZoom.Web.ApiContractTests.Infrastructure;

namespace YummyZoom.Web.ApiContractTests.Orders;

public class AuthContractTests
{
    [Test]
    public async Task InitiateOrder_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        var path = "/api/v1.0/orders/initiate";
        var reqBody = new { };
        var json = System.Text.Json.JsonSerializer.Serialize(reqBody);
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(json);
        var resp = await client.PostAsJsonAsync(path, reqBody);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(raw);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
