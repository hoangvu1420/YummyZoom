using YummyZoom.Application.Common.Interfaces;
using YummyZoom.Domain.UserAggregate;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.Users.Commands.RegisterUser;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<Guid>>
{
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserAggregateRepository _userRepository;

    public RegisterUserCommandHandler(
        IIdentityService identityService,
        IUnitOfWork unitOfWork,
        IUserAggregateRepository userRepository)
    {
        _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public Task<Result<Guid>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // New implementation using the functional UoW pattern
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // 1) Create identity user + assign Customer role
            var idResult = await _identityService.CreateIdentityUserAsync(
                request.Email!, 
                request.Password!, 
                Roles.Customer);
            
            if (idResult.IsFailure) 
                return Result.Failure<Guid>(idResult.Error);

            // 2) Build domain User aggregate
            var roleResult = RoleAssignment.Create(Roles.Customer);
            if (roleResult.IsFailure) 
                return Result.Failure<Guid>(roleResult.Error);

            var userResult = User.Create(
                UserId.Create(idResult.Value),
                request.Name!, 
                request.Email!, 
                null,
                new List<RoleAssignment> { roleResult.Value });
                
            if (userResult.IsFailure) 
                return Result.Failure<Guid>(userResult.Error);

            // 3) Persist the domain user
            await _userRepository.AddAsync(userResult.Value, cancellationToken);

            return Result.Success(idResult.Value);
        }, cancellationToken);
    }
}
