using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.FunctionalTests.Common;
using YummyZoom.Web.Realtime.Hubs;

namespace YummyZoom.Application.FunctionalTests.Web.SignalR;

[TestFixture]
public class TeamCartHubAuthTests : BaseTestFixture
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

    private static ClaimsPrincipal MakePrincipal()
    {
        var identity = new ClaimsIdentity("Test");
        return new ClaimsPrincipal(identity);
    }

    [Test]
    public async Task Subscribe_Authorized_Adds_To_Group()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var authz = new Mock<IAuthorizationService>(MockBehavior.Strict);
        var logger = new Mock<ILogger<TeamCartHub>>();

        authz
            .Setup(a => a.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<object?>(),
                It.IsAny<string>()))
            .ReturnsAsync(AuthorizationResult.Success());

        var hub = new TeamCartHub(authz.Object, logger.Object);

        var context = new Mock<HubCallerContext>(MockBehavior.Strict);
        context.SetupGet(c => c.User).Returns(MakePrincipal());
        context.SetupGet(c => c.ConnectionId).Returns("conn-1");

        var groups = new Mock<IGroupManager>(MockBehavior.Strict);
        groups
            .Setup(g => g.AddToGroupAsync("conn-1", $"teamcart:{cartId}", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        SetHubProperty(hub, nameof(Hub.Context), context.Object);
        SetHubProperty(hub, nameof(Hub.Groups), groups.Object);

        // Act
        await hub.SubscribeToCart(cartId);

        // Assert
        groups.Verify();
    }

    [Test]
    public async Task Subscribe_Forbidden_Throws_HubException()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var authz = new Mock<IAuthorizationService>(MockBehavior.Strict);
        var logger = new Mock<ILogger<TeamCartHub>>();

        authz
            .Setup(a => a.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<object?>(),
                It.IsAny<string>()))
            .ReturnsAsync(AuthorizationResult.Failed());

        var hub = new TeamCartHub(authz.Object, logger.Object);

        var context = new Mock<HubCallerContext>(MockBehavior.Strict);
        context.SetupGet(c => c.User).Returns(MakePrincipal());
        context.SetupGet(c => c.ConnectionId).Returns("conn-2");

        var groups = new Mock<IGroupManager>(MockBehavior.Strict);

        SetHubProperty(hub, nameof(Hub.Context), context.Object);
        SetHubProperty(hub, nameof(Hub.Groups), groups.Object);

        // Act + Assert
        var act = async () => await hub.SubscribeToCart(cartId);
        (await act.Should().ThrowAsync<HubException>()).WithMessage("Forbidden");

        groups.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

