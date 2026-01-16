using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting; // IHostedService
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Infrastructure;
using YummyZoom.Infrastructure.Persistence.EfCore.Seeding;
using YummyZoom.Web.ApiContractTests.Infrastructure;


namespace YummyZoom.Web.ApiContractTests.Infrastructure;

public class ApiContractWebAppFactory : WebApplicationFactory<Program>
{
    public CapturingSender Sender { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.UseSetting("ConnectionStrings:YummyZoomDb", "Host=localhost;Database=dummy;Username=dummy;Password=dummy");
        builder.UseSetting("ConnectionStrings:redis", "localhost:6379");

        // Add authorization services like in Infrastructure
        builder.ConfigureServices(services =>
        {
            services.AddAuthorizationBuilder()
                .AddPolicy("HasPermission", policy => policy.Requirements.Add(new HasPermissionRequirement("TeamCartMember")));
            services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        });

        builder.ConfigureTestServices(services =>
        {
            // Replace ISender
            var existing = services.FirstOrDefault(d => d.ServiceType == typeof(ISender));
            if (existing is not null) services.Remove(existing);
            services.AddSingleton<ISender>(Sender);

            // Disable Outbox publisher hosted service
            var hosted = services.FirstOrDefault(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType?.Name == "OutboxPublisherHostedService");
            if (hosted is not null) services.Remove(hosted);

            // Disable Menu read model maintenance hosted service
            var hosted2 = services.FirstOrDefault(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType?.Name == "FullMenuViewMaintenanceHostedService");
            if (hosted2 is not null) services.Remove(hosted2);

            // Disable Cache invalidation subscriber hosted service (depends on Redis)
            var hosted3 = services.FirstOrDefault(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType?.Name == "CacheInvalidationSubscriber");
            if (hosted3 is not null) services.Remove(hosted3);

            // Disable coupon read model maintenance hosted service (depends on Postgres)
            var hosted4 = services.FirstOrDefault(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType?.Name == "ActiveCouponViewMaintenanceHostedService");
            if (hosted4 is not null) services.Remove(hosted4);

            // Disable DB seeding for API contract tests (avoids connecting to Postgres during app startup).
            // The contract test suite replaces ISender with a stub and does not need database access.
            var seeders = services.Where(d => d.ServiceType == typeof(ISeeder)).ToList();
            foreach (var seeder in seeders)
            {
                services.Remove(seeder);
            }

            // Remove Redis-related services to avoid connection issues in tests
            var redisServices = services.Where(d =>
                d.ServiceType.Name.Contains("Redis") ||
                d.ServiceType.Name.Contains("ConnectionMultiplexer") ||
                d.ServiceType == typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache))
                .ToList();
            foreach (var redisService in redisServices)
            {
                services.Remove(redisService);
            }

            // Add a mock distributed cache for tests
            services.AddDistributedMemoryCache();

            // Enable TeamCart feature for contract tests
            services.Configure<YummyZoom.Web.Configuration.FeatureFlagsOptions>(options =>
            {
                options.TeamCart = true;
            });

            // Add the authorization handler for permission-based policies
            services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, YummyZoom.Application.Common.Authorization.PermissionAuthorizationHandler>();

            // Inject test auth
            services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = TestAuthHandler.Scheme;
                o.DefaultChallengeScheme = TestAuthHandler.Scheme;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Scheme, _ => { });

            // Ensure ProblemDetails is properly configured for tests
            services.AddProblemDetails();

            // Configure HTTPS redirection to avoid 400 with empty body in TestServer
            services.Configure<Microsoft.AspNetCore.HttpsPolicy.HttpsRedirectionOptions>(o =>
            {
                o.HttpsPort = 443;
            });
        });
        // Also set hosting setting used by HttpsRedirectionMiddleware
        builder.UseSetting("https_port", "443");
    }

    // Ensure requests use HTTPS to bypass HttpsRedirectionMiddleware issues in TestServer
    public new HttpClient CreateClient() => CreateClient(new WebApplicationFactoryClientOptions());

    public new HttpClient CreateClient(WebApplicationFactoryClientOptions options)
    {
        options ??= new WebApplicationFactoryClientOptions();
        options.BaseAddress ??= new Uri("https://localhost");
        return base.CreateClient(options);
    }
}
