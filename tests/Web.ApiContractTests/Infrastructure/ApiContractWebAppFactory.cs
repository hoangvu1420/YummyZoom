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

            // Inject test auth
            services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = TestAuthHandler.Scheme;
                o.DefaultChallengeScheme = TestAuthHandler.Scheme;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Scheme, _ => { });
        });
    }
}
