using MediatR;
using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.FunctionalTests.Authorization;

/// <summary>
/// Mock command that requires restaurant owner permissions for testing.
/// </summary>
[Authorize(Policy = Policies.MustBeRestaurantOwner)]
public record TestRestaurantOwnerCommand(Guid RestaurantId) : IRequest<Result<Unit>>, IRestaurantCommand
{
    RestaurantId IRestaurantCommand.RestaurantId => Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

/// <summary>
/// Mock command that requires restaurant staff permissions for testing.
/// </summary>
[Authorize(Policy = Policies.MustBeRestaurantStaff)]
public record TestRestaurantStaffCommand(Guid RestaurantId) : IRequest<Result<Unit>>, IRestaurantCommand
{
    RestaurantId IRestaurantCommand.RestaurantId => Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

/// <summary>
/// Mock command with no authorization requirements for testing.
/// </summary>
public record TestUnprotectedCommand(Guid RestaurantId) : IRequest<Result<Unit>>, IRestaurantCommand
{
    RestaurantId IRestaurantCommand.RestaurantId => Domain.RestaurantAggregate.ValueObjects.RestaurantId.Create(RestaurantId);
}

/// <summary>
/// Mock handlers for the test commands - they just return success.
/// </summary>
public class TestRestaurantOwnerCommandHandler : IRequestHandler<TestRestaurantOwnerCommand, Result<Unit>>
{
    public Task<Result<Unit>> Handle(TestRestaurantOwnerCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Success(Unit.Value));
    }
}

public class TestRestaurantStaffCommandHandler : IRequestHandler<TestRestaurantStaffCommand, Result<Unit>>
{
    public Task<Result<Unit>> Handle(TestRestaurantStaffCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Success(Unit.Value));
    }
}

public class TestUnprotectedCommandHandler : IRequestHandler<TestUnprotectedCommand, Result<Unit>>
{
    public Task<Result<Unit>> Handle(TestUnprotectedCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Success(Unit.Value));
    }
}
