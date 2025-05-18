using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Users.Commands.RegisterUser;

public record RegisterUserCommand : IRequest<Result<Guid>>
{
    public string? Email { get; init; }
    public string? Password { get; init; }
    public string? Name { get; init; }
}
