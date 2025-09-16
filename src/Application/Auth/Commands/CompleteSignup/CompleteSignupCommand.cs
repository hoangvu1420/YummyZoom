using FluentValidation;
using MediatR;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.UserAggregate;
using YummyZoom.Domain.UserAggregate.Errors;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Auth.Commands.CompleteSignup;

public record CompleteSignupCommand(
    string Name,
    string? Email = null
) : IRequest<Result>;

public class CompleteSignupCommandValidator : AbstractValidator<CompleteSignupCommand>
{
    public CompleteSignupCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public class CompleteSignupCommandHandler : IRequestHandler<CompleteSignupCommand, Result>
{
    private static readonly Error Unauthorized = Error.Problem("Auth.Unauthorized", "User is not authenticated.");

    private readonly IUser _currentUser;
    private readonly IUserAggregateRepository _userRepository;
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteSignupCommandHandler(
        IUser currentUser,
        IUserAggregateRepository userRepository,
        IIdentityService identityService,
        IUnitOfWork unitOfWork)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Result> Handle(CompleteSignupCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.Id is null)
        {
            return Result.Failure(Unauthorized);
        }

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // Identity user id is the domain user id (one-to-one)
            var userGuid = Guid.Parse(_currentUser.Id);
            var domainUserId = UserId.Create(userGuid);

            // If user already exists, treat as success (idempotent completion)
            var existing = await _userRepository.GetByIdAsync(domainUserId, cancellationToken);
            if (existing is not null)
            {
                return Result.Success();
            }

            // Try to use Identity's username as phone (OTP flow sets username to phone E.164)
            var username = await _identityService.GetUserNameAsync(_currentUser.Id);
            var phoneE164 = username; // may be null or non-phone in other auth modes

            var name = request.Name.Trim();
            var email = string.IsNullOrWhiteSpace(request.Email) ? $"{userGuid:N}@signup.temp" : request.Email.Trim();

            var createResult = User.Create(
                domainUserId,
                name,
                email,
                phoneE164,
                isActive: true);

            if (createResult.IsFailure)
            {
                return Result.Failure(createResult.Error);
            }

            await _userRepository.AddAsync(createResult.Value, cancellationToken);
            return Result.Success();
        }, cancellationToken);
    }
}

