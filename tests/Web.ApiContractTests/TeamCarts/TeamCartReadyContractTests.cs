using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using YummyZoom.Application.TeamCarts.Commands.SetMemberReady;
using YummyZoom.Infrastructure.Serialization.JsonOptions;
using YummyZoom.SharedKernel;
using YummyZoom.Web.ApiContractTests.Infrastructure;
using YummyZoom.Web.Endpoints;

namespace YummyZoom.Web.ApiContractTests.TeamCarts;

public class TeamCartReadyContractTests
{
    [Test]
    public async Task SetMemberReady_WithValidRequest_Returns200OK()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var cartId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<SetMemberReadyCommand>();
            var cmd = (SetMemberReadyCommand)req;
            cmd.TeamCartId.Should().Be(cartId);
            cmd.IsReady.Should().BeTrue();
            return Result.Success();
        });

        var body = new SetMemberReadyRequest(true);
        var path = $"/api/v1/team-carts/{cartId}/ready";

        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(requestJson);

        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task SetMemberReady_WhenUserNotMember_Returns400Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "user-1");

        var cartId = Guid.NewGuid();
        factory.Sender.RespondWith(_ => Result.Failure(
            Error.Validation("TeamCart.UserNotMember", "User is not a member of this team cart")));

        var body = new SetMemberReadyRequest(true);
        var path = $"/api/v1/team-carts/{cartId}/ready";

        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(requestJson);

        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = JsonSerializer.Deserialize<ProblemDetails>(rawResponse);
        problem!.Status.Should().Be(400);
        problem.Title.Should().Be("TeamCart.UserNotMember");
    }
}
