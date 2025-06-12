using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.Notifications.Commands.SendNotificationToUser;
using YummyZoom.Application.Notifications;
using YummyZoom.Application.Users.Commands.RegisterDevice;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.Infrastructure.Data;
using YummyZoom.SharedKernel.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace YummyZoom.Application.FunctionalTests.Notifications;

using static Testing;

public class SendNotificationToUserTests : NotificationTestsBase
{
    [SetUp]
    public async Task TestSetup()
    {
        await EnsureRolesExistAsync(Roles.Administrator, Roles.Customer);
    }

    #region Success Scenarios

    [Test]
    public async Task SendNotificationToUser_AsAdministrator_WithActiveDevices_ShouldSucceed()
    {
        // Arrange
        await RunAsAdministratorAsync();
        var targetUserId = await SetupUserWithDeviceAsync("target@test.com", "test-fcm-token-123");
        
        var command = new SendNotificationToUserCommand(
            targetUserId,
            "Order Update",
            "Your order has been confirmed!",
            new Dictionary<string, string> { { "orderId", "ORD-123" }, { "type", "order_update" } });

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
    }

    [Test]
    public async Task SendNotificationToUser_WithDataPayload_ShouldSucceed()
    {
        // Arrange
        await RunAsAdministratorAsync();
        var targetUserId = await SetupUserWithDeviceAsync("user@test.com", "token-with-data");
        
        var dataPayload = new Dictionary<string, string>
        {
            { "action", "view_order" },
            { "orderId", "123" },
            { "category", "food" }
        };
        
        var command = new SendNotificationToUserCommand(
            targetUserId,
            "Special Offer",
            "Check out our new menu!",
            dataPayload);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
    }

    [Test]
    public async Task SendNotificationToUser_WithMultipleDevices_ShouldSucceed()
    {
        // Arrange
        await RunAsAdministratorAsync();
        var targetUserId = await SetupUserWithMultipleDevicesAsync("multidevice@test.com", 
            new[] { "token1", "token2", "token3" });
        
        var command = new SendNotificationToUserCommand(
            targetUserId,
            "Multi-Device Test",
            "This should reach all your devices");

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        
        // Verify user has multiple active devices
        var deviceCount = await CountActiveDevicesForUserAsync(targetUserId);
        deviceCount.Should().Be(3);
    }

    [Test]
    public async Task SendNotificationToUser_WithoutDataPayload_ShouldSucceed()
    {
        // Arrange
        await RunAsAdministratorAsync();
        var targetUserId = await SetupUserWithDeviceAsync("simple@test.com", "simple-token");
        
        var command = new SendNotificationToUserCommand(
            targetUserId,
            "Simple Message",
            "No extra data needed");

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
    }

    #endregion

    #region Authorization Scenarios

    [Test]
    public async Task SendNotificationToUser_AsNonAdministrator_ShouldReturnForbidden()
    {
        // Arrange
        var customerId = await RunAsUserAsync("customer@test.com", "Password123!", new[] { Roles.Customer });
        var targetUserId = await SetupUserWithDeviceAsync("target@test.com", "target-token");
        
        var command = new SendNotificationToUserCommand(
            targetUserId,
            "Unauthorized Test",
            "This should fail");

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task SendNotificationToUser_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        // No user authentication setup
        var command = new SendNotificationToUserCommand(
            Guid.NewGuid(),
            "Unauthenticated Test",
            "This should fail");

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    #endregion

    #region Validation Scenarios

    [Test]
    public async Task SendNotificationToUser_WithEmptyTitle_ShouldFailValidation()
    {
        // Arrange
        await RunAsAdministratorAsync();
        var command = new SendNotificationToUserCommand(
            Guid.NewGuid(),
            "", // Empty title
            "Valid body");

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task SendNotificationToUser_WithTooLongTitle_ShouldFailValidation()
    {
        // Arrange
        await RunAsAdministratorAsync();
        var tooLongTitle = new string('a', 101); // Exceeds 100 char limit
        var command = new SendNotificationToUserCommand(
            Guid.NewGuid(),
            tooLongTitle,
            "Valid body");

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task SendNotificationToUser_WithEmptyBody_ShouldFailValidation()
    {
        // Arrange
        await RunAsAdministratorAsync();
        var command = new SendNotificationToUserCommand(
            Guid.NewGuid(),
            "Valid title",
            ""); // Empty body

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task SendNotificationToUser_WithTooLongBody_ShouldFailValidation()
    {
        // Arrange
        await RunAsAdministratorAsync();
        var tooLongBody = new string('b', 501); // Exceeds 500 char limit
        var command = new SendNotificationToUserCommand(
            Guid.NewGuid(),
            "Valid title",
            tooLongBody);

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task SendNotificationToUser_WithEmptyUserId_ShouldFailValidation()
    {
        // Arrange
        await RunAsAdministratorAsync();
        var command = new SendNotificationToUserCommand(
            Guid.Empty, // Empty UserId
            "Valid title",
            "Valid body");

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    #endregion

    #region Error Scenarios

    [Test]
    public async Task SendNotificationToUser_WithNoActiveDevices_ShouldReturnNoDevicesError()
    {
        // Arrange
        await RunAsAdministratorAsync();
        var userWithoutDevices = await RunAsUserAsync("nodevices@test.com", "Password123!", new[] { Roles.Customer });
        
        // Restore administrator context
        await RunAsAdministratorAsync();
        
        var command = new SendNotificationToUserCommand(
            userWithoutDevices,
            "No Devices Test",
            "This user has no devices");

        // Act
        var result = await SendAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(NotificationErrors.NoActiveDevices(UserId.Create(userWithoutDevices)).Code);
    }

    [Test]
    public async Task SendNotificationToUser_WithNonExistentUser_ShouldReturnNoDevicesError()
    {
        // Arrange
        await RunAsAdministratorAsync();
        var nonExistentUserId = Guid.NewGuid();
        
        var command = new SendNotificationToUserCommand(
            nonExistentUserId,
            "Non-existent User Test",
            "This user doesn't exist");

        // Act
        var result = await SendAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(NotificationErrors.NoActiveDevices(UserId.Create(nonExistentUserId)).Code);
    }

    [Test]
    public async Task SendNotificationToUser_WithInactiveDevices_ShouldReturnNoDevicesError()
    {
        // Arrange
        await RunAsAdministratorAsync();
        var targetUserId = await SetupUserWithInactiveDeviceAsync("inactive@test.com", "inactive-token");
        
        var command = new SendNotificationToUserCommand(
            targetUserId,
            "Inactive Device Test",
            "This user has only inactive devices");

        // Act
        var result = await SendAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(NotificationErrors.NoActiveDevices(UserId.Create(targetUserId)).Code);
    }

    #endregion

    #region Integration Scenarios

    [Test]
    public async Task SendNotificationToUser_CompleteWorkflow_ShouldWorkCorrectly()
    {
        // Arrange - Setup complete scenario
        await RunAsAdministratorAsync();
        
        // Create target user and register device
        var targetUserId = await RunAsUserAsync("workflow@test.com", "Password123!", new[] { Roles.Customer });
        var registerCommand = new RegisterDeviceCommand("workflow-token", "iOS", "iPhone-123");
        var registerResult = await SendAsync(registerCommand);
        registerResult.ShouldBeSuccessful();
        
        // Switch back to admin for notification
        await RunAsAdministratorAsync();
        
        var notificationCommand = new SendNotificationToUserCommand(
            targetUserId,
            "Workflow Test",
            "Testing complete notification workflow",
            new Dictionary<string, string> { { "workflowStep", "complete" } });

        // Act
        var result = await SendAsync(notificationCommand);

        // Assert
        result.ShouldBeSuccessful();
        
        // Verify device still exists and is active
        var deviceExists = await FindDeviceByTokenAsync("workflow-token");
        deviceExists.Should().NotBeNull();
        deviceExists!.IsActive.Should().BeTrue();
    }

    #endregion
} 
