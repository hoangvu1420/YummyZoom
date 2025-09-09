using System.Text.Json;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Stripe;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Caching;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Restaurants.Queries.Common;
using YummyZoom.Domain.Services;
using YummyZoom.Infrastructure.BackgroundServices;
using YummyZoom.Infrastructure.Caching;
using YummyZoom.Infrastructure.Caching.Abstractions;
using YummyZoom.Infrastructure.Caching.Distributed;
using YummyZoom.Infrastructure.Caching.Memory;
using YummyZoom.Infrastructure.Caching.Serialization;
using YummyZoom.Infrastructure.Identity;
using YummyZoom.Infrastructure.Messaging.Invalidation;
using YummyZoom.Infrastructure.Messaging.Outbox;
using YummyZoom.Infrastructure.Notifications.Firebase;
using YummyZoom.Infrastructure.Payments.Stripe;
using YummyZoom.Infrastructure.Persistence;
using YummyZoom.Infrastructure.Persistence.EfCore;
using YummyZoom.Infrastructure.Persistence.EfCore.Interceptors;
using YummyZoom.Infrastructure.Persistence.ReadModels.FullMenu;
using YummyZoom.Infrastructure.Persistence.ReadModels.Reviews;
using YummyZoom.Infrastructure.Persistence.ReadModels.Search;
using YummyZoom.Infrastructure.Persistence.Repositories;
using YummyZoom.Infrastructure.Realtime;
using YummyZoom.Infrastructure.StateStores.TeamCartStore;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("YummyZoomDb");
        Guard.Against.Null(connectionString, message: "Connection string 'YummyZoomDb' not found.");

        builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, SoftDeleteInterceptor>();
        // builder.Services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>(); // replaced by outbox enqueue
        builder.Services.AddScoped<ISaveChangesInterceptor, ConvertDomainEventsToOutboxInterceptor>();

        builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseNpgsql(connectionString, npgsql => npgsql.UseNetTopologySuite());
        });

        builder.EnrichNpgsqlDbContext<ApplicationDbContext>();

        builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        builder.Services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<ApplicationDbContext>());

        builder.Services.AddScoped<ApplicationDbContextInitialiser>();

        builder.AddFirebaseIfConfigured();
        builder.AddCachingIfConfigured(); 

        builder.Services.AddAuthentication()
            .AddBearerToken(IdentityConstants.BearerScheme, options =>
            {
                options.RefreshTokenExpiration = TimeSpan.FromDays(7);
            });

        builder.Services.AddAuthorizationBuilder();

        builder.Services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
            })
            .AddDefaultTokenProviders()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddApiEndpoints();

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddTransient<IIdentityService, Identity.IdentityService>();

        builder.Services.AddScoped<IUserAggregateRepository, UserAggregateRepository>();
        builder.Services.AddScoped<IRoleAssignmentRepository, RoleAssignmentRepository>();
        builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
        builder.Services.AddScoped<IUserDeviceSessionRepository, UserDeviceSessionRepository>();
        builder.Services.AddScoped<IOrderRepository, OrderRepository>();
        builder.Services.AddScoped<IRestaurantRepository, RestaurantRepository>();
        builder.Services.AddScoped<IMenuRepository, MenuRepository>();
        builder.Services.AddScoped<IMenuCategoryRepository, MenuCategoryRepository>();
        builder.Services.AddScoped<IMenuItemRepository, MenuItemRepository>();
        builder.Services.AddScoped<ICouponRepository, CouponRepository>();
        builder.Services.AddScoped<ICustomizationGroupRepository, CustomizationGroupRepository>();
        builder.Services.AddScoped<IInboxStore, InboxStore>();
        builder.Services.AddScoped<ITeamCartRepository, TeamCartRepository>();
        builder.Services.AddSingleton<ITeamCartRealtimeNotifier, NoOpTeamCartRealtimeNotifier>();
        builder.Services.AddScoped<IRestaurantAccountRepository, RestaurantAccountRepository>();
        builder.Services.AddSingleton<IOrderRealtimeNotifier, NoOpOrderRealtimeNotifier>();

        // Register the connection factory for Dapper queries
        builder.Services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();

        builder.Services.AddSingleton<IFcmService, FcmService>();

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(Policies.CanPurge, policy => policy.RequireRole(Roles.Administrator))
            .AddPolicy(Policies.MustBeRestaurantOwner, policy =>
                policy.AddRequirements(new HasPermissionRequirement(Roles.RestaurantOwner)))
            .AddPolicy(Policies.MustBeRestaurantStaff, policy =>
                policy.AddRequirements(new HasPermissionRequirement(Roles.RestaurantStaff)))
            .AddPolicy(Policies.MustBeUserOwner, policy =>
                policy.AddRequirements(new HasPermissionRequirement(Roles.UserOwner)))
            // Order policies
            .AddPolicy(Policies.MustBeOrderOwner, policy =>
                policy.AddRequirements(new HasPermissionRequirement(Roles.OrderOwner)))
            .AddPolicy(Policies.MustBeOrderManager, policy =>
                policy.AddRequirements(new HasPermissionRequirement(Roles.OrderManager)))
            // TeamCart policies
            .AddPolicy(Policies.MustBeTeamCartHost, policy =>
                policy.AddRequirements(new HasPermissionRequirement(Roles.TeamCartHost)))
            .AddPolicy(Policies.MustBeTeamCartMember, policy =>
                policy.AddRequirements(new HasPermissionRequirement(Roles.TeamCartMember)))
            .AddPolicy(Policies.MustBeTeamCartParticipant, policy =>
                policy.AddRequirements(new HasPermissionRequirement(Roles.TeamCartMember)));

        builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, YummyZoomClaimsPrincipalFactory>();

        builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        // Stripe configuration
        builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection(StripeOptions.SectionName));

        var stripeOptions = builder.Configuration.GetSection(StripeOptions.SectionName).Get<StripeOptions>();
        if (stripeOptions is not null)
        {
            StripeConfiguration.ApiKey = stripeOptions.SecretKey;
        }

        builder.Services.AddScoped<IPaymentGatewayService, StripeService>();

        // Register Domain Services
        builder.Services.AddScoped<OrderFinancialService>();
        builder.Services.AddScoped<TeamCartConversionService>();

        // Outbox publisher options and hosted service (enabled here; can be toggled by env later)
        builder.Services.Configure<OutboxPublisherOptions>(opt =>
        {
            opt.BatchSize = 50;
            opt.PollInterval = TimeSpan.FromMilliseconds(250);
            opt.MaxBackoff = TimeSpan.FromMinutes(5);
            opt.MaxAttempts = 10;
        });

        builder.Services.AddSingleton<IOutboxProcessor, OutboxProcessor>();
        builder.Services.AddHostedService<OutboxPublisherHostedService>();

        // Read model rebuild services
        builder.Services.AddScoped<IFullMenuViewMaintainer, FullMenuViewMaintainer>();

        // FullMenu read model maintenance (backfill + reconciliation)
        builder.Services.Configure<FullMenuViewMaintenanceOptions>(
            builder.Configuration.GetSection("ReadModelMaintenance"));
        builder.Services.AddHostedService<FullMenuViewMaintenanceHostedService>();

        // Search read model maintainer
        builder.Services.AddScoped<ISearchReadModelMaintainer, SearchIndexMaintainer>();

        // SearchIndex read model maintenance
        builder.Services.Configure<SearchIndexMaintenanceOptions>(
            builder.Configuration.GetSection("SearchIndexMaintenance"));
        builder.Services.AddHostedService<SearchIndexMaintenanceHostedService>();

        // Review summaries maintainer
        builder.Services.AddScoped<IReviewSummaryMaintainer, ReviewSummaryMaintainer>();

        // ReviewSummary read model maintenance
        builder.Services.Configure<ReviewSummaryMaintenanceOptions>(
            builder.Configuration.GetSection("ReviewSummaryMaintenance"));
        builder.Services.AddHostedService<ReviewSummaryMaintenanceHostedService>();

        // TeamCart expiration options (Phase 3.8)
        builder.Services.Configure<TeamCartExpirationOptions>(
            builder.Configuration.GetSection(TeamCartExpirationOptions.SectionName));
        // Hosted service registration can be toggled at runtime by Enabled flag inside ExecuteAsync
        builder.Services.AddHostedService<TeamCartExpirationHostedService>();
    }

    public static void AddFirebaseIfConfigured(this IHostApplicationBuilder builder)
    {
        using var loggerFactory = LoggerFactory.Create(config => config.AddConsole());
        var logger = loggerFactory.CreateLogger("Firebase.Initialization");

        try
        {
            var firebaseConfig = new FirebaseAdminSdkConfig();
            builder.Configuration.GetSection(FirebaseAdminSdkConfig.SectionName).Bind(firebaseConfig);

            if (string.IsNullOrEmpty(firebaseConfig.PrivateKey))
            {
                logger.LogWarning("Firebase Admin SDK configuration is incomplete. FCM will not be initialized.");
                return;
            }

            // Convert the FirebaseAdminSdkConfig object back to a JSON string
            // The GoogleCredential.FromJson method expects a JSON string
            var jsonCredentials = JsonSerializer.Serialize(firebaseConfig, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            // Create the app instance from our explicit credentials.
            var app = FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromJson(jsonCredentials)
            });

            // Get the messaging client FROM OUR EXPLICIT APP INSTANCE.
            var firebaseMessaging = FirebaseMessaging.GetMessaging(app);

            // Register this specific instance as a singleton in the DI container.
            builder.Services.AddSingleton(firebaseMessaging);

            logger.LogInformation("Firebase Admin SDK initialized successfully from configuration and FirebaseMessaging registered as a singleton.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Failed to initialize Firebase Admin SDK. FCM will not be available.");
        }
    }

    public static void AddCachingIfConfigured(this IHostApplicationBuilder builder)
    {
        using var loggerFactory = LoggerFactory.Create(config => config.AddConsole());
        var logger = loggerFactory.CreateLogger("Caching.Initialization");

        // Caching: prefer Redis if configured via Aspire (ConnectionStrings:Redis) or explicit config, otherwise fall back to memory cache
        var redisConnection = builder.Configuration.GetConnectionString("redis")
            ?? builder.Configuration["Cache:Redis:ConnectionString"];

        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            // Shared multiplexer for advanced Redis operations (pub/sub, sets)
            builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
                StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnection));

            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
            });
            // Cache service bindings (distributed)
            builder.Services.AddSingleton<ICacheSerializer, JsonCacheSerializer>();
            builder.Services.AddSingleton<ICacheKeyFactory, DefaultCacheKeyFactory>();
            builder.Services.AddSingleton<ICacheService, DistributedCacheService>();
            builder.Services.AddSingleton<ICacheInvalidationPublisher, RedisInvalidationPublisher>();
            builder.Services.AddHostedService<CacheInvalidationSubscriber>();
            
            // TeamCart store (Redis-backed)
            builder.Services.Configure<TeamCartStoreOptions>(
                builder.Configuration.GetSection(TeamCartStoreOptions.SectionName));
            builder.Services.AddSingleton<ITeamCartStore, RedisTeamCartStore>();
            logger.LogInformation("Redis cache configured successfully.");
        }
        else
        {
            builder.Services.AddMemoryCache();
            // Cache service bindings (memory)
            builder.Services.AddSingleton<ICacheKeyFactory, DefaultCacheKeyFactory>();
            builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
            builder.Services.AddSingleton<ICacheInvalidationPublisher, NoOpInvalidationPublisher>();
            logger.LogInformation("Redis connection string not found. Falling back to in-memory cache.");
        }
    }
}

