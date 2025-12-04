using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Common.Interfaces.IServices;

/// <summary>
/// Sends FCM data-only pushes for TeamCart changes to all active member devices.
/// Payload: { type: "teamcart", teamCartId, version, state, title, body, message, route }
/// </summary>
public interface ITeamCartPushNotifier
{
    Task<Result> PushTeamCartDataAsync(TeamCartId teamCartId, long version, CancellationToken cancellationToken = default);
}

