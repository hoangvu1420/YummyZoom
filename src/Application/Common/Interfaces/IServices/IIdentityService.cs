using YummyZoom.SharedKernel;

namespace YummyZoom.Application.Common.Interfaces.IServices;

public interface IIdentityService
{
    Task<string?> GetUserNameAsync(string userId);

    Task<bool> IsInRoleAsync(string userId, string role);

    Task<Result<Guid>> CreateUserAsync(string email, string password, string name);

    Task<Result<Guid>> CreateIdentityUserAsync(string email, string password, string roleName);

    Task<Result> UpdateEmailAsync(string userId, string newEmail);

    Task<Result> UpdateProfileAsync(string userId, string name, string? phoneNumber);

    Task<Result> DeleteUserAsync(string userId);

    Task<Result> AddUserToRoleAsync(Guid userId, string role);

    Task<Result> RemoveUserFromRoleAsync(Guid userId, string role);

    Task<bool> UserExistsAsync(Guid userId);
}
