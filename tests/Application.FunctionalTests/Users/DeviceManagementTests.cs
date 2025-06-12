using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Users.Commands.RegisterDevice;
using YummyZoom.Application.Users.Commands.UnregisterDevice;
using YummyZoom.Application.Users.Commands;
using YummyZoom.Infrastructure.Data;
using YummyZoom.SharedKernel.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace YummyZoom.Application.FunctionalTests.Users;

using static Testing;

public class DeviceManagementTests : BaseTestFixture
{
    [SetUp]
    public async Task TestSetup()
    {
        await EnsureRolesExistAsync(Roles.Customer);
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
            DeviceId: "old-device"
        );
        var firstResult = await SendAsync(firstCommand);
        firstResult.ShouldBeSuccessful();

        // Get the original registration time
        var originalDevice = await FindDeviceByTokenAsync("update-test-token");
        var originalRegisteredAt = originalDevice!.RegisteredAt;

        // Wait a brief moment to ensure timestamp difference
        await Task.Delay(100);

        // Register same token with different platform
        var updateCommand = new RegisterDeviceCommand(
            FcmToken: "update-test-token",
            Platform: "iOS",
            DeviceId: "new-device"
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
        updatedDevice.DeviceId.Should().Be("new-device", "DeviceId should be updated");
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
        var result = await SendAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserDeviceErrors.UserNotAuthenticated().Code);
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
        var result = await SendAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(UserDeviceErrors.UserNotAuthenticated().Code);
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
        
        // Create an inactive device directly in the database
        var inactiveDevice = new UserDevice
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FcmToken = "inactive-token",
            Platform = "Android",
            RegisteredAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = false // Inactive device
        };
        await AddAsync(inactiveDevice);

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

    private static async Task<UserDevice?> FindDeviceByTokenAsync(string fcmToken)
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        return await context.UserDevices
            .FirstOrDefaultAsync(d => d.FcmToken == fcmToken);
    }

    private static async Task<int> CountDevicesWithTokenAsync(string fcmToken)
    {
        using var scope = CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        return await context.UserDevices
            .CountAsync(d => d.FcmToken == fcmToken);
    }

    #endregion
} 
