using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Common.Interfaces.IServices;

public interface IPhoneOtpService
{
    Task<Result<(Guid IdentityUserId, bool IsNew)>> EnsureUserExistsAsync(string phoneE164, CancellationToken ct = default);
    Task<Result<string>> GenerateLoginCodeAsync(Guid identityUserId, CancellationToken ct = default);
    Task<Result<bool>> VerifyLoginCodeAsync(Guid identityUserId, string code, CancellationToken ct = default);
    Task<Result> ConfirmPhoneAsync(Guid identityUserId, CancellationToken ct = default);
    Task<Result<Guid?>> FindByPhoneAsync(string phoneE164, CancellationToken ct = default);
}

