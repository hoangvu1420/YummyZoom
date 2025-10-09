using System.Text.Json.Serialization;

namespace YummyZoom.Application.Auth.Commands.CheckAuthStatus;

public class CheckAuthStatusResponse
{
    public bool Success { get; set; }
    public CheckAuthStatusData Data { get; set; } = null!;
}

public class CheckAuthStatusData
{
    public UserStatus UserStatus { get; set; }
    public bool UserExists { get; set; }
    public bool HasPassword { get; set; }
    public bool ProfileComplete { get; set; }
    public UserInfo? UserInfo { get; set; }
    public SecurityInfo SecurityInfo { get; set; } = null!;
}

public class UserInfo
{
    public string FirstName { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime? LastLogin { get; set; }
    public DateTimeOffset AccountCreated { get; set; }
}

public class SecurityInfo
{
    public bool PhoneVerified { get; set; }
    public bool AccountLocked { get; set; }
    public int FailedAttempts { get; set; }
    public DateTime? LockoutUntil { get; set; }
}

public enum UserStatus
{
    [JsonPropertyName("new_user")]
    NewUser,
    [JsonPropertyName("existing_no_password")]
    ExistingNoPassword,
    [JsonPropertyName("existing_with_password")]
    ExistingWithPassword,
    [JsonPropertyName("account_locked")]
    AccountLocked
}
