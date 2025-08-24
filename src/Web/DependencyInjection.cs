using Azure.Identity;
using YummyZoom.Web.Services;
using Microsoft.AspNetCore.Mvc;

using NSwag;
using NSwag.Generation.Processors.Security;
using Asp.Versioning;
using Azure.Security.KeyVault.Secrets;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Infrastructure.Serialization;
using YummyZoom.Web.Realtime;

namespace YummyZoom.Web;

public static class DependencyInjection
{
    public static void AddWebServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        builder.Services.AddScoped<IUser, CurrentUser>();

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddExceptionHandler<CustomExceptionHandler>();

        // Customise default API behaviour
        builder.Services.Configure<ApiBehaviorOptions>(options =>
            options.SuppressModelStateInvalidFilter = true);

        builder.Services.AddEndpointsApiExplorer();

        // Add API Versioning
        builder.Services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        }).AddApiExplorer(options =>
        {
            // Format the version as "v{major}.{minor}"
            options.GroupNameFormat = "'v'VVV";

            // Substitute the version in the route template
            options.SubstituteApiVersionInUrl = true;
        });

        builder.Services.AddOpenApiDocument(configure =>
        {
            configure.Title = "YummyZoom API";

            // Add JWT
            configure.AddSecurity("JWT", Enumerable.Empty<string>(), new OpenApiSecurityScheme
            {
                Type = OpenApiSecuritySchemeType.ApiKey,
                Name = "Authorization",
                In = OpenApiSecurityApiKeyLocation.Header,
                Description = "Type into the textbox: Bearer {your JWT token}."
            });

            configure.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("JWT"));
        });

        // Ensure domain AggregateRootId<> value objects serialize as their underlying primitive (e.g. Guid) in API responses
        // so contract tests and clients see stable primitive values instead of { "value": "..." } objects.
        builder.Services.ConfigureHttpJsonOptions(o =>
        {
            // Avoid duplicate converter registrations if called again (idempotent)
            var already = o.SerializerOptions.Converters.OfType<AggregateRootIdJsonConverterFactory>().Any();
            if (!already)
            {
                o.SerializerOptions.Converters.Add(new AggregateRootIdJsonConverterFactory());
            }
        });

        // Real-time (SignalR) services for hubs
        builder.Services.AddSignalR();

        // Real-time notifier: override Infrastructure's NoOp with SignalR-backed adapter in Web host
        builder.Services.AddSingleton<IOrderRealtimeNotifier, SignalROrderRealtimeNotifier>();
    }

    public static void AddKeyVaultIfConfigured(this IHostApplicationBuilder builder)
    {
        var keyVaultUri = builder.Configuration["AZURE_KEY_VAULT_ENDPOINT"];
        if (string.IsNullOrWhiteSpace(keyVaultUri))
        {
            return;
        }

        try
        {
            // Try to connect to the Key Vault to ensure it's accessible
            var credential = new DefaultAzureCredential();
            var client = new SecretClient(new Uri(keyVaultUri), credential);
            // Attempt to list secrets as a connectivity check (minimal call)
            using IEnumerator<SecretProperties> enumerator = client.GetPropertiesOfSecrets().GetEnumerator();
            enumerator.MoveNext();

            builder.Configuration.AddAzureKeyVault(
                new Uri(keyVaultUri),
                credential);
        }
        catch (Exception)
        {
            // Log or handle the exception as needed
            using var loggerFactory = LoggerFactory.Create(config => config.AddConsole());
            var logger = loggerFactory.CreateLogger("AzureKeyVault.Initialization");
            logger.LogCritical($"Failed to connect to Azure Key Vault at '{keyVaultUri}'.");
            // throw new InvalidOperationException($"Failed to connect to Azure Key Vault at '{keyVaultUri}'.", ex);
        }
    }
}
