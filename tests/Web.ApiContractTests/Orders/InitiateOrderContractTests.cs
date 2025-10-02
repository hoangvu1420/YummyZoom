using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MediatR;
using NUnit.Framework;
using YummyZoom.Application.Orders.Commands.InitiateOrder;
using YummyZoom.Domain.Common.ValueObjects;
using YummyZoom.Domain.OrderAggregate.ValueObjects;
using YummyZoom.Infrastructure.Serialization;
using YummyZoom.Infrastructure.Serialization.JsonOptions;
using YummyZoom.SharedKernel;
using YummyZoom.Web.ApiContractTests.Infrastructure;
using YummyZoom.Web.Endpoints;

namespace YummyZoom.Web.ApiContractTests.Orders;

public class InitiateOrderContractTests
{
    [Test]
    public async Task InitiateOrder_MapsRequestToCommand_Returns200()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var expectedId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<InitiateOrderCommand>();
            return Result.Success(new InitiateOrderResponse(
                OrderId.Create(expectedId),
                "ORD-12345",
                new Money(25.00m, "USD"),
                null,
                null));
        });

        var body = new InitiateOrderRequest(
            Guid.NewGuid(), Guid.NewGuid(),
            new() { new InitiateOrderItemRequest(Guid.NewGuid(), 1) },
            new("Street", "City", "State", "Zip", "Country"),
            "card", null, null, 0m, null);

        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine("REQUEST POST /api/v1/orders/initiate");
        TestContext.WriteLine(requestJson);

        var resp = await client.PostAsJsonAsync("/api/v1/orders/initiate", body);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        // Use the same domain serialization options (includes AggregateRootIdJsonConverterFactory) for client-side deserialization
        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        var responseDto = JsonSerializer.Deserialize<InitiateOrderResponse>(rawResponse, DomainJson.Options);
        responseDto!.OrderId.Value.Should().Be(expectedId);
    }
}
