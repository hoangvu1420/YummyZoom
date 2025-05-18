using YummyZoom.Application.Common.Behaviours;
using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Application.TodoItems.Commands.CreateTodoItem;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace YummyZoom.Application.UnitTests.Common.Behaviours;

public class RequestLoggerTests
{
    private Mock<ILogger<CreateTodoItemCommand>> _logger = null!;
    private Mock<IUser> _user = null!;
    private Mock<IIdentityService> _identityService = null!;
    private Mock<MediatR.RequestHandlerDelegate<MediatR.Unit>> _next = null!;

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<CreateTodoItemCommand>>();
        _user = new Mock<IUser>();
        _identityService = new Mock<IIdentityService>();
    }

    [Test]
    public async Task ShouldCallGetUserNameAsyncOnceIfAuthenticated()
    {
        _user.Setup(x => x.Id).Returns(Guid.NewGuid().ToString());
        _next = new Mock<MediatR.RequestHandlerDelegate<MediatR.Unit>>();

        var requestLogger = new LoggingBehaviour<CreateTodoItemCommand, MediatR.Unit>(_logger.Object, _user.Object, _identityService.Object);

        await requestLogger.Handle(new CreateTodoItemCommand { ListId = Guid.NewGuid(), Title = "title" }, _next.Object, new CancellationToken());

        _identityService.Verify(i => i.GetUserNameAsync(It.IsAny<string>()), Times.Once); 
        _next.Verify(n => n(), Times.Once);
    }

    [Test]
    public async Task ShouldNotCallGetUserNameAsyncOnceIfUnauthenticated()
    {
        _next = new Mock<MediatR.RequestHandlerDelegate<MediatR.Unit>>();
        var requestLogger = new LoggingBehaviour<CreateTodoItemCommand, MediatR.Unit>(_logger.Object, _user.Object, _identityService.Object);

        await requestLogger.Handle(new CreateTodoItemCommand { ListId = Guid.NewGuid(), Title = "title" }, _next.Object, new CancellationToken());

        _identityService.Verify(i => i.GetUserNameAsync(It.IsAny<string>()), Times.Never); 
        _next.Verify(n => n(), Times.Once);
    }
}
