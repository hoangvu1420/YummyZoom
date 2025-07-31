using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Models;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.Users.Commands;
using YummyZoom.Application.Users.Commands.RegisterDevice;
using YummyZoom.Application.Users.Commands.UnregisterDevice;
using YummyZoom.Infrastructure.Data;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.FunctionalTests.Features.Users;

using static Testing;

/// <summary>
/// DTO class representing device information for testing purposes.
/// Combines data from Device and UserDeviceSession entities to maintain compatibility with existing tests.
/// </summary>
public class DeviceTestInfo
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FcmToken { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool IsActive { get; set; }
    public string? DeviceId { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class DeviceManagementTests : BaseTestFixture
{
    [SetUp]
    public async Task TestSetup()
    {
        await EnsureRolesExistAsync(Roles.User);
    }

    #region RegisterDevice Tests

    [Test]
    public async Task RegisterDevice_WithValidData_ShouldSucceedAndCreateDevice()
    {
        // Arrange
        var userId = await RunAsDefaultUserAsync();
        var command = new RegisterDeviceCommand(
            FcmToken: "test-fcm-token-12345",
            Platform: "Android",
            DeviceId: "device-123"
        );

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();

        // Verify device was created in database
        var deviceInDb = await FindDeviceByTokenAsync("test-fcm-token-12345");
        deviceInDb.Should().NotBeNull();
        deviceInDb!.UserId.Should().Be(userId);
        deviceInDb.FcmToken.Should().Be("test-fcm-token-12345");
        deviceInDb.Platform.Should().Be("Android");
        deviceInDb.DeviceId.Should().Be("device-123");
        deviceInDb.IsActive.Should().BeTrue();
        deviceInDb.RegisteredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        deviceInDb.LastUsedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Test]
    public async Task RegisterDevice_WithoutDeviceId_ShouldSucceedWithNullDeviceId()
    {
        // Arrange
        var userId = await RunAsDefaultUserAsync();
        var command = new RegisterDeviceCommand(
            FcmToken: "test-fcm-token-minimal",
            Platform: "iOS"
        );

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();

        // Verify device was created with null DeviceId
        var deviceInDb = await FindDeviceByTokenAsync("test-fcm-token-minimal");
        deviceInDb.Should().NotBeNull();
        deviceInDb!.DeviceId.Should().BeNull();
        deviceInDb.Platform.Should().Be("iOS");
    }

    [Test]
    public async Task RegisterDevice_WithExistingToken_ShouldUpdateDevice()
    {
        // Arrange
        var userId = await RunAsDefaultUserAsync();
        
        // Register device first time
        var firstCommand = new RegisterDeviceCommand(
            FcmToken: "update-test-token",
            Platform: "Android",
            DeviceId: "consistent-device"
        );
        var firstResult = await SendAsync(firstCommand);
        firstResult.ShouldBeSuccessful();

        // Get the original registration time
        var originalDevice = await FindDeviceByTokenAsync("update-test-token");
        var originalRegisteredAt = originalDevice!.RegisteredAt;

        // Wait a brief moment to ensure timestamp difference
        await Task.Delay(100);

        // Register same token with same DeviceId but different platform (device info update scenario)
        var updateCommand = new RegisterDeviceCommand(
            FcmToken: "update-test-token",
            Platform: "iOS", // Updated platform
            DeviceId: "consistent-device", // Same DeviceId
            ModelName: "iPhone 15" // Added model info
        );

        // Act
        var updateResult = await SendAsync(updateCommand);

        // Assert
        updateResult.ShouldBeSuccessful();

        // Verify device was updated, not duplicated
        var devicesWithToken = await CountDevicesWithTokenAsync("update-test-token");
        devicesWithToken.Should().Be(1, "Should update existing device, not create new one");

        var updatedDevice = await FindDeviceByTokenAsync("update-test-token");
        updatedDevice!.Platform.Should().Be("iOS", "Platform should be updated");
        updatedDevice.DeviceId.Should().Be("consistent-device", "DeviceId should remain the same");
        updatedDevice.RegisteredAt.Should().Be(originalRegisteredAt, "RegisteredAt should remain unchanged");
        updatedDevice.LastUsedAt.Should().BeAfter(originalRegisteredAt, "LastUsedAt should be updated");
        updatedDevice.UpdatedAt.Should().BeAfter(originalRegisteredAt, "UpdatedAt should be updated");
        updatedDevice.IsActive.Should().BeTrue();
    }

    [Test]
    public async Task RegisterDevice_WithoutAuthentication_ShouldReturnFailure()
    {
        // Arrange
        // No user authentication setup
        var command = new RegisterDeviceCommand(
            FcmToken: "unauthenticated-token",
            Platform: "Android"
        );

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Test]
    public async Task RegisterDevice_WithEmptyFcmToken_ShouldFailValidation()
    {
        // Arrange
        await RunAsDefaultUserAsync();
        var command = new RegisterDeviceCommand(
            FcmToken: "", // Empty token
            Platform: "Android"
        );

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task RegisterDevice_WithEmptyPlatform_ShouldFailValidation()
    {
        // Arrange
        await RunAsDefaultUserAsync();
        var command = new RegisterDeviceCommand(
            FcmToken: "valid-token",
            Platform: "" // Empty platform
        );

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task RegisterDevice_WithTooLongFcmToken_ShouldFailValidation()
    {
        // Arrange
        await RunAsDefaultUserAsync();
        var tooLongToken = new string('a', 513); // 513 characters, exceeds 512 limit
        var command = new RegisterDeviceCommand(
            FcmToken: tooLongToken,
            Platform: "Android"
        );

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task RegisterDevice_WithDifferentDeviceId_ShouldCreateNewDevice()
    {
        // Arrange
        var userId = await RunAsDefaultUserAsync();
        
        // Register device first time
        var firstCommand = new RegisterDeviceCommand(
            FcmToken: "multi-device-token",
            Platform: "Android",
            DeviceId: "phone-123"
        );
        var firstResult = await SendAsync(firstCommand);
        firstResult.ShouldBeSuccessful();

        // Wait a brief moment
        await Task.Delay(100);

        // Register different device with same FCM token (device change scenario)
        var secondCommand = new RegisterDeviceCommand(
            FcmToken: "multi-device-token",
            Platform: "Android",
            DeviceId: "tablet-456"
        );

        // Act
        var secondResult = await SendAsync(secondCommand);

        // Assert
        secondResult.ShouldBeSuccessful();

        // Verify that a new device was created (different DeviceIds = different physical devices)
        var activeDevice = await FindDeviceByTokenAsync("multi-device-token");
        activeDevice.Should().NotBeNull();
        activeDevice!.DeviceId.Should().Be("tablet-456", "Should be using the new device");
        activeDevice.Platform.Should().Be("Android");
        activeDevice.IsActive.Should().BeTrue();
    }

    [Test]
    public async Task RegisterDevice_AppReinstallScenario_ShouldReuseCompatibleDevice()
    {
        // Arrange
        var userId = await RunAsDefaultUserAsync();
        
        // Register device without DeviceId (app installed on device without stable ID)
        var firstCommand = new RegisterDeviceCommand(
            FcmToken: "reinstall-token",
            Platform: "iOS"
        );
        var firstResult = await SendAsync(firstCommand);
        firstResult.ShouldBeSuccessful();

        var originalDevice = await FindDeviceByTokenAsync("reinstall-token");
        var originalRegisteredAt = originalDevice!.RegisteredAt;

        // Wait a brief moment
        await Task.Delay(100);

        // App reinstall: now has DeviceId but same FCM token
        var reinstallCommand = new RegisterDeviceCommand(
            FcmToken: "reinstall-token",
            Platform: "iOS",
            DeviceId: "iphone-789"
        );

        // Act
        var reinstallResult = await SendAsync(reinstallCommand);

        // Assert
        reinstallResult.ShouldBeSuccessful();

        // Should reuse the same device and add the DeviceId
        var devicesWithToken = await CountDevicesWithTokenAsync("reinstall-token");
        devicesWithToken.Should().Be(1, "Should reuse existing device, not create new one");

        var updatedDevice = await FindDeviceByTokenAsync("reinstall-token");
        updatedDevice!.DeviceId.Should().Be("iphone-789", "DeviceId should be added to existing device");
        updatedDevice.Platform.Should().Be("iOS");
        updatedDevice.RegisteredAt.Should().Be(originalRegisteredAt, "RegisteredAt should remain unchanged");
        updatedDevice.IsActive.Should().BeTrue();
    }

    [Test]
    public async Task RegisterDevice_TokenRefreshScenario_ShouldCreateNewSession()
    {
        // Arrange
        var userId = await RunAsDefaultUserAsync();
        
        // Register device first time
        var firstCommand = new RegisterDeviceCommand(
            FcmToken: "old-token",
            Platform: "Android",
            DeviceId: "stable-device-123"
        );
        var firstResult = await SendAsync(firstCommand);
        firstResult.ShouldBeSuccessful();

        // Wait a brief moment
        await Task.Delay(100);

        // Token refresh: same device, new FCM token
        var refreshCommand = new RegisterDeviceCommand(
            FcmToken: "new-token",
            Platform: "Android",
            DeviceId: "stable-device-123"
        );

        // Act
        var refreshResult = await SendAsync(refreshCommand);

        // Assert
        refreshResult.ShouldBeSuccessful();

        // Old token should be inactive
        var oldTokenDevice = await FindDeviceByTokenAsync("old-token");
        oldTokenDevice.Should().BeNull("Old token should be inactive");

        // New token should be active on the same device
        var newTokenDevice = await FindDeviceByTokenAsync("new-token");
        newTokenDevice.Should().NotBeNull();
        newTokenDevice!.DeviceId.Should().Be("stable-device-123");
        newTokenDevice.Platform.Should().Be("Android");
        newTokenDevice.IsActive.Should().BeTrue();
    }

    #endregion

    #region UnregisterDevice Tests

    [Test]
    public async Task UnregisterDevice_WithExistingToken_ShouldSucceedAndRemoveDevice()
    {
        // Arrange
        var userId = await RunAsDefaultUserAsync();
        
        // First register a device
        var registerCommand = new RegisterDeviceCommand(
            FcmToken: "token-to-remove",
            Platform: "Android"
        );
        var registerResult = await SendAsync(registerCommand);
        registerResult.ShouldBeSuccessful();

        // Verify device exists
        var deviceBeforeRemoval = await FindDeviceByTokenAsync("token-to-remove");
        deviceBeforeRemoval.Should().NotBeNull();

        var unregisterCommand = new UnregisterDeviceCommand("token-to-remove");

        // Act
        var result = await SendAsync(unregisterCommand);

        // Assert
        result.ShouldBeSuccessful();

        // Verify device was removed from database
        var deviceAfterRemoval = await FindDeviceByTokenAsync("token-to-remove");
        deviceAfterRemoval.Should().BeNull("Device should be removed from database");
    }

    [Test]
    public async Task UnregisterDevice_WithNonExistentToken_ShouldReturnFailure()
    {
        // Arrange
        await RunAsDefaultUserAsync();
        var command = new UnregisterDeviceCommand("non-existent-token");

        // Act
        var result = await SendAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserDeviceErrors.TokenNotFound("non-existent-token").Code);
    }

    [Test]
    public async Task UnregisterDevice_WithoutAuthentication_ShouldReturnFailure()
    {
        // Arrange
        // No user authentication setup
        var command = new UnregisterDeviceCommand("some-token");

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Test]
    public async Task UnregisterDevice_WithEmptyToken_ShouldFailValidation()
    {
        // Arrange
        await RunAsDefaultUserAsync();
        var command = new UnregisterDeviceCommand(""); // Empty token

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task UnregisterDevice_WithInactiveToken_ShouldReturnFailure()
    {
        // Arrange
        var userId = await RunAsDefaultUserAsync();
        
        // Create a device and inactive session directly in the database using the new model
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var device = new Device
        {
            Id = Guid.NewGuid(),
            DeviceId = "inactive-device-id",
            Platform = "Android",
            ModelName = "Test Device",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        context.Devices.Add(device);
        await context.SaveChangesAsync();

        var inactiveSession = new UserDeviceSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DeviceId = device.Id,
            FcmToken = "inactive-token",
            IsActive = false, // Inactive session
            LastLoginAt = DateTime.UtcNow.AddHours(-1),
            LoggedOutAt = DateTime.UtcNow
        };
        context.UserDeviceSessions.Add(inactiveSession);
        await context.SaveChangesAsync();

        var command = new UnregisterDeviceCommand("inactive-token");

        // Act
        var result = await SendAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserDeviceErrors.TokenNotFound("inactive-token").Code);
    }

    #endregion

    #region Integration Tests

    [Test]
    public async Task DeviceManagement_CompleteWorkflow_ShouldWorkCorrectly()
    {
        // Arrange
        var userId = await RunAsDefaultUserAsync();
        var fcmToken = "workflow-test-token";

        // Act & Assert - Register device
        var registerCommand = new RegisterDeviceCommand(fcmToken, "Android", "test-device");
        var registerResult = await SendAsync(registerCommand);
        registerResult.ShouldBeSuccessful();

        // Verify device exists and is active
        var device = await FindDeviceByTokenAsync(fcmToken);
        device.Should().NotBeNull();
        device!.IsActive.Should().BeTrue();

        // Act & Assert - Update device (re-register with different platform)
        var updateCommand = new RegisterDeviceCommand(fcmToken, "iOS", "updated-device");
        var updateResult = await SendAsync(updateCommand);
        updateResult.ShouldBeSuccessful();

        // Verify device was updated
        var updatedDevice = await FindDeviceByTokenAsync(fcmToken);
        updatedDevice!.Platform.Should().Be("iOS");
        updatedDevice.DeviceId.Should().Be("updated-device");

        // Act & Assert - Unregister device
        var unregisterCommand = new UnregisterDeviceCommand(fcmToken);
        var unregisterResult = await SendAsync(unregisterCommand);
        unregisterResult.ShouldBeSuccessful();

        // Verify device was removed
        var removedDevice = await FindDeviceByTokenAsync(fcmToken);
        removedDevice.Should().BeNull();
    }

    [Test]
    public async Task RegisterDevice_MultipleDevicesForSameUser_ShouldAllowMultipleDevices()
    {
        // Arrange
        var userId = await RunAsDefaultUserAsync();

        // Act - Register multiple devices for the same user
        var androidCommand = new RegisterDeviceCommand("android-token", "Android", "android-device");
        var iosCommand = new RegisterDeviceCommand("ios-token", "iOS", "ios-device");
        var webCommand = new RegisterDeviceCommand("web-token", "Web");

        var androidResult = await SendAsync(androidCommand);
        var iosResult = await SendAsync(iosCommand);
        var webResult = await SendAsync(webCommand);

        // Assert
        androidResult.ShouldBeSuccessful();
        iosResult.ShouldBeSuccessful();
        webResult.ShouldBeSuccessful();

        // Verify all devices exist for the user
        var androidDevice = await FindDeviceByTokenAsync("android-token");
        var iosDevice = await FindDeviceByTokenAsync("ios-token");
        var webDevice = await FindDeviceByTokenAsync("web-token");

        androidDevice.Should().NotBeNull();
        iosDevice.Should().NotBeNull();
        webDevice.Should().NotBeNull();

        // All should belong to the same user
        androidDevice!.UserId.Should().Be(userId);
        iosDevice!.UserId.Should().Be(userId);
        webDevice!.UserId.Should().Be(userId);
    }

    #endregion

    #region Helper Methods

    private static async Task<DeviceTestInfo?> FindDeviceByTokenAsync(string fcmToken)
    {
        using var scope = CreateScope();
        var userDeviceSessionRepository = scope.ServiceProvider.GetRequiredService<IUserDeviceSessionRepository>();
        
        var session = await userDeviceSessionRepository.GetActiveSessionByTokenAsync(fcmToken);
        if (session == null)
            return null;

        // Get the device information
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var device = await context.Devices.FirstOrDefaultAsync(d => d.Id == session.DeviceId);
        
        if (device == null)
            return null;

        // Combine session and device data into the test DTO
        return new DeviceTestInfo
        {
            Id = session.Id,
            UserId = session.UserId,
            FcmToken = session.FcmToken,
            Platform = device.Platform,
            RegisteredAt = device.CreatedAt, // Map device.CreatedAt to RegisteredAt (device registration time)
            LastUsedAt = session.LastLoginAt,   // Use LastLoginAt as LastUsedAt for compatibility
            IsActive = session.IsActive,
            DeviceId = device.DeviceId,
            UpdatedAt = device.UpdatedAt
        };
    }

    private static async Task<int> CountDevicesWithTokenAsync(string fcmToken)
    {
        using var scope = CreateScope();
        var userDeviceSessionRepository = scope.ServiceProvider.GetRequiredService<IUserDeviceSessionRepository>();
        
        var session = await userDeviceSessionRepository.GetActiveSessionByTokenAsync(fcmToken);
        return session != null ? 1 : 0;
    }

    #endregion
}
