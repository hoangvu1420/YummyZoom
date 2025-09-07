using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Domain.RestaurantAggregate.ValueObjects;
using YummyZoom.Domain.TeamCartAggregate;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.Domain.UserAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Commands.CreateTeamCart;

public sealed class CreateTeamCartCommandHandler : IRequestHandler<CreateTeamCartCommand, Result<CreateTeamCartResponse>>
{
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly IRestaurantRepository _restaurantRepository;
    private readonly IUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateTeamCartCommandHandler> _logger;

    public CreateTeamCartCommandHandler(
        ITeamCartRepository teamCartRepository,
        IRestaurantRepository restaurantRepository,
        IUser currentUser,
        IUnitOfWork unitOfWork,
        ILogger<CreateTeamCartCommandHandler> logger)
    {
        _teamCartRepository = teamCartRepository ?? throw new ArgumentNullException(nameof(teamCartRepository));
        _restaurantRepository = restaurantRepository ?? throw new ArgumentNullException(nameof(restaurantRepository));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<CreateTeamCartResponse>> Handle(CreateTeamCartCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // Ensure authenticated user
            if (_currentUser.DomainUserId is null)
            {
                throw new UnauthorizedAccessException();
            }

            var hostUserId = _currentUser.DomainUserId!;
            var restaurantId = RestaurantId.Create(request.RestaurantId);

            // Validate restaurant exists
            var restaurant = await _restaurantRepository.GetByIdAsync(restaurantId, cancellationToken);
            if (restaurant is null)
            {
                _logger.LogWarning("Restaurant not found: {RestaurantId}", request.RestaurantId);
                return Result.Failure<CreateTeamCartResponse>(CreateTeamCartErrors.RestaurantNotFound(request.RestaurantId));
            }

            // Create TeamCart aggregate
            var createResult = TeamCart.Create(hostUserId, restaurantId, request.HostName, request.DeadlineUtc);
            if (createResult.IsFailure)
            {
                _logger.LogWarning("Failed to create TeamCart: {Error}", createResult.Error.Description);
                return Result.Failure<CreateTeamCartResponse>(createResult.Error);
            }

            var cart = createResult.Value;

            await _teamCartRepository.AddAsync(cart, cancellationToken);

            // Respond with identifiers and share token details
            var response = new CreateTeamCartResponse(
                TeamCartId: cart.Id.Value,
                ShareToken: cart.ShareToken.Value,
                ShareTokenExpiresAtUtc: cart.ShareToken.ExpiresAt
            );

            _logger.LogInformation("Created TeamCart {TeamCartId} for Restaurant {RestaurantId} by Host {HostUserId}", cart.Id.Value, restaurantId.Value, hostUserId.Value);

            return Result.Success(response);
        }, cancellationToken);
    }
}
