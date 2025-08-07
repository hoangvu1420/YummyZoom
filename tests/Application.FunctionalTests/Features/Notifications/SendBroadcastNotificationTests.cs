using YummyZoom.Application.Common.Exceptions;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Application.FunctionalTests.Infrastructure;
using YummyZoom.Application.Notifications;
using YummyZoom.Application.Notifications.Commands.SendBroadcastNotification;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.FunctionalTests.Features.Notifications;

using static Testing;

public class SendBroadcastNotificationTests : NotificationTestsBase
{
    [SetUp]
    public async Task TestSetup()
    {
        await EnsureRolesExistAsync(Roles.Administrator, Roles.User);
    }

    #region Success Scenarios

    [Test]
    public async Task SendBroadcastNotification_AsAdministrator_WithActiveDevices_ShouldSucceed()
    {
        // Arrange
        await RunAsAdministratorAsync();
        await SetupMultipleUsersWithDevicesAsync(
            ("user1@test.com", "token1"),
            ("user2@test.com", "token2"),
            ("user3@test.com", "token3")
        );
        
        var command = new SendBroadcastNotificationCommand(
            "Special Announcement",
            "New feature available for all users!",
            new Dictionary<string, string> { { "feature", "new_menu" }, { "type", "announcement" } });

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
    }

    [Test]
    public async Task SendBroadcastNotification_WithDataPayload_ShouldSucceed()
    {
        // Arrange
        await RunAsAdministratorAsync();
        await SetupMultipleUsersWithDevicesAsync(
            ("user1@test.com", "token1"),
            ("user2@test.com", "token2"),
            ("user3@test.com", "token3")
        );
        
        var dataPayload = new Dictionary<string, string>
        {
            { "action", "view_promotion" },
            { "promotionId", "PROMO123" },
            { "category", "all" },
            { "urgency", "high" }
        };
        
        var command = new SendBroadcastNotificationCommand(
            "Flash Sale",
            "50% off everything! Limited time only.",
            dataPayload);

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
    }

    [Test]
    public async Task SendBroadcastNotification_WithoutDataPayload_ShouldSucceed()
    {
        // Arrange
        await RunAsAdministratorAsync();
        await SetupMultipleUsersWithDevicesAsync(
            ("user1@test.com", "token1"),
            ("user2@test.com", "token2"),
            ("user3@test.com", "token3")
        );
        
        var command = new SendBroadcastNotificationCommand(
            "Simple Broadcast",
            "This is a simple message to all users");

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
    }

    [Test]
    public async Task SendBroadcastNotification_WithMixedDeviceStates_ShouldSucceed()
    {
        // Arrange
        await RunAsAdministratorAsync();
        await SetupUsersWithMixedDeviceStatesAsync();
        
        var command = new SendBroadcastNotificationCommand(
            "Mixed States Test",
            "Should only reach active devices");

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        
        // Verify that only active sessions exist in system
        var activeSessionCount = await CountAllActiveSessionsAsync();
        activeSessionCount.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task SendBroadcastNotification_WithSingleUser_ShouldSucceed()
    {
        // Arrange
        await RunAsAdministratorAsync();
        await SetupUserWithDeviceAsync("singleuser@test.com", "single-token");
        
        var command = new SendBroadcastNotificationCommand(
            "Solo User Test",
            "Testing broadcast to single user");

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
    }

    #endregion

    #region Authorization Scenarios

    [Test]
    public async Task SendBroadcastNotification_AsNonAdministrator_ShouldReturnForbidden()
    {
        // Arrange
        var customerId = await RunAsUserAsync("customer@test.com", TestConfiguration.DefaultUsers.CommonTestPassword, new[] { Roles.User });
        
        var command = new SendBroadcastNotificationCommand(
            "Unauthorized Broadcast",
            "This should fail");

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Test]
    public async Task SendBroadcastNotification_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        // No user authentication setup
        var command = new SendBroadcastNotificationCommand(
            "Unauthenticated Broadcast",
            "This should fail");

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    #endregion

    #region Validation Scenarios

    [Test]
    public async Task SendBroadcastNotification_WithEmptyTitle_ShouldFailValidation()
    {
        // Arrange
        await RunAsAdministratorAsync();
        var command = new SendBroadcastNotificationCommand(
            "", // Empty title
            "Valid body");

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task SendBroadcastNotification_WithTooLongTitle_ShouldFailValidation()
    {
        // Arrange
        await RunAsAdministratorAsync();
        var tooLongTitle = new string('a', 101); // Exceeds 100 char limit
        var command = new SendBroadcastNotificationCommand(
            tooLongTitle,
            "Valid body");

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task SendBroadcastNotification_WithEmptyBody_ShouldFailValidation()
    {
        // Arrange
        await RunAsAdministratorAsync();
        var command = new SendBroadcastNotificationCommand(
            "Valid title",
            ""); // Empty body

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Test]
    public async Task SendBroadcastNotification_WithTooLongBody_ShouldFailValidation()
    {
        // Arrange
        await RunAsAdministratorAsync();
        var tooLongBody = new string('b', 501); // Exceeds 500 char limit
        var command = new SendBroadcastNotificationCommand(
            "Valid title",
            tooLongBody);

        // Act
        Func<Task> act = () => SendAsync(command);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    #endregion

    #region Error Scenarios

    [Test]
    public async Task SendBroadcastNotification_WithNoActiveDevices_ShouldReturnNoDevicesError()
    {
        // Arrange
        await RunAsAdministratorAsync();
        // No devices registered

        var command = new SendBroadcastNotificationCommand(
            "No Devices Test",
            "No users have registered devices");

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure(NotificationErrors.NoActiveDevicesForBroadcast().Code);
    }

    [Test]
    public async Task SendBroadcastNotification_WithOnlyInactiveDevices_ShouldReturnNoDevicesError()
    {
        // Arrange
        await RunAsAdministratorAsync();
        await SetupUsersWithOnlyInactiveDevicesAsync();

        var command = new SendBroadcastNotificationCommand(
            "Only Inactive Test",
            "All devices are inactive");

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeFailure(NotificationErrors.NoActiveDevicesForBroadcast().Code);
    }

    #endregion

    #region Integration Scenarios

    [Test]
    public async Task SendBroadcastNotification_CompleteWorkflow_ShouldWorkCorrectly()
    {
        // Arrange - Setup complete scenario with multiple users
        await RunAsAdministratorAsync();
        
        // Create multiple users and register devices
        await SetupCompleteWorkflowUsersAsync();
        
        var broadcastCommand = new SendBroadcastNotificationCommand(
            "Workflow Test Broadcast",
            "Testing complete broadcast workflow",
            new Dictionary<string, string> { { "workflowType", "complete" } });

        // Act
        var result = await SendAsync(broadcastCommand);

        // Assert
        result.ShouldBeSuccessful();
        
        // Verify multiple sessions exist and are active
        var activeSessionCount = await CountAllActiveSessionsAsync();
        activeSessionCount.Should().BeGreaterThan(2);
    }

    [Test]
    public async Task SendBroadcastNotification_LargeUserBase_ShouldHandleEfficiently()
    {
        // Arrange
        await RunAsAdministratorAsync();
        await SetupLargeUserBaseAsync();
        
        var command = new SendBroadcastNotificationCommand(
            "Large Scale Test",
            "Testing broadcast to many users");

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
        
        // Verify large number of sessions
        var activeSessionCount = await CountAllActiveSessionsAsync();
        activeSessionCount.Should().BeGreaterThan(5);
    }

    #endregion

    #region Performance Scenarios

    [Test]
    public async Task SendBroadcastNotification_RepeatedCalls_ShouldBeConsistent()
    {
        // Arrange
        await RunAsAdministratorAsync();
        await SetupMultipleUsersWithDevicesAsync(
            ("user1@test.com", "token1"),
            ("user2@test.com", "token2"),
            ("user3@test.com", "token3")
        );
        
        var command = new SendBroadcastNotificationCommand(
            "Repeated Test",
            "Testing repeated broadcast calls");

        // Act & Assert - Multiple calls should all succeed
        for (int i = 0; i < 3; i++)
        {
            var result = await SendAsync(command);
            result.ShouldBeSuccessful();
        }
    }

    #endregion
}
