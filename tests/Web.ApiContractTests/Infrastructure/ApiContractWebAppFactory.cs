using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Web.ApiContractTests.Infrastructure;
using Microsoft.Extensions.Hosting; // IHostedService

namespace YummyZoom.Web.ApiContractTests.Infrastructure;

public class ApiContractWebAppFactory : WebApplicationFactory<Program>
{
    public CapturingSender Sender { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.UseSetting("ConnectionStrings:YummyZoomDb", "Host=localhost;Database=dummy;Username=dummy;Password=dummy");

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
