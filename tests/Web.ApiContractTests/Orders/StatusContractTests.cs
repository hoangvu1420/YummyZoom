using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using YummyZoom.Application.Orders.Queries.GetOrderById;
using YummyZoom.SharedKernel;
using YummyZoom.Web.ApiContractTests.Infrastructure;

namespace YummyZoom.Web.ApiContractTests.Orders;

public class StatusContractTests
{
    [Test]
    public async Task GetOrderById_WhenNotFound_Returns404Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        // Return a generic Result<GetOrderByIdResponse> failure so the endpoint's expected Result<GetOrderByIdResponse>
        // matches the captured response type (avoids InvalidCastException in CapturingSender).
        factory.Sender.RespondWith(_ => Result.Failure<GetOrderByIdResponse>(Error.NotFound("Order.NotFound", "Missing order")));

        var path = $"/api/v1/orders/{Guid.NewGuid()}";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(raw);
        var problem = System.Text.Json.JsonSerializer.Deserialize<ProblemDetails>(raw);
        problem!.Status.Should().Be(404);
        problem.Title.Should().Be("Order.NotFound");
    }
}
