namespace YummyZoom.Application.Common.Interfaces.IServices;

public interface IUserOnboardingService
{
    Task<YummyZoom.SharedKernel.Result<bool>> IsOnboardingCompleteAsync(Guid identityUserId, CancellationToken ct = default);
    Task<YummyZoom.SharedKernel.Result> MarkOnboardingCompleteAsync(Guid identityUserId, CancellationToken ct = default);
}

