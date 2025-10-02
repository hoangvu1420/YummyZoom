using YummyZoom.Domain.UserAggregate.ValueObjects;

namespace YummyZoom.Application.Common.Authorization;

public interface IUserCommand : IContextualCommand
{
    UserId UserId { get; }

    // Default implementations
    string IContextualCommand.ResourceType => "User";
    string IContextualCommand.ResourceId => UserId.Value.ToString();
}
