using YummyZoom.Infrastructure.Data;
using YummyZoom.Infrastructure.Data.Interceptors;
using YummyZoom.Infrastructure.Data.Repositories;
using YummyZoom.Infrastructure.Identity;
using YummyZoom.Infrastructure.Notifications;
using YummyZoom.Infrastructure.Notifications.Firebase;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YummyZoom.SharedKernel.Constants;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authorization;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Infrastructure.Payments.Stripe;
using YummyZoom.Domain.Services;
using Stripe;
using System.Text.Json;

namespace YummyZoom.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("YummyZoomDb");
        Guard.Against.Null(connectionString, message: "Connection string 'YummyZoomDb' not found.");

        builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, SoftDeleteInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseNpgsql(connectionString);
        });

        builder.EnrichNpgsqlDbContext<ApplicationDbContext>();

        builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        builder.Services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<ApplicationDbContext>());

        builder.Services.AddScoped<ApplicationDbContextInitialiser>();

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
        builder.Services.AddScoped<IMenuItemRepository, MenuItemRepository>();
        builder.Services.AddScoped<ICouponRepository, CouponRepository>();

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
                policy.AddRequirements(new HasPermissionRequirement(Roles.UserOwner)));

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
}
