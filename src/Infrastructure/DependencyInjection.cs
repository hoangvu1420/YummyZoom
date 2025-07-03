using YummyZoom.Infrastructure.Data;
using YummyZoom.Infrastructure.Data.Interceptors;
using YummyZoom.Infrastructure.Data.Repositories;
using YummyZoom.Infrastructure.Identity;
using YummyZoom.Infrastructure.Notifications;
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

namespace YummyZoom.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("YummyZoomDb");
        Guard.Against.Null(connectionString, message: "Connection string 'YummyZoomDb' not found.");

        builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
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
        builder.Services.AddTransient<IIdentityService, IdentityService>();
        builder.Services.AddScoped<IUserAggregateRepository, UserAggregateRepository>();
        builder.Services.AddScoped<IRoleAssignmentRepository, RoleAssignmentRepository>();
        builder.Services.AddScoped<IDeviceRepository, DeviceRepository>();
        builder.Services.AddScoped<IUserDeviceSessionRepository, UserDeviceSessionRepository>();

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
    }

    public static void AddFirebaseIfConfigured(this IHostApplicationBuilder builder)
    {
        var fcmAdminKeyJson = builder.Configuration["yummyzoom-fcm-admin-key"];
        
        if (string.IsNullOrWhiteSpace(fcmAdminKeyJson))
        {
            using var loggerFactory = LoggerFactory.Create(config => config.AddConsole());
            var logger = loggerFactory.CreateLogger("Firebase.Initialization");
            logger.LogWarning("Firebase Admin SDK key 'yummyzoom-fcm-admin-key' not found in configuration. FCM will not be initialized.");
            return;
        }

        try
        {
            // Create the app instance from our explicit credentials.
            var app = FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromJson(fcmAdminKeyJson)
            });

            // Get the messaging client FROM OUR EXPLICIT APP INSTANCE.
            var firebaseMessaging = FirebaseMessaging.GetMessaging(app);

            // Register this specific instance as a singleton in the DI container.
            builder.Services.AddSingleton(firebaseMessaging);

            using var loggerFactory = LoggerFactory.Create(config => config.AddConsole());
            var logger = loggerFactory.CreateLogger("Firebase.Initialization");
            logger.LogInformation("Firebase Admin SDK initialized and FirebaseMessaging registered as a singleton.");
        }
        catch (Exception ex)
        {
            using var loggerFactory = LoggerFactory.Create(config => config.AddConsole());
            var logger = loggerFactory.CreateLogger("Firebase.Initialization");
            logger.LogCritical(ex, "Failed to initialize Firebase Admin SDK. FCM will not be available.");
        }
    }
}
