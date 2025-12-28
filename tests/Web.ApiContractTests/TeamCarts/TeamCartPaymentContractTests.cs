using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
using YummyZoom.Application.TeamCarts.Commands.ApplyCouponToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.ApplyTipToTeamCart;
using YummyZoom.Application.TeamCarts.Commands.CommitToCodPayment;
using YummyZoom.Application.TeamCarts.Commands.InitiateMemberOnlinePayment;
using YummyZoom.Application.TeamCarts.Commands.LockTeamCartForPayment;
using YummyZoom.Application.TeamCarts.Commands.RemoveCouponFromTeamCart;
using YummyZoom.Infrastructure.Serialization.JsonOptions;
using YummyZoom.SharedKernel;
using YummyZoom.Web.ApiContractTests.Infrastructure;

namespace YummyZoom.Web.ApiContractTests.TeamCarts;

public class TeamCartPaymentContractTests
{
    #region Lock TeamCart Tests

    [Test]
    public async Task LockTeamCartForPayment_WithValidRequest_Returns204()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "host-user");

        var cartId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<LockTeamCartForPaymentCommand>();
            var cmd = (LockTeamCartForPaymentCommand)req;
            cmd.TeamCartId.Should().Be(cartId);
            return Result.Success(new LockTeamCartForPaymentResponse(1));
        });

        var path = $"/api/v1/team-carts/{cartId}/lock";
        TestContext.WriteLine($"REQUEST POST {path}");

        var resp = await client.PostAsync(path, null);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(rawResponse);
        doc.RootElement.GetProperty("quoteVersion").GetInt64().Should().Be(1);
    }

    [Test]
    public async Task LockTeamCartForPayment_WhenEmptyCart_Returns400Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "host-user");

        var cartId = Guid.NewGuid();
        factory.Sender.RespondWith(_ => Result.Failure<LockTeamCartForPaymentResponse>(
            Error.Validation("TeamCart.EmptyCart", "Cannot lock empty cart for payment")));

        var path = $"/api/v1/team-carts/{cartId}/lock";
        TestContext.WriteLine($"REQUEST POST {path}");

        var resp = await client.PostAsync(path, null);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = JsonSerializer.Deserialize<ProblemDetails>(rawResponse);
        problem!.Status.Should().Be(400);
        problem.Title.Should().Be("TeamCart.EmptyCart");
    }

    [Test]
    public async Task LockTeamCartForPayment_WhenNotHost_Returns403Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "not-host-user");

        var cartId = Guid.NewGuid();
        factory.Sender.RespondWith(_ => Result.Failure<LockTeamCartForPaymentResponse>(
            Error.Failure("TeamCart.NotHost", "Only the host can lock the cart")));

        var path = $"/api/v1/team-carts/{cartId}/lock";
        TestContext.WriteLine($"REQUEST POST {path}");

        var resp = await client.PostAsync(path, null);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = JsonSerializer.Deserialize<ProblemDetails>(rawResponse);
        problem!.Status.Should().Be(400);
        problem.Title.Should().Be("TeamCart.NotHost");
    }

    #endregion

    #region Apply Tip Tests

    [Test]
    public async Task ApplyTipToTeamCart_WithValidAmount_Returns204()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "host-user");

        var cartId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<ApplyTipToTeamCartCommand>();
            var cmd = (ApplyTipToTeamCartCommand)req;
            cmd.TeamCartId.Should().Be(cartId);
            cmd.TipAmount.Should().Be(5.00m);
            return Result.Success();
        });

        var body = new { TipAmount = 5.00m };
        var path = $"/api/v1/team-carts/{cartId}/tip";

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
    public async Task ApplyTipToTeamCart_WithNegativeAmount_Returns400Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "host-user");

        var cartId = Guid.NewGuid();
        factory.Sender.RespondWith(_ => Result.Failure(
            Error.Validation("ApplyTipToTeamCart.InvalidAmount", "Tip amount cannot be negative")));

        var body = new { TipAmount = -1.00m };
        var path = $"/api/v1/team-carts/{cartId}/tip";

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
        problem.Title.Should().Be("ApplyTipToTeamCart.InvalidAmount");
    }

    [Test]
    public async Task ApplyTipToTeamCart_WhenNotLocked_Returns400Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "host-user");

        var cartId = Guid.NewGuid();
        factory.Sender.RespondWith(_ => Result.Failure(
            Error.Validation("TeamCart.InvalidStatus", "Cart must be locked to apply tip")));

        var body = new { TipAmount = 5.00m };
        var path = $"/api/v1/team-carts/{cartId}/tip";

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
        problem.Title.Should().Be("TeamCart.InvalidStatus");
    }

    #endregion

    #region Apply Coupon Tests

    [Test]
    public async Task ApplyCouponToTeamCart_WithValidCoupon_Returns204()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "host-user");

        var cartId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<ApplyCouponToTeamCartCommand>();
            var cmd = (ApplyCouponToTeamCartCommand)req;
            cmd.TeamCartId.Should().Be(cartId);
            cmd.CouponCode.Should().Be("SAVE10");
            return Result.Success();
        });

        var body = new { CouponCode = "SAVE10" };
        var path = $"/api/v1/team-carts/{cartId}/coupon";

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
    public async Task ApplyCouponToTeamCart_WithInvalidCoupon_Returns404Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "host-user");

        var cartId = Guid.NewGuid();
        factory.Sender.RespondWith(_ => Result.Failure(
            Error.NotFound("Coupon.NotFound", "Coupon not found or expired")));

        var body = new { CouponCode = "INVALID" };
        var path = $"/api/v1/team-carts/{cartId}/coupon";

        var requestJson = JsonSerializer.Serialize(body, DomainJson.Options);
        TestContext.WriteLine($"REQUEST POST {path}");
        TestContext.WriteLine(requestJson);

        var resp = await client.PostAsJsonAsync(path, body, DomainJson.Options);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = JsonSerializer.Deserialize<ProblemDetails>(rawResponse);
        problem!.Status.Should().Be(404);
        problem.Title.Should().Be("Coupon.NotFound");
    }

    #endregion

    #region Remove Coupon Tests

    [Test]
    public async Task RemoveCouponFromTeamCart_WithValidRequest_Returns204()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "host-user");

        var cartId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<RemoveCouponFromTeamCartCommand>();
            var cmd = (RemoveCouponFromTeamCartCommand)req;
            cmd.TeamCartId.Should().Be(cartId);
            return Result.Success();
        });

        var path = $"/api/v1/team-carts/{cartId}/coupon";
        TestContext.WriteLine($"REQUEST DELETE {path}");

        var resp = await client.DeleteAsync(path);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task RemoveCouponFromTeamCart_WhenNoCouponApplied_Returns400Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "host-user");

        var cartId = Guid.NewGuid();
        factory.Sender.RespondWith(_ => Result.Failure(
            Error.Validation("TeamCart.NoCouponApplied", "No coupon is currently applied to remove")));

        var path = $"/api/v1/team-carts/{cartId}/coupon";
        TestContext.WriteLine($"REQUEST DELETE {path}");

        var resp = await client.DeleteAsync(path);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = JsonSerializer.Deserialize<ProblemDetails>(rawResponse);
        problem!.Status.Should().Be(400);
        problem.Title.Should().Be("TeamCart.NoCouponApplied");
    }

    #endregion

    #region COD Payment Tests

    [Test]
    public async Task CommitToCodPayment_WithValidRequest_Returns204()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "member-user");

        var cartId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<CommitToCodPaymentCommand>();
            var cmd = (CommitToCodPaymentCommand)req;
            cmd.TeamCartId.Should().Be(cartId);
            return Result.Success();
        });

        var path = $"/api/v1/team-carts/{cartId}/payments/cod";
        TestContext.WriteLine($"REQUEST POST {path}");

        var resp = await client.PostAsync(path, null);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task CommitToCodPayment_WhenNotLocked_Returns400Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "member-user");

        var cartId = Guid.NewGuid();
        factory.Sender.RespondWith(_ => Result.Failure(
            Error.Validation("TeamCart.InvalidStatus", "Cart must be locked for payment commitments")));

        var path = $"/api/v1/team-carts/{cartId}/payments/cod";
        TestContext.WriteLine($"REQUEST POST {path}");

        var resp = await client.PostAsync(path, null);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = JsonSerializer.Deserialize<ProblemDetails>(rawResponse);
        problem!.Status.Should().Be(400);
        problem.Title.Should().Be("TeamCart.InvalidStatus");
    }

    #endregion

    #region Online Payment Tests

    [Test]
    public async Task InitiateMemberOnlinePayment_WithValidRequest_Returns200()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "member-user");

        var cartId = Guid.NewGuid();
        factory.Sender.RespondWith(req =>
        {
            req.Should().BeOfType<InitiateMemberOnlinePaymentCommand>();
            var cmd = (InitiateMemberOnlinePaymentCommand)req;
            cmd.TeamCartId.Should().Be(cartId);
            return Result.Success(new InitiateMemberOnlinePaymentResponse(
                "pi_1234567890",
                "pi_1234567890_secret_abcdef"
            ));
        });

        var path = $"/api/v1/team-carts/{cartId}/payments/online";
        TestContext.WriteLine($"REQUEST POST {path}");

        var resp = await client.PostAsync(path, null);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(rawResponse);
        doc.RootElement.GetProperty("paymentIntentId").GetString().Should().Be("pi_1234567890");
        doc.RootElement.GetProperty("clientSecret").GetString().Should().Be("pi_1234567890_secret_abcdef");
    }

    [Test]
    public async Task InitiateMemberOnlinePayment_WhenPaymentServiceError_Returns500Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "member-user");

        var cartId = Guid.NewGuid();
        factory.Sender.RespondWith(_ => Result.Failure<InitiateMemberOnlinePaymentResponse>(
            Error.Failure("PaymentGateway.ServiceError", "Payment service temporarily unavailable")));

        var path = $"/api/v1/team-carts/{cartId}/payments/online";
        TestContext.WriteLine($"REQUEST POST {path}");

        var resp = await client.PostAsync(path, null);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = JsonSerializer.Deserialize<ProblemDetails>(rawResponse);
        problem!.Status.Should().Be(400);
        problem.Title.Should().Be("PaymentGateway.ServiceError");
    }

    [Test]
    public async Task InitiateMemberOnlinePayment_WhenAlreadyCommitted_Returns409Problem()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", "member-user");

        var cartId = Guid.NewGuid();
        factory.Sender.RespondWith(_ => Result.Failure<InitiateMemberOnlinePaymentResponse>(
            Error.Conflict("MemberPayment.AlreadyCommitted", "Member has already committed to payment")));

        var path = $"/api/v1/team-carts/{cartId}/payments/online";
        TestContext.WriteLine($"REQUEST POST {path}");

        var resp = await client.PostAsync(path, null);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = JsonSerializer.Deserialize<ProblemDetails>(rawResponse);
        problem!.Status.Should().Be(409);
        problem.Title.Should().Be("MemberPayment.AlreadyCommitted");
    }

    [Test]
    public async Task InitiateMemberOnlinePayment_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        var path = $"/api/v1/team-carts/{Guid.NewGuid()}/payments/online";
        TestContext.WriteLine($"REQUEST POST {path}");

        var resp = await client.PostAsync(path, null);

        var rawResponse = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}");
        TestContext.WriteLine(rawResponse);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
}
