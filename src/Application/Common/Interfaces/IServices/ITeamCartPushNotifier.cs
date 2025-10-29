using YummyZoom.Domain.TeamCartAggregate.ValueObjects;
using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Common.Interfaces.IServices;

/// <summary>
/// Sends FCM data-only pushes for TeamCart changes to all active member devices.
/// Payload: { teamCartId, version }
/// </summary>
public interface ITeamCartPushNotifier
{
    Task<Result> PushTeamCartDataAsync(TeamCartId teamCartId, CancellationToken cancellationToken = default);
}

