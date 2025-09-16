using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.SharedKernel;

namespace YummyZoom.Infrastructure.Identity.PhoneOtp;

public class StaticPhoneOtpService : IPhoneOtpService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly StaticPhoneOtpOptions _options;

    public StaticPhoneOtpService(UserManager<ApplicationUser> users, IOptions<StaticPhoneOtpOptions> options)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<Result<(Guid IdentityUserId, bool IsNew)>> EnsureUserExistsAsync(string phoneE164, CancellationToken ct = default)
    {
        var user = await _users.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneE164, ct);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = phoneE164,
                Email = $"{phoneE164.Replace("+", "").Replace("-", "")}@phone.temp", // Temporary email based on phone
                PhoneNumber = phoneE164,
                PhoneNumberConfirmed = false
            };

            var create = await _users.CreateAsync(user);
            if (!create.Succeeded)
            {
                var errors = string.Join(", ", create.Errors.Select(e => e.Description));
                return Result.Failure<(Guid, bool)>(Error.Problem("Otp.UserCreateFailed", errors));
            }

            try
            {
                await _users.AddToRoleAsync(user, YummyZoom.SharedKernel.Constants.Roles.User);
            }
            catch
            {
                // Ignore role assignment failures in MVP mode
            }

            return Result.Success((user.Id, true));
        }

        return Result.Success((user.Id, false));
    }

    public async Task<Result<Guid?>> FindByPhoneAsync(string phoneE164, CancellationToken ct = default)
    {
        var user = await _users.Users.AsNoTracking().FirstOrDefaultAsync(u => u.PhoneNumber == phoneE164, ct);
        return Result.Success(user?.Id);
    }

    public Task<Result<string>> GenerateLoginCodeAsync(Guid identityUserId, CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success(_options.StaticCode));
    }

    public async Task<Result<bool>> VerifyLoginCodeAsync(Guid identityUserId, string code, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(identityUserId.ToString());
        if (user is null)
        {
            return Result.Failure<bool>(Error.NotFound("Otp.UserNotFound", "User not found."));
        }

        var valid = string.Equals(code, _options.StaticCode, StringComparison.Ordinal);
        return Result.Success(valid);
    }

    public async Task<Result> ConfirmPhoneAsync(Guid identityUserId, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(identityUserId.ToString());
        if (user is null)
        {
            return Result.Failure(Error.NotFound("Otp.UserNotFound", "User not found."));
        }

        if (!user.PhoneNumberConfirmed)
        {
            user.PhoneNumberConfirmed = true;
            var update = await _users.UpdateAsync(user);
            if (!update.Succeeded)
            {
                var errors = string.Join(", ", update.Errors.Select(e => e.Description));
                return Result.Failure(Error.Problem("Otp.ConfirmPhoneFailed", errors));
            }
        }

        return Result.Success();
    }
}

