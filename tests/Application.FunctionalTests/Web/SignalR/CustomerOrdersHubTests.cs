using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Web.Realtime.Hubs;

namespace YummyZoom.Application.FunctionalTests.Web.SignalR;

[TestFixture]
public class CustomerOrdersHubTests : BaseTestFixture
{
    private static void SetHubProperty<T>(Hub hub, string name, T value)
    {
        var prop = typeof(Hub).GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(hub, value);
            return;
        }

        var backingField = typeof(Hub).GetField($"<{name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        if (backingField != null)
        {
            backingField.SetValue(hub, value);
            return;
        }

        throw new InvalidOperationException($"Unable to set Hub property '{name}'.");
    }

    private static ClaimsPrincipal MakePrincipal(string? userId = null)
    {
        var identity = new ClaimsIdentity("Test");
        if (userId != null)
        {
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));
        }
        return new ClaimsPrincipal(identity);
    }

    [Test]
    public async Task SubscribeToOrder_Authorized_Adds_To_Group()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var authz = new Mock<IAuthorizationService>(MockBehavior.Strict);
        var logger = new Mock<ILogger<CustomerOrdersHub>>();

        // Policy succeeds
        authz
            .Setup(a => a.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<object?>(),
                It.IsAny<string>()))
            .ReturnsAsync(AuthorizationResult.Success());

        var hub = new CustomerOrdersHub(authz.Object, logger.Object);

        var context = new Mock<HubCallerContext>(MockBehavior.Strict);
        context.SetupGet(c => c.User).Returns(MakePrincipal("user-123"));
        context.SetupGet(c => c.ConnectionId).Returns("conn-1");

        var groups = new Mock<IGroupManager>(MockBehavior.Strict);
        groups
            .Setup(g => g.AddToGroupAsync("conn-1", $"order:{orderId}", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        SetHubProperty(hub, nameof(Hub.Context), context.Object);
        SetHubProperty(hub, nameof(Hub.Groups), groups.Object);

        // Act
        await hub.SubscribeToOrder(orderId);

        // Assert
        groups.Verify();
    }

    [Test]
    public async Task SubscribeToOrder_Forbidden_Throws_HubException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var authz = new Mock<IAuthorizationService>(MockBehavior.Strict);
        var logger = new Mock<ILogger<CustomerOrdersHub>>();

        // Policy fails
        authz
            .Setup(a => a.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<object?>(),
                It.IsAny<string>()))
            .ReturnsAsync(AuthorizationResult.Failed());

        var hub = new CustomerOrdersHub(authz.Object, logger.Object);

        var context = new Mock<HubCallerContext>(MockBehavior.Strict);
        context.SetupGet(c => c.User).Returns(MakePrincipal("user-456"));
        context.SetupGet(c => c.ConnectionId).Returns("conn-2");

        var groups = new Mock<IGroupManager>(MockBehavior.Strict);
        // No AddToGroupAsync should be called in forbidden path

        SetHubProperty(hub, nameof(Hub.Context), context.Object);
        SetHubProperty(hub, nameof(Hub.Groups), groups.Object);

        // Act + Assert
        var act = async () => await hub.SubscribeToOrder(orderId);
        (await act.Should().ThrowAsync<HubException>()).WithMessage("Forbidden");

        groups.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SubscribeToOrder_NullUser_Throws_HubException()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var authz = new Mock<IAuthorizationService>(MockBehavior.Strict);
        var logger = new Mock<ILogger<CustomerOrdersHub>>();

        var hub = new CustomerOrdersHub(authz.Object, logger.Object);

        var context = new Mock<HubCallerContext>(MockBehavior.Strict);
        context.SetupGet(c => c.User).Returns((ClaimsPrincipal?)null);
        context.SetupGet(c => c.ConnectionId).Returns("conn-3");

        var groups = new Mock<IGroupManager>(MockBehavior.Strict);

        SetHubProperty(hub, nameof(Hub.Context), context.Object);
        SetHubProperty(hub, nameof(Hub.Groups), groups.Object);

        // Act + Assert
        var act = async () => await hub.SubscribeToOrder(orderId);
        (await act.Should().ThrowAsync<HubException>()).WithMessage("Unauthorized");

        groups.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task UnsubscribeFromOrder_Removes_From_Group()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var authz = new Mock<IAuthorizationService>(MockBehavior.Strict);
        var logger = new Mock<ILogger<CustomerOrdersHub>>();

        var hub = new CustomerOrdersHub(authz.Object, logger.Object);

        var context = new Mock<HubCallerContext>(MockBehavior.Strict);
        context.SetupGet(c => c.ConnectionId).Returns("conn-4");

        var groups = new Mock<IGroupManager>(MockBehavior.Strict);
        groups
            .Setup(g => g.RemoveFromGroupAsync("conn-4", $"order:{orderId}", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        SetHubProperty(hub, nameof(Hub.Context), context.Object);
        SetHubProperty(hub, nameof(Hub.Groups), groups.Object);

        // Act
        await hub.UnsubscribeFromOrder(orderId);

        // Assert
        groups.Verify();
    }
}
