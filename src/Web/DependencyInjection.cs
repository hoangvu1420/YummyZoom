using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Mvc;
using NSwag;
using NSwag.Generation.Processors.Security;
using YummyZoom.Application.Common.Configuration;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Search.Queries.UniversalSearch;
using YummyZoom.Infrastructure.Serialization;
using YummyZoom.Infrastructure.Serialization.Converters;
using YummyZoom.Infrastructure.StateStores.TeamCartStore;
using YummyZoom.Web.Configuration;
using YummyZoom.Web.Realtime;
using YummyZoom.Web.Services;
using YummyZoom.Web.Security;
using YummyZoom.Web.Services.OrderFlowSimulator;

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

        // Add CORS policy
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

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

        // Bind feature flags and TeamCart store options
        builder.Services.Configure<FeatureFlagsOptions>(
            builder.Configuration.GetSection(FeatureFlagsOptions.SectionName));
        builder.Services.Configure<TeamCartStoreOptions>(
            builder.Configuration.GetSection(TeamCartStoreOptions.SectionName));

        // Expose availability snapshot for endpoints and guards
        builder.Services.AddSingleton<ITeamCartFeatureAvailability, TeamCartFeatureAvailability>();

        // Log TeamCart feature readiness status on startup
        builder.Services.AddOptions<FeatureFlagsOptions>()
            .PostConfigure(options =>
            {
                using var loggerFactory = LoggerFactory.Create(config => config.AddConsole());
                var logger = loggerFactory.CreateLogger("TeamCart.Feature");

                var redis = builder.Configuration.GetConnectionString("redis")
                            ?? builder.Configuration["Cache:Redis:ConnectionString"];

                if (options.TeamCart)
                {
                    if (string.IsNullOrWhiteSpace(redis))
                    {
                        logger.LogWarning("Features:TeamCart is enabled but Redis is not configured. TeamCart endpoints must remain disabled.");
                    }
                    else
                    {
                        logger.LogInformation("Features:TeamCart enabled and Redis configured. Real-time TeamCart is ready to wire.");
                    }
                }
                else
                {
                    logger.LogInformation("Features:TeamCart is disabled.");
                }
            });

        // Dev/Test simulation services (registered always; gated at endpoint)
        builder.Services.AddSingleton<IDevImpersonationService, DevImpersonationService>();
        builder.Services.AddSingleton<IOrderFlowSimulator, OrderFlowSimulator>();

        // Ensure domain AggregateRootId<> value objects serialize as their underlying primitive (e.g. Guid) in API responses
        // so contract tests and clients see stable primitive values instead of { "value": "..." } objects.
        builder.Services.ConfigureHttpJsonOptions(o =>
        {
            // Avoid duplicate converter registrations if called again (idempotent)
            var alreadyId = o.SerializerOptions.Converters.OfType<AggregateRootIdJsonConverterFactory>().Any();
            if (!alreadyId)
            {
                o.SerializerOptions.Converters.Add(new AggregateRootIdJsonConverterFactory());
            }

            var alreadyEnum = o.SerializerOptions.Converters.OfType<JsonStringEnumConverter>().Any();
            if (!alreadyEnum)
            {
                o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            }
        });

        // Real-time (SignalR) services for hubs
        builder.Services.AddSignalR();

        // Real-time notifiers: override Infrastructure's NoOps with SignalR-backed adapters in Web host
        builder.Services.AddSingleton<IOrderRealtimeNotifier, SignalROrderRealtimeNotifier>();

        // TeamCart notifier behind feature flag (falls back to NoOp from Infrastructure if disabled)
        var teamCartEnabled = builder.Configuration.GetSection(FeatureFlagsOptions.SectionName).GetValue<bool>("TeamCart");
        if (teamCartEnabled)
        {
            builder.Services.AddSingleton<ITeamCartRealtimeNotifier, SignalRTeamCartRealtimeNotifier>();
        }

        // Result explanations & badges options (configurable thresholds)
        builder.Services.Configure<ResultExplanationOptions>(
            builder.Configuration.GetSection("Search:ResultExplanation"));

        // Rate limiting options
        builder.Services.Configure<RateLimitingOptions>(
            builder.Configuration.GetSection(RateLimitingOptions.SectionName));

        // Rate limiting for OTP endpoints
        builder.Services.AddRateLimiter(options =>
        {
            var rateLimitingConfig = builder.Configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>() ?? new RateLimitingOptions();
            
            // OTP Request rate limiting per IP
            options.AddPolicy("otp-request-ip", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitingConfig.OtpRequest.PerIp.PerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // OTP Verify rate limiting per IP
            options.AddPolicy("otp-verify-ip", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitingConfig.OtpVerify.PerIp.Per5Min,
                        Window = TimeSpan.FromMinutes(5),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString();
                }
                
                await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", token);
            };
        });

        // Image proxy options
        builder.Services.Configure<ImageProxyOptions>(
            builder.Configuration.GetSection(ImageProxyOptions.SectionName));

        // Add rate limiting policy for image proxy (per-IP)
        builder.Services.AddRateLimiter(options =>
        {
            var proxyConfig = builder.Configuration
                .GetSection(ImageProxyOptions.SectionName)
                .Get<ImageProxyOptions>() ?? new ImageProxyOptions();

            options.AddPolicy("image-proxy-ip", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 60, // default 60/min; adjust via config later if needed
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));
        });
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
