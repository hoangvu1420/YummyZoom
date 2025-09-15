using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.SharedKernel;

namespace YummyZoom.Infrastructure.Identity.PhoneOtp;

public class IdentityPhoneOtpService : IPhoneOtpService
{
    private readonly UserManager<ApplicationUser> _users;

    public IdentityPhoneOtpService(UserManager<ApplicationUser> users)
    {
        _users = users;
    }

    public async Task<Result<(Guid IdentityUserId, bool IsNew)>> EnsureUserExistsAsync(string phoneE164, CancellationToken ct = default)
    {
        var user = await _users.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneE164, ct);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = phoneE164,
                PhoneNumber = phoneE164,
                PhoneNumberConfirmed = false
            };

            var create = await _users.CreateAsync(user);
            if (!create.Succeeded)
            {
                var errors = string.Join(", ", create.Errors.Select(e => e.Description));
                return Result.Failure<(Guid, bool)>(Error.Unexpected("Otp.UserCreateFailed", errors));
            }

            // Add baseline role if it exists in your system (optional)
            try { await _users.AddToRoleAsync(user, YummyZoom.SharedKernel.Constants.Roles.User); } catch { /* ignore if roles not configured */ }

            return Result.Success((user.Id, true));
        }

        return Result.Success((user.Id, false));
    }

    public async Task<Result<Guid?>> FindByPhoneAsync(string phoneE164, CancellationToken ct = default)
    {
        var user = await _users.Users.AsNoTracking().FirstOrDefaultAsync(u => u.PhoneNumber == phoneE164, ct);
        return Result.Success(user?.Id);
    }

    public async Task<Result<string>> GenerateLoginCodeAsync(Guid identityUserId, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(identityUserId.ToString());
        if (user is null)
            return Result.Failure<string>(Error.NotFound("Otp.UserNotFound", "User not found."));

        var code = await _users.GenerateUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, "PhoneLogin");
        return Result.Success(code);
    }

    public async Task<Result<bool>> VerifyLoginCodeAsync(Guid identityUserId, string code, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(identityUserId.ToString());
        if (user is null)
            return Result.Failure<bool>(Error.NotFound("Otp.UserNotFound", "User not found."));

        var valid = await _users.VerifyUserTokenAsync(user, TokenOptions.DefaultPhoneProvider, "PhoneLogin", code);
        return Result.Success(valid);
    }

    public async Task<Result> ConfirmPhoneAsync(Guid identityUserId, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(identityUserId.ToString());
        if (user is null)
            return Result.Failure(Error.NotFound("Otp.UserNotFound", "User not found."));

        if (!user.PhoneNumberConfirmed)
        {
            user.PhoneNumberConfirmed = true;
            var update = await _users.UpdateAsync(user);
            if (!update.Succeeded)
            {
                var errors = string.Join(", ", update.Errors.Select(e => e.Description));
                return Result.Failure(Error.Unexpected("Otp.ConfirmPhoneFailed", errors));
            }
        }
        return Result.Success();
    }
}

