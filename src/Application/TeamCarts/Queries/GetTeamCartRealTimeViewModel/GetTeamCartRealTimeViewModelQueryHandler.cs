using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.TeamCarts.Models;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.TeamCarts.Queries.GetTeamCartRealTimeViewModel;

/// <summary>
/// Handler that retrieves the TeamCart real-time view model from Redis.
/// This is optimized for live updates and never hits SQL.
/// Authorization is enforced post-fetch: caller must be a member of the team cart.
/// </summary>
public sealed class GetTeamCartRealTimeViewModelQueryHandler : IRequestHandler<GetTeamCartRealTimeViewModelQuery, Result<GetTeamCartRealTimeViewModelResponse>>
{
    private readonly ITeamCartStore _teamCartStore;
    private readonly IUser _currentUser;
    private readonly ILogger<GetTeamCartRealTimeViewModelQueryHandler> _logger;

    public GetTeamCartRealTimeViewModelQueryHandler(
        ITeamCartStore teamCartStore,
        IUser currentUser,
        ILogger<GetTeamCartRealTimeViewModelQueryHandler> logger)
    {
        _teamCartStore = teamCartStore;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<GetTeamCartRealTimeViewModelResponse>> Handle(GetTeamCartRealTimeViewModelQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.DomainUserId is null)
        {
            throw new UnauthorizedAccessException();
        }

        var teamCartId = TeamCartId.Create(request.TeamCartIdGuid);
        
        // Retrieve the real-time view model from Redis
        var viewModel = await _teamCartStore.GetVmAsync(teamCartId, cancellationToken);
        
        if (viewModel is null)
        {
            _logger.LogWarning("TeamCart real-time view model not found: {TeamCartId}", request.TeamCartIdGuid);
            return Result.Failure<GetTeamCartRealTimeViewModelResponse>(GetTeamCartRealTimeViewModelErrors.NotFound);
        }

        // Authorization check: user must be a member
        var currentUserId = _currentUser.DomainUserId.Value;
        if (!viewModel.Members.Any(m => m.UserId == currentUserId))
        {
            _logger.LogWarning("User {UserId} is not a member of TeamCart {TeamCartId}", currentUserId, request.TeamCartIdGuid);
            return Result.Failure<GetTeamCartRealTimeViewModelResponse>(GetTeamCartRealTimeViewModelErrors.NotMember);
        }

        return Result.Success(new GetTeamCartRealTimeViewModelResponse(viewModel));
    }
}
