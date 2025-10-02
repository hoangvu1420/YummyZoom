using MediatR;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.FunctionalTests.Authorization;

// Test user command that requires user ownership
[Authorize(Policy = Policies.MustBeUserOwner)]
public record TestUserOwnerCommand(Guid UserId) : IRequest<Result<Unit>>, IUserCommand
{
    UserId IUserCommand.UserId => Domain.UserAggregate.ValueObjects.UserId.Create(UserId);
}

// Test unprotected user command (no authorization required)
public record TestUnprotectedUserCommand(Guid UserId) : IRequest<Result<Unit>>, IUserCommand
{
    UserId IUserCommand.UserId => Domain.UserAggregate.ValueObjects.UserId.Create(UserId);
}

// Command handlers
public class TestUserOwnerCommandHandler : IRequestHandler<TestUserOwnerCommand, Result<Unit>>
{
    public Task<Result<Unit>> Handle(TestUserOwnerCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Success(Unit.Value));
    }
}

public class TestUnprotectedUserCommandHandler : IRequestHandler<TestUnprotectedUserCommand, Result<Unit>>
{
    public Task<Result<Unit>> Handle(TestUnprotectedUserCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Success(Unit.Value));
    }
}
