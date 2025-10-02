using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace YummyZoom.Web.ApiContractTests.Infrastructure;

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public new const string Scheme = "Test";

#pragma warning disable CS0618 // ISystemClock obsolete
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock) : base(options, logger, encoder, clock) { }
#pragma warning restore CS0618

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Request.Headers["x-test-user-id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userId))
        {
            // Return failure to trigger 401 Unauthorized when no auth header is present
            // This allows testing of authorization properly
            return Task.FromResult(AuthenticateResult.Fail("Missing x-test-user-id header"));
        }

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };

        var permissionsRaw = Request.Headers["x-test-permissions"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(permissionsRaw))
        {
            foreach (var permission in permissionsRaw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                claims.Add(new Claim("permission", permission));
            }
        }

        var rolesRaw = Request.Headers["x-test-roles"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(rolesRaw))
        {
            foreach (var role in rolesRaw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        var identity = new ClaimsIdentity(claims, Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
