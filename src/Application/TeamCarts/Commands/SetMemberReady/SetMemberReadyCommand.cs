using YummyZoom.Application.Common.Authorization;
using YummyZoom.Application.Common.Interfaces.IRepositories;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Common.Security;
using YummyZoom.Domain.TeamCartAggregate.Errors;
using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;
using YummyZoom.SharedKernel.Constants;

namespace YummyZoom.Application.TeamCarts.Commands.SetMemberReady;

[Authorize(Policy = Policies.MustBeTeamCartMember)]
public sealed record SetMemberReadyCommand(
    Guid TeamCartId,
    bool IsReady
) : IRequest<Result>, ITeamCartCommand
{
    TeamCartId ITeamCartCommand.TeamCartId => Domain.TeamCartAggregate.ValueObjects.TeamCartId.Create(TeamCartId);
}

public sealed class SetMemberReadyCommandHandler : IRequestHandler<SetMemberReadyCommand, Result>
{
    private readonly ITeamCartStore _store;
    private readonly ITeamCartRepository _teamCartRepository;
    private readonly ITeamCartRealtimeNotifier _notifier;
    private readonly ITeamCartPushNotifier _pushNotifier;
    private readonly IUser _currentUser;

    public SetMemberReadyCommandHandler(
        ITeamCartStore store,
        ITeamCartRepository teamCartRepository,
        ITeamCartRealtimeNotifier notifier,
        ITeamCartPushNotifier pushNotifier,
        IUser currentUser)
    {
        _store = store;
        _teamCartRepository = teamCartRepository;
        _notifier = notifier;
        _pushNotifier = pushNotifier;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(SetMemberReadyCommand request, CancellationToken cancellationToken)
    {
        var cartId = TeamCartId.Create(request.TeamCartId);
        var userId = _currentUser.DomainUserId;

        if (userId is null)
        {
            return Result.Failure(TeamCartErrors.UserNotMember);
        }

        var cart = await _store.GetVmAsync(cartId, cancellationToken);
        if (cart is null)
        {
            return Result.Failure(TeamCartErrors.TeamCartNotFound);
        }

        var member = cart.Members.FirstOrDefault(m => m.UserId == userId.Value);
        if (member is null)
        {
            return Result.Failure(TeamCartErrors.UserNotMember);
        }

        await _store.SetMemberReadyAsync(cartId, userId.Value, request.IsReady, cancellationToken);

        var updatedCart = await _store.GetVmAsync(cartId, cancellationToken);
        if (updatedCart is not null && updatedCart.Members.All(m => m.IsReady))
        {
            await _notifier.NotifyAllMembersReady(cartId, cancellationToken);
            
            // Only notify host when all members are ready (reduce spam)
            var actorMember = updatedCart.Members.FirstOrDefault(m => m.UserId == userId.Value);
            var context = new TeamCartNotificationContext
            {
                EventType = "MemberReadyStatusChanged",
                ActorUserId = userId.Value,
                ActorName = actorMember?.Name ?? "Thành viên"
            };
            
            var push = await _pushNotifier.PushTeamCartDataAsync(
                cartId, 
                updatedCart.Version, 
                TeamCartNotificationTarget.Host,
                context,
                NotificationDeliveryType.Hybrid,
                cancellationToken);
                
            if (push.IsFailure)
            {
                return Result.Failure(push.Error);
            }
        }

        return Result.Success();
    }
}
