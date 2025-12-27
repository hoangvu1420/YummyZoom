using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Common.Interfaces.IServices;

public record PayoutProviderRequest(
    Guid PayoutId,
    decimal Amount,
    string Currency,
    string IdempotencyKey);

public record PayoutProviderResult(
    bool Accepted,
    string? ProviderReferenceId);

public interface IPayoutProvider
{
    Task<Result<PayoutProviderResult>> RequestPayoutAsync(
        PayoutProviderRequest request,
        CancellationToken cancellationToken = default);
}
