using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.UserAggregate;
using YummyZoom.Domain.UserAggregate.Entities;
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
    private readonly IPhoneOtpService _phoneOtpService;

    public UserSeeder(
        UserManager<ApplicationUser> userManager,
        IIdentityService identityService,
        IUserAggregateRepository userRepository,
        IDeviceRepository deviceRepository,
        IUserDeviceSessionRepository userDeviceSessionRepository,
        IPhoneOtpService phoneOtpService)
    {
        _userManager = userManager;
        _identityService = identityService;
        _userRepository = userRepository;
        _deviceRepository = deviceRepository;
        _userDeviceSessionRepository = userDeviceSessionRepository;
        _phoneOtpService = phoneOtpService;
    }

    public string Name => "User";
    public int Order => 20;

    public async Task<bool> CanSeedAsync(SeedingContext context, CancellationToken cancellationToken = default)
    {
        if (await context.DbContext.DomainUsers.AnyAsync(cancellationToken))
        {
            return false;
        }

        return !await _userManager.Users.AnyAsync(cancellationToken);
    }

    public async Task<Result> SeedAsync(SeedingContext context, CancellationToken cancellationToken = default)
    {
        var adminCreated = await EnsureAdminUserAsync(context, cancellationToken);

        var seededDomainUsers = new List<User>();
        foreach (var scenario in GetOnboardingScenarios())
        {
            var result = await SeedPhoneScenarioAsync(context, scenario, cancellationToken);
            if (result.DomainUser is not null)
            {
                seededDomainUsers.Add(result.DomainUser);
            }
        }

        if (seededDomainUsers.Count > 0)
        {
            context.SharedData["SeededUsers"] = seededDomainUsers;
        }

        context.Logger.LogInformation("[User] Seeding completed: {AdminCount} admin user(s), {UserCount} domain user(s) seeded",
            adminCreated ? 1 : 0,
            seededDomainUsers.Count);

        return Result.Success();
    }

    private async Task<bool> EnsureAdminUserAsync(SeedingContext context, CancellationToken cancellationToken)
    {
        const string adminEmail = "administrator@yummyzoom.vn";
        var admin = await _userManager.FindByEmailAsync(adminEmail);
        if (admin is not null)
        {
            return false; // Admin already exists, not created
        }

        try
        {
            admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail };
            var createResult = await _userManager.CreateAsync(admin, "Administrator1!");
            if (!createResult.Succeeded)
            {
                context.Logger.LogWarning("Failed to create admin user: {Errors}", string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return false;
            }

            var roleResult = await _userManager.AddToRolesAsync(admin, new[] { Roles.Administrator });
            if (!roleResult.Succeeded)
            {
                context.Logger.LogWarning("Failed to assign administrator role to {Email}: {Errors}", adminEmail, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }

            return true; // Admin successfully created
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex, "Failed to seed admin user");
            return false;
        }
    }

    private async Task<PhoneScenarioResult> SeedPhoneScenarioAsync(
        SeedingContext context,
        PhoneUserScenario scenario,
        CancellationToken cancellationToken)
    {
        try
        {
            var ensureResult = await _phoneOtpService.EnsureUserExistsAsync(scenario.PhoneE164, cancellationToken);
            if (ensureResult.IsFailure)
            {
                context.Logger.LogWarning("Failed to ensure identity user for {Phone}: {Error}", scenario.PhoneE164, ensureResult.Error);
                return PhoneScenarioResult.Empty;
            }

            var (identityUserId, isNewIdentity) = ensureResult.Value;

            if (scenario.ConfirmPhone)
            {
                var confirmResult = await _phoneOtpService.ConfirmPhoneAsync(identityUserId, cancellationToken);
                if (confirmResult.IsFailure)
                {
                    context.Logger.LogWarning("Failed to confirm phone {Phone}: {Error}", scenario.PhoneE164, confirmResult.Error);
                }
            }

            // Ensure baseline User role assignment
            if (!await _identityService.IsInRoleAsync(identityUserId.ToString(), Roles.User))
            {
                var assignRole = await _identityService.AddUserToRoleAsync(identityUserId, Roles.User);
                if (assignRole.IsFailure)
                {
                    context.Logger.LogWarning("Failed to assign user role for {Phone}: {Error}", scenario.PhoneE164, assignRole.Error);
                }
            }

            if (!scenario.CompleteSignup)
            {
                context.Logger.LogDebug("Seeded OTP-only identity {Phone} (new: {IsNew})", scenario.PhoneE164, isNewIdentity);
                return PhoneScenarioResult.Create(identityUserId, null, false);
            }

            if (string.IsNullOrWhiteSpace(scenario.Name) || string.IsNullOrWhiteSpace(scenario.Email))
            {
                context.Logger.LogWarning("Skipping signup seeding for {Phone}: missing name or email", scenario.PhoneE164);
                return PhoneScenarioResult.Create(identityUserId, null, false);
            }

            var domainUser = await CreateOrUpdateDomainUserAsync(
                context,
                identityUserId,
                scenario,
                cancellationToken);

            if (domainUser is null)
            {
                return PhoneScenarioResult.Create(identityUserId, null, true);
            }

            if (!string.IsNullOrWhiteSpace(scenario.Password))
            {
                var hasPassword = await _identityService.HasPasswordAsync(identityUserId);
                if (!hasPassword)
                {
                    var passwordResult = await _identityService.SetPasswordAsync(identityUserId.ToString(), scenario.Password);
                    if (passwordResult.IsFailure)
                    {
                        context.Logger.LogWarning("Failed to set password for {Email}: {Error}", scenario.Email, passwordResult.Error);
                    }
                }
            }

            if (scenario.PrimaryAddress is not null)
            {
                await EnsurePrimaryAddressAsync(context, domainUser, scenario.PrimaryAddress, cancellationToken);
            }

            if (scenario.Device is not null)
            {
                await EnsureDeviceSessionAsync(context, identityUserId, scenario.Device, cancellationToken);
            }

            context.Logger.LogDebug(
                "Seeded onboarded user {Email} ({Phone}) with address: {HasAddress}, device: {HasDevice}",
                scenario.Email,
                scenario.PhoneE164,
                scenario.PrimaryAddress is not null,
                scenario.Device is not null);

            return PhoneScenarioResult.Create(identityUserId, domainUser, true);
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex, "Failed to seed onboarding scenario for {Phone}", scenario.PhoneE164);
            return PhoneScenarioResult.Empty;
        }
    }

    private async Task<User?> CreateOrUpdateDomainUserAsync(
        SeedingContext context,
        Guid identityUserId,
        PhoneUserScenario scenario,
        CancellationToken cancellationToken)
    {
        var userId = UserId.Create(identityUserId);
        var existing = await _userRepository.GetByIdAsync(userId, cancellationToken);

        var name = scenario.Name!.Trim();
        var email = scenario.Email!.Trim();

        if (existing is null)
        {
            var createResult = User.Create(
                userId,
                name,
                email,
                scenario.PhoneE164,
                isActive: true);

            if (createResult.IsFailure)
            {
                context.Logger.LogWarning("Failed to create domain user for {Email}: {Error}", email, createResult.Error);
                return null;
            }

            var user = createResult.Value;
            user.ClearDomainEvents();

            await _userRepository.AddAsync(user, cancellationToken);
            await context.DbContext.SaveChangesAsync(cancellationToken);

            // Sync identity email (username stays phone per OTP flow)
            var emailResult = await _identityService.UpdateEmailAsync(identityUserId.ToString(), email);
            if (emailResult.IsFailure)
            {
                context.Logger.LogWarning("Failed to update identity email for {Email}: {Error}", email, emailResult.Error);
            }

            return user;
        }

        var needsUpdate = false;

        if (!string.Equals(existing.Name, name, StringComparison.Ordinal) ||
            !string.Equals(existing.PhoneNumber, scenario.PhoneE164, StringComparison.Ordinal))
        {
            var profileResult = existing.UpdateProfile(name, scenario.PhoneE164);
            if (profileResult.IsFailure)
            {
                context.Logger.LogWarning("Failed to update profile for {Email}: {Error}", email, profileResult.Error);
            }
            else
            {
                needsUpdate = true;
            }
        }

        if (!string.Equals(existing.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            var emailResult = existing.UpdateEmail(email);
            if (emailResult.IsFailure)
            {
                context.Logger.LogWarning("Failed to update domain email for {Email}: {Error}", email, emailResult.Error);
            }
            else
            {
                needsUpdate = true;
            }

            var identityEmailResult = await _identityService.UpdateEmailAsync(identityUserId.ToString(), email);
            if (identityEmailResult.IsFailure)
            {
                context.Logger.LogWarning("Failed to update identity email for {Email}: {Error}", email, identityEmailResult.Error);
            }
        }

        if (needsUpdate)
        {
            existing.ClearDomainEvents();
            await _userRepository.UpdateAsync(existing, cancellationToken);
            await context.DbContext.SaveChangesAsync(cancellationToken);
        }

        return existing;
    }

    private async Task EnsurePrimaryAddressAsync(
        SeedingContext context,
        User domainUser,
        AddressSeed addressSeed,
        CancellationToken cancellationToken)
    {
        var primary = domainUser.Addresses.FirstOrDefault();
        var trimmed = addressSeed.Trimmed();
        var addressUpdated = false;

        if (primary is null)
        {
            var state = trimmed.State ?? string.Empty;
            var address = Address.Create(
                trimmed.Street,
                trimmed.City,
                state,
                trimmed.ZipCode,
                trimmed.Country,
                trimmed.Label,
                trimmed.DeliveryInstructions);

            var addResult = domainUser.AddAddress(address);
            if (addResult.IsFailure)
            {
                context.Logger.LogWarning("Failed to add address for {UserId}: {Error}", domainUser.Id.Value, addResult.Error);
                return;
            }

            addressUpdated = true;
        }
        else
        {
            if (!AddressEquals(primary, trimmed))
            {
                var state = trimmed.State ?? string.Empty;
                primary.UpdateDetails(
                    trimmed.Street,
                    trimmed.City,
                    state,
                    trimmed.ZipCode,
                    trimmed.Country,
                    trimmed.Label,
                    trimmed.DeliveryInstructions);
                addressUpdated = true;
            }

            foreach (var extra in domainUser.Addresses.Skip(1).ToList())
            {
                var removeResult = domainUser.RemoveAddress(extra.Id);
                if (removeResult.IsFailure)
                {
                    context.Logger.LogWarning("Failed to remove extra address {AddressId} for user {UserId}: {Error}", extra.Id.Value, domainUser.Id.Value, removeResult.Error);
                }
                else
                {
                    addressUpdated = true;
                }
            }
        }

        if (addressUpdated)
        {
            domainUser.ClearDomainEvents();
            await _userRepository.UpdateAsync(domainUser, cancellationToken);
            await context.DbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static bool AddressEquals(Address existing, AddressSeed trimmed)
    {
        return string.Equals(existing.Street, trimmed.Street, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(existing.City, trimmed.City, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(existing.State, trimmed.State, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(existing.ZipCode, trimmed.ZipCode, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(existing.Country, trimmed.Country, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(existing.Label ?? string.Empty, trimmed.Label ?? string.Empty, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(existing.DeliveryInstructions ?? string.Empty, trimmed.DeliveryInstructions ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private async Task EnsureDeviceSessionAsync(
        SeedingContext context,
        Guid identityUserId,
        DeviceSeed deviceSeed,
        CancellationToken cancellationToken)
    {
        var platform = deviceSeed.Platform.Trim();
        var model = deviceSeed.ModelName?.Trim();
        var deviceId = string.IsNullOrWhiteSpace(deviceSeed.DeviceId) ? null : deviceSeed.DeviceId.Trim();
        var fcmToken = deviceSeed.FcmToken.Trim();

        var device = await _deviceRepository.GetByDeviceIdAsync(deviceId, cancellationToken);
        if (device is null)
        {
            device = await _deviceRepository.AddAsync(deviceId, platform, model, cancellationToken);
        }
        else
        {
            device.Platform = platform;
            device.ModelName = model;
            device.UpdatedAt = DateTime.UtcNow;
        }

        await context.DbContext.SaveChangesAsync(cancellationToken);

        var existingSession = await _userDeviceSessionRepository.GetActiveSessionByTokenAsync(fcmToken, cancellationToken);
        if (existingSession is not null)
        {
            existingSession.IsActive = false;
            existingSession.LoggedOutAt = DateTime.UtcNow;
            await context.DbContext.SaveChangesAsync(cancellationToken);
        }

        await _userDeviceSessionRepository.DeactivateSessionsForDeviceAsync(device.Id, cancellationToken);
        await _userDeviceSessionRepository.AddSessionAsync(identityUserId, device.Id, fcmToken, cancellationToken);
        await context.DbContext.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<PhoneUserScenario> GetOnboardingScenarios() =>
        new List<PhoneUserScenario>
        {
            new PhoneUserScenario(
                PhoneE164: "+84901234560",
                Name: "Nguyễn Thị Minh Anh",
                Email: "minhanh.nguyen@yummyzoom.vn",
                Password: "MinhAnhSeed123!",
                PrimaryAddress: new AddressSeed(
                    Street: "56 Phố Huế, Phường Ngô Quyền",
                    City: "Hà Nội",
                    State: "Hà Nội",
                    ZipCode: "100000",
                    Country: "VN",
                    Label: "Nhà riêng",
                    DeliveryInstructions: "Gọi trước khi đến"),
                Device: new DeviceSeed(
                    Platform: "iOS",
                    ModelName: "iPhone 15 Pro",
                    DeviceId: "seed-ios-minhanh",
                    FcmToken: "seedTokenMinhAnhIOS"),
                CompleteSignup: true),
            new PhoneUserScenario(
                PhoneE164: "+84901234561",
                Name: "Trần Quang Huy",
                Email: "huy.tran@yummyzoom.vn",
                Password: null,
                PrimaryAddress: new AddressSeed(
                    Street: "89 Đội Cấn, Phường Liễu Giai",
                    City: "Hà Nội",
                    State: "Hà Nội",
                    ZipCode: "100100",
                    Country: "VN",
                    Label: "Chung cư",
                    DeliveryInstructions: "Nhận tại sảnh tòa A block"),
                Device: new DeviceSeed(
                    Platform: "Android",
                    ModelName: "Samsung Galaxy S24",
                    DeviceId: "seed-android-huy",
                    FcmToken: "seedTokenHuyAndroid"),
                CompleteSignup: true),
            new PhoneUserScenario(
                PhoneE164: "+84901234562",
                Name: "Lê Thu Giang",
                Email: "giang.le@yummyzoom.vn",
                Password: "ThuGiangSeed123!",
                PrimaryAddress: new AddressSeed(
                    Street: "12 Ngõ 27 Xuân Diệu, Phường Quảng An",
                    City: "Hà Nội",
                    State: "Hà Nội",
                    ZipCode: "100200",
                    Country: "VN",
                    Label: "Căn hộ",
                    DeliveryInstructions: "Báo với bảo vệ tòa N"),
                Device: new DeviceSeed(
                    Platform: "iOS",
                    ModelName: "iPhone 14",
                    DeviceId: "seed-ios-giang",
                    FcmToken: "seedTokenGiangIOS"),
                CompleteSignup: true),
            new PhoneUserScenario(
                PhoneE164: "+84901234563",
                Name: "Phạm Đức Long",
                Email: "long.pham@yummyzoom.vn",
                Password: "LongSeed123!",
                PrimaryAddress: new AddressSeed(
                    Street: "210 Nguyễn Trãi, Phường Thượng Đình",
                    City: "Hà Nội",
                    State: "Hà Nội",
                    ZipCode: "100300",
                    Country: "VN",
                    Label: "Văn phòng",
                    DeliveryInstructions: "Lên tầng 5"),
                Device: new DeviceSeed(
                    Platform: "Android",
                    ModelName: "Xiaomi 13T",
                    DeviceId: "seed-android-long",
                    FcmToken: "seedTokenLongAndroid"),
                CompleteSignup: true),
            new PhoneUserScenario(
                PhoneE164: "+84901234564",
                Name: "Đặng Hoài Phương",
                Email: "phuong.dang@yummyzoom.vn",
                Password: null,
                PrimaryAddress: new AddressSeed(
                    Street: "35 Trần Quốc Toản, Phường Trần Hưng Đạo",
                    City: "Hà Nội",
                    State: "Hà Nội",
                    ZipCode: "100400",
                    Country: "VN",
                    Label: "Nhà riêng",
                    DeliveryInstructions: null),
                Device: new DeviceSeed(
                    Platform: "iOS",
                    ModelName: "iPhone 13 mini",
                    DeviceId: null,
                    FcmToken: "seedTokenPhuongIOS"),
                CompleteSignup: true),
            new PhoneUserScenario(
                PhoneE164: "+84901234565",
                Name: "Nguyễn Văn Nam",
                Email: "nam.nguyen@yummyzoom.vn",
                Password: "NamSeed123!",
                PrimaryAddress: new AddressSeed(
                    Street: "18 Ngõ 50 Hoàng Hoa Thám, Phường Ngọc Hà",
                    City: "Hà Nội",
                    State: "Hà Nội",
                    ZipCode: "100500",
                    Country: "VN",
                    Label: "Nhà trọ",
                    DeliveryInstructions: "Gửi tại nhà xe"),
                Device: new DeviceSeed(
                    Platform: "Android",
                    ModelName: "OPPO Find X7",
                    DeviceId: "seed-android-nam",
                    FcmToken: "seedTokenNamAndroid"),
                CompleteSignup: true),
            new PhoneUserScenario(
                PhoneE164: "+84901234566",
                Name: "Bùi Kim Hoa",
                Email: "hoa.bui@yummyzoom.vn",
                Password: null,
                PrimaryAddress: new AddressSeed(
                    Street: "155 Phố Huế, Phường Ngô Thì Nhậm",
                    City: "Hà Nội",
                    State: "Hà Nội",
                    ZipCode: "100600",
                    Country: "VN",
                    Label: "Nhà riêng",
                    DeliveryInstructions: "Tránh chạm báo động trẻ em"),
                Device: new DeviceSeed(
                    Platform: "iOS",
                    ModelName: "iPhone SE 3",
                    DeviceId: "seed-ios-hoa",
                    FcmToken: "seedTokenHoaIOS"),
                CompleteSignup: true),
            new PhoneUserScenario(
                PhoneE164: "+84901234567",
                Name: "Phan Thành Đạt",
                Email: "dat.phan@yummyzoom.vn",
                Password: "DatSeed123!",
                PrimaryAddress: null,
                Device: new DeviceSeed(
                    Platform: "Android",
                    ModelName: "Samsung Galaxy A55",
                    DeviceId: null,
                    FcmToken: "seedTokenDatAndroid"),
                CompleteSignup: true),
            new PhoneUserScenario(
                PhoneE164: "+84901234568",
                Name: "Võ Mỹ Linh",
                Email: "linh.vo@yummyzoom.vn",
                Password: "MyLinhSeed123!",
                PrimaryAddress: new AddressSeed(
                    Street: "22 Ngõ 4 Lạc Long Quân, Phường Bưởi",
                    City: "Hà Nội",
                    State: "Hà Nội",
                    ZipCode: "100700",
                    Country: "VN",
                    Label: "Nhà riêng",
                    DeliveryInstructions: "Giao buổi tối trước 20h"),
                Device: new DeviceSeed(
                    Platform: "iOS",
                    ModelName: "iPhone 15",
                    DeviceId: "seed-ios-linh",
                    FcmToken: "seedTokenLinhIOS"),
                CompleteSignup: true),
            new PhoneUserScenario(
                PhoneE164: "+84901234569",
                Name: null,
                Email: null,
                Password: null,
                PrimaryAddress: null,
                Device: null,
                CompleteSignup: false)
        };

    private sealed record PhoneUserScenario(
        string PhoneE164,
        string? Name,
        string? Email,
        string? Password,
        AddressSeed? PrimaryAddress,
        DeviceSeed? Device,
        bool CompleteSignup,
        bool ConfirmPhone = true);

    private sealed record AddressSeed(
        string Street,
        string City,
        string? State,
        string ZipCode,
        string Country,
        string? Label,
        string? DeliveryInstructions)
    {
        public AddressSeed Trimmed() => new(
            Street.Trim(),
            City.Trim(),
            (State ?? string.Empty).Trim(),
            ZipCode.Trim(),
            Country.Trim(),
            Label?.Trim(),
            DeliveryInstructions?.Trim());
    }

    private sealed record DeviceSeed(
        string Platform,
        string? ModelName,
        string? DeviceId,
        string FcmToken);

    private sealed record PhoneScenarioResult(Guid IdentityUserId, User? DomainUser, bool CompletedSignup)
    {
        public static PhoneScenarioResult Empty { get; } = new(Guid.Empty, null, false);

        public static PhoneScenarioResult Create(Guid identityUserId, User? domainUser, bool completedSignup)
            => new(identityUserId, domainUser, completedSignup);
    }
}
