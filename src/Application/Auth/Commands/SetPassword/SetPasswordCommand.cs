using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Security;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Auth.Commands.SetPassword;

[Authorize(Policy = Policies.CompletedOTP)]
public record SetPasswordCommand(string NewPassword) : IRequest<Result>;

public class SetPasswordCommandValidator : AbstractValidator<SetPasswordCommand>
{
    public SetPasswordCommandValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(6);
    }
}

public class SetPasswordCommandHandler : IRequestHandler<SetPasswordCommand, Result>
{
    private readonly IUser _currentUser;
    private readonly IIdentityService _identityService;
    private readonly ILogger<SetPasswordCommandHandler> _logger;
    
    public SetPasswordCommandHandler(
        IUser currentUser,
        IIdentityService identityService,
        ILogger<SetPasswordCommandHandler> logger)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> Handle(SetPasswordCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.Id is null)
        {
            _logger.LogWarning("User is not authenticated");
            throw new UnauthorizedAccessException();
        }

        return await _identityService.SetPasswordAsync(_currentUser.Id, request.NewPassword);
    }
}
