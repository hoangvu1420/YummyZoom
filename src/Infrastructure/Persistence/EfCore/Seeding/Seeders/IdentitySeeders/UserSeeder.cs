using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.UserAggregate;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Infrastructure.Identity;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Infrastructure.Persistence.EfCore.Seeding.Seeders.IdentitySeeders;

public class UserSeeder : ISeeder
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IIdentityService _identityService;
    private readonly IUserAggregateRepository _userRepository;
    private readonly IDeviceRepository _deviceRepository;
    private readonly IUserDeviceSessionRepository _userDeviceSessionRepository;

    public UserSeeder(
        UserManager<ApplicationUser> userManager,
        IIdentityService identityService,
        IUserAggregateRepository userRepository,
        IDeviceRepository deviceRepository,
        IUserDeviceSessionRepository userDeviceSessionRepository)
    {
        _userManager = userManager;
        _identityService = identityService;
        _userRepository = userRepository;
        _deviceRepository = deviceRepository;
        _userDeviceSessionRepository = userDeviceSessionRepository;
    }

    public string Name => "User";
    public int Order => 20;

    public Task<bool> CanSeedAsync(SeedingContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public async Task<Result> SeedAsync(SeedingContext context, CancellationToken cancellationToken = default)
    {
        // Admin user (legacy default)
        var adminEmail = "administrator@localhost";
        var admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail };
        if (_userManager.Users.All(u => u.UserName != admin.UserName))
        {
            await _userManager.CreateAsync(admin, "Administrator1!");
            await _userManager.AddToRolesAsync(admin, new[] { Roles.Administrator });
        }

        // Two sample users with devices and FCM tokens (idempotent)
        await SeedDefaultUserWithDeviceAsync(context,
            name: "User 1",
            email: "hoangnguyenvu1420@gmail.com",
            password: "123456",
            fcmToken: "eaVgeLKNTgqOSMFsNmUYo5:APA91bEmTbtxCFo_888pNUNEUi6euk66GP6iYbtWV_Sq2uWeb81IPO1LMfKpsabH77N_xjdVwmNgd3ms3xTLf92iK8DVvr-Zh_bOKB-wZIm3Ns3E5T0O5Xs",
            platform: "Android",
            deviceId: "seed-device-1",
            modelName: "Seed Device 1");

        await SeedDefaultUserWithDeviceAsync(context,
            name: "User 2",
            email: "hoangnguyenvu1220@gmail.com",
            password: "123456",
            fcmToken: "cjYujyIsRHGCBNHQaIsvy2:APA91bErEQrSMTKusz8AkdpswagbOwt4x2FqkTJRURMa6xg1HpcDANoxEH0RTz-J4hlC1QXOIKsjBtwBk6pn8JVWNLliQxRYtYHHVrI77doOwPOrLxHQruE",
            platform: "Android",
            deviceId: "seed-device-2",
            modelName: "Seed Device 2");

        return Result.Success();
    }

    private async Task SeedDefaultUserWithDeviceAsync(
        SeedingContext ctx,
        string name,
        string email,
        string password,
        string fcmToken,
        string platform,
        string deviceId,
        string? modelName)
    {
        if (_userManager.Users.Any(u => u.Email == email))
        {
            ctx.Logger.LogInformation("User with email {Email} already exists, skipping seeding", email);
            return;
        }

        try
        {
            // 1) Create identity user + assign User role
            var idResult = await _identityService.CreateIdentityUserAsync(email, password, Roles.User);
            if (idResult.IsFailure)
            {
                ctx.Logger.LogWarning("Failed to create identity user for {Email}: {Error}", email, idResult.Error);
                return;
            }

            // 2) Create domain user
            var userResult = User.Create(UserId.Create(idResult.Value), name, email, null, isActive: true);
            if (userResult.IsFailure)
            {
                ctx.Logger.LogWarning("Failed to create domain user for {Email}: {Error}", email, userResult.Error);
                return;
            }

            userResult.Value.ClearDomainEvents();
            await _userRepository.AddAsync(userResult.Value);
            await ctx.DbContext.SaveChangesAsync();

            // 3) Device + Session
            var device = await _deviceRepository.GetByDeviceIdAsync(deviceId);
            if (device == null)
            {
                device = await _deviceRepository.AddAsync(deviceId, platform, modelName);
                await ctx.DbContext.SaveChangesAsync();
            }

            await _userDeviceSessionRepository.AddSessionAsync(idResult.Value, device.Id, fcmToken);
            await ctx.DbContext.SaveChangesAsync();

            ctx.Logger.LogInformation("Seeded default user {Email} with device {DeviceId} (ID: {UserId})", email, deviceId, idResult.Value);
        }
        catch (Exception ex)
        {
            ctx.Logger.LogError(ex, "Failed to seed default user {Email}", email);
        }
    }
}
