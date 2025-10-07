using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using YummyZoom.Application.Auth.Commands.CompleteSignup;
using YummyZoom.Application.Auth.Commands.RequestPhoneOtp;
using YummyZoom.Application.Auth.Commands.VerifyPhoneOtp;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.UserAggregate;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.Web.ApiContractTests.Infrastructure;

namespace YummyZoom.Web.ApiContractTests.Users;

public class UsersAuthContractTests
{
    [Test]
    public async Task OtpRequest_WhenValid_NotDevelopment_Returns202_AndMapsCommand()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        // Respond with a code, though non-dev path returns 202 regardless
        factory.Sender.RespondWith(_ => Result.Success(new RequestPhoneOtpResponse("654321")));

        var path = "/api/v1/users/auth/otp/request";
        var body = new { phoneNumber = "+15550123456" };
        TestContext.WriteLine($"REQUEST POST {path}\n{System.Text.Json.JsonSerializer.Serialize(body)}");
        var resp = await client.PostAsJsonAsync(path, body);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");

        resp.StatusCode.Should().Be(HttpStatusCode.Accepted);

        // Mapping assertion
        factory.Sender.LastRequest.Should().BeOfType<RequestPhoneOtpCommand>();
        var last = (RequestPhoneOtpCommand)factory.Sender.LastRequest!;
        last.PhoneNumber.Should().Be(body.phoneNumber);
    }

    [Test]
    public async Task OtpRequest_WhenValid_ReturnsEnvironmentHeader()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        factory.Sender.RespondWith(_ => Result.Success(new RequestPhoneOtpResponse("654321")));

        var path = "/api/v1/users/auth/otp/request";
        var body = new { phoneNumber = "+15550123456" };
        var resp = await client.PostAsJsonAsync(path, body);

        resp.StatusCode.Should().Be(HttpStatusCode.Accepted);
        resp.Headers.Should().ContainKey("X-YummyZoom-Environment");
        // Test environment should return "Test" as the environment name
        resp.Headers.GetValues("X-YummyZoom-Environment").First().Should().Be("Test");
    }


    // [Test]
    // public async Task OtpVerify_WhenValid_Returns200_AndMapsCommand()
    // {
    //     // NOTE: Contract-test scope verifies routing + mapping only.
    //     // Do NOT assert token JSON here; the Identity Bearer handler processes SignIn in higher-fidelity tests.
    //     var factory = new ApiContractWebAppFactory();
    //     var client = factory.CreateClient();

    //     var identityId = Guid.NewGuid();
    //     factory.Sender.RespondWith(_ => Result.Success(new VerifyPhoneOtpResponse(identityId, true, true)));

    //     var path = "/api/v1/users/auth/otp/verify";
    //     var body = new { phoneNumber = "+15550123456", code = "111111" };
    //     TestContext.WriteLine($"REQUEST POST {path}\n{System.Text.Json.JsonSerializer.Serialize(body)}");
    //     var resp = await client.PostAsJsonAsync(path, body);
    //     var raw = await resp.Content.ReadAsStringAsync();
    //     TestContext.WriteLine($"RESPONSE " + (int)resp.StatusCode + " " + resp.StatusCode + "\n" + raw);

    //     // Expect success status only; token emission is covered by integration tests.
    //     resp.StatusCode.Should().Be(HttpStatusCode.OK);

    //     // Mapping assertion
    //     factory.Sender.LastRequest.Should().BeOfType<VerifyPhoneOtpCommand>();
    //     var last = (VerifyPhoneOtpCommand)factory.Sender.LastRequest!;
    //     last.PhoneNumber.Should().Be(body.phoneNumber);
    //     last.Code.Should().Be(body.code);
    // }

    [Test]
    public async Task AuthStatus_WithoutAuth_Returns401()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();

        var path = "/api/v1/users/auth/status";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task AuthStatus_WithAuth_NewUser_ReturnsFlagsTrue()
    {
        var userId = Guid.NewGuid();
        var factory = new ApiContractWebAppFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IUserAggregateRepository>();
                services.AddSingleton<IUserAggregateRepository>(new FakeUserRepo(null));
            });
        });
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", userId.ToString());

        var path = "/api/v1/users/auth/status";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = System.Text.Json.JsonDocument.Parse(raw);
        doc.RootElement.GetProperty("isNewUser").GetBoolean().Should().BeTrue();
        doc.RootElement.GetProperty("requiresOnboarding").GetBoolean().Should().BeTrue();
    }

    [Test]
    public async Task AuthStatus_WithAuth_ExistingUser_ReturnsFlagsFalse()
    {
        var userId = Guid.NewGuid();
        var domainUserId = UserId.Create(userId);
        var existing = User.Create(domainUserId, name: "X", email: "x@y.com", phoneNumber: "+15550100000", isActive: true).Value;

        var factory = new ApiContractWebAppFactory().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IUserAggregateRepository>();
                services.AddSingleton<IUserAggregateRepository>(new FakeUserRepo(existing));
            });
        });
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", userId.ToString());

        var path = "/api/v1/users/auth/status";
        TestContext.WriteLine($"REQUEST GET {path}");
        var resp = await client.GetAsync(path);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = System.Text.Json.JsonDocument.Parse(raw);
        doc.RootElement.GetProperty("isNewUser").GetBoolean().Should().BeFalse();
        doc.RootElement.GetProperty("requiresOnboarding").GetBoolean().Should().BeFalse();
    }

    [Test]
    public async Task CompleteSignup_WithAuth_Returns200_AndMapsCommand()
    {
        var factory = new ApiContractWebAppFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-test-user-id", Guid.NewGuid().ToString());

        factory.Sender.RespondWith(_ => Result.Success());

        var path = "/api/v1/users/auth/complete-signup";
        var body = new { name = "New User", email = "new@example.com" };
        TestContext.WriteLine($"REQUEST POST {path}\n{System.Text.Json.JsonSerializer.Serialize(body)}");
        var resp = await client.PostAsJsonAsync(path, body);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        factory.Sender.LastRequest.Should().BeOfType<CompleteSignupCommand>();
        var last = (CompleteSignupCommand)factory.Sender.LastRequest!;
        last.Name.Should().Be(body.name);
        last.Email.Should().Be(body.email);
    }

    [Test]
    public async Task OtpRequest_WhenThrottled_Returns429_WithRetryAfterHeader()
    {
        var factory = new ApiContractWebAppFactory();
        factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Mock the throttle store to simulate throttling
                services.RemoveAll<IOtpThrottleStore>();
                services.AddSingleton<IOtpThrottleStore>(new FakeThrottleStore(isThrottled: true, retryAfterSeconds: 60));
            });
        });
        var client = factory.CreateClient();

        // Return throttled error from handler
        factory.Sender.RespondWith(_ => Result.Failure<RequestPhoneOtpResponse>(
            Error.Problem("Otp.Throttled", "Too many requests. Please try again in 60 seconds.")));

        var path = "/api/v1/users/auth/otp/request";
        var body = new { phoneNumber = "+15550123456" };
        TestContext.WriteLine($"REQUEST POST {path}\n{System.Text.Json.JsonSerializer.Serialize(body)}");
        var resp = await client.PostAsJsonAsync(path, body);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");

        resp.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        resp.Headers.RetryAfter?.Delta.Should().Be(TimeSpan.FromSeconds(60));

        // Verify the response contains problem details
        using var doc = System.Text.Json.JsonDocument.Parse(raw);
        doc.RootElement.GetProperty("status").GetInt32().Should().Be(429);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Otp.Throttled");
        doc.RootElement.GetProperty("detail").GetString().Should().Contain("60 seconds");
    }

    [Test]
    public async Task OtpVerify_WhenLockedOut_Returns423_WithRetryAfterHeader()
    {
        var factory = new ApiContractWebAppFactory();
        factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Mock the throttle store to simulate lockout
                services.RemoveAll<IOtpThrottleStore>();
                services.AddSingleton<IOtpThrottleStore>(new FakeThrottleStore(isLockedOut: true, lockoutRemainingSeconds: 300));
            });
        });
        var client = factory.CreateClient();

        // Return lockout error from handler
        factory.Sender.RespondWith(_ => Result.Failure<VerifyPhoneOtpResponse>(
            Error.Problem("Otp.LockedOut", "Account temporarily locked. Please try again in 300 seconds.")));

        var path = "/api/v1/users/auth/otp/verify";
        var body = new { phoneNumber = "+15550123456", code = "111111" };
        TestContext.WriteLine($"REQUEST POST {path}\n{System.Text.Json.JsonSerializer.Serialize(body)}");
        var resp = await client.PostAsJsonAsync(path, body);
        var raw = await resp.Content.ReadAsStringAsync();
        TestContext.WriteLine($"RESPONSE {(int)resp.StatusCode} {resp.StatusCode}\n{raw}");

        resp.StatusCode.Should().Be(HttpStatusCode.Locked);
        resp.Headers.RetryAfter?.Delta.Should().Be(TimeSpan.FromSeconds(300));

        // Verify the response contains problem details
        using var doc = System.Text.Json.JsonDocument.Parse(raw);
        doc.RootElement.GetProperty("status").GetInt32().Should().Be(423);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Otp.LockedOut");
        doc.RootElement.GetProperty("detail").GetString().Should().Contain("300 seconds");
    }

    private sealed class FakeUserRepo : IUserAggregateRepository
    {
        private readonly User? _user;
        public FakeUserRepo(User? user) => _user = user;

        public Task AddAsync(User user, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken = default) => Task.FromResult(_user);
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult(_user);
        public Task UpdateAsync(User user, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task SoftDeleteAsync(User user, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task RestoreAsync(User user, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<User?> GetByIdIncludingDeletedAsync(UserId userId, CancellationToken cancellationToken = default) => Task.FromResult(_user);
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult(_user);
        public Task DeleteAsync(User user, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class FakeThrottleStore : IOtpThrottleStore
    {
        private readonly bool _isThrottled;
        private readonly int _retryAfterSeconds;
        private readonly bool _isLockedOut;
        private readonly int _lockoutRemainingSeconds;

        public FakeThrottleStore(bool isThrottled = false, int retryAfterSeconds = 0, bool isLockedOut = false, int lockoutRemainingSeconds = 0)
        {
            _isThrottled = isThrottled;
            _retryAfterSeconds = retryAfterSeconds;
            _isLockedOut = isLockedOut;
            _lockoutRemainingSeconds = lockoutRemainingSeconds;
        }

        public Task<int> IncrementRequestCountAsync(string phoneNumber, int windowMinutes, CancellationToken cancellationToken = default)
            => Task.FromResult(_isThrottled ? 999 : 1);

        public Task<int> GetRequestCountAsync(string phoneNumber, int windowMinutes, CancellationToken cancellationToken = default)
            => Task.FromResult(_isThrottled ? 999 : 0);

        public Task<int> GetRetryAfterSecondsAsync(string phoneNumber, int windowMinutes, CancellationToken cancellationToken = default)
            => Task.FromResult(_retryAfterSeconds);

        public Task ResetRequestCountAsync(string phoneNumber, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<int> RecordFailedVerifyAsync(string phoneNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(1);

        public Task<int> GetFailedVerifyCountAsync(string phoneNumber, int windowMinutes, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task SetLockoutAsync(string phoneNumber, int lockoutMinutes, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<int> GetLockoutRemainingSecondsAsync(string phoneNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(_lockoutRemainingSeconds);

        public Task<bool> IsLockedOutAsync(string phoneNumber, CancellationToken cancellationToken = default)
            => Task.FromResult(_isLockedOut);

        public Task ResetFailedVerifyAndLockoutAsync(string phoneNumber, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
