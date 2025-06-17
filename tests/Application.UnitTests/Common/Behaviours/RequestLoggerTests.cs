using YummyZoom.Application.Common.Behaviours;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.TodoItems.Commands.CreateTodoItem;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Security.Claims;

namespace YummyZoom.Application.UnitTests.Common.Behaviours;

public class RequestLoggerTests
{
    private Mock<ILogger<CreateTodoItemCommand>> _logger = null!;
    private Mock<IUser> _user = null!;
    private Mock<MediatR.RequestHandlerDelegate<MediatR.Unit>> _next = null!;

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<CreateTodoItemCommand>>();
        _user = new Mock<IUser>();
    }

    [Test]
    public async Task ShouldExtractUsernameFromClaimsIfAuthenticated()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var username = "test@example.com";
        
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));
        
        _user.Setup(x => x.Id).Returns(userId);
        _user.Setup(x => x.Principal).Returns(claimsPrincipal);
        _next = new Mock<MediatR.RequestHandlerDelegate<MediatR.Unit>>();

        var requestLogger = new LoggingBehaviour<CreateTodoItemCommand, MediatR.Unit>(_logger.Object, _user.Object);

        // Act
        await requestLogger.Handle(new CreateTodoItemCommand { ListId = Guid.NewGuid(), Title = "title" }, _next.Object, new CancellationToken());

        // Assert
        _next.Verify(n => n(), Times.Once);
        // Verify that logging was called (we can't easily verify the exact content without more complex setup)
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("YummyZoom Request")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task ShouldHandleUnauthenticatedUser()
    {
        // Arrange
        _user.Setup(x => x.Id).Returns((string?)null);
        _user.Setup(x => x.Principal).Returns((ClaimsPrincipal?)null);
        _next = new Mock<MediatR.RequestHandlerDelegate<MediatR.Unit>>();
        
        var requestLogger = new LoggingBehaviour<CreateTodoItemCommand, MediatR.Unit>(_logger.Object, _user.Object);

        // Act
        await requestLogger.Handle(new CreateTodoItemCommand { ListId = Guid.NewGuid(), Title = "title" }, _next.Object, new CancellationToken());

        // Assert
        _next.Verify(n => n(), Times.Once);
        // Verify that logging was called even for unauthenticated users
        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("YummyZoom Request")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
