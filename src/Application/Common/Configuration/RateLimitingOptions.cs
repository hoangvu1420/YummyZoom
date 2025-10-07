namespace YummyZoom.Application.Common.Configuration;

/// <summary>
/// Configuration options for OTP rate limiting and throttling.
/// Provides strongly-typed access to rate limiting thresholds and lockout settings.
/// </summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Rate limiting options for OTP request operations.
    /// </summary>
    public OtpRequestOptions OtpRequest { get; set; } = new();

    /// <summary>
    /// Rate limiting options for OTP verification operations.
    /// </summary>
    public OtpVerifyOptions OtpVerify { get; set; } = new();
}

/// <summary>
/// Rate limiting configuration for OTP request operations.
/// </summary>
public sealed class OtpRequestOptions
{
    /// <summary>
    /// Per-IP rate limiting settings applied by middleware.
    /// </summary>
    public PerIpLimits PerIp { get; set; } = new();

    /// <summary>
    /// Per-phone rate limiting settings applied by business logic.
    /// </summary>
    public PerPhoneLimits PerPhone { get; set; } = new();

    public sealed class PerIpLimits
    {
        /// <summary>
        /// Maximum requests per minute per IP address.
        /// </summary>
        public int PerMinute { get; set; } = 5;

        /// <summary>
        /// Maximum requests per hour per IP address.
        /// </summary>
        public int PerHour { get; set; } = 30;
    }

    public sealed class PerPhoneLimits
    {
        /// <summary>
        /// Maximum OTP requests per minute per phone number.
        /// </summary>
        public int PerMinute { get; set; } = 1;

        /// <summary>
        /// Maximum OTP requests per hour per phone number.
        /// </summary>
        public int PerHour { get; set; } = 10;
    }
}

/// <summary>
/// Rate limiting configuration for OTP verification operations.
/// </summary>
public sealed class OtpVerifyOptions
{
    /// <summary>
    /// Per-IP rate limiting settings applied by middleware.
    /// </summary>
    public PerIpLimits PerIp { get; set; } = new();

    /// <summary>
    /// Per-phone lockout settings applied by business logic.
    /// </summary>
    public PerPhoneLimits PerPhone { get; set; } = new();

    public sealed class PerIpLimits
    {
        /// <summary>
        /// Maximum verification attempts per 5 minutes per IP address.
        /// </summary>
        public int Per5Min { get; set; } = 10;
    }

    public sealed class PerPhoneLimits
    {
        /// <summary>
        /// Time window in minutes for tracking failed verification attempts.
        /// </summary>
        public int FailedAttemptsWindowMinutes { get; set; } = 10;

        /// <summary>
        /// Duration in minutes for account lockout after exceeding failed attempts.
        /// </summary>
        public int LockoutMinutes { get; set; } = 5;

        /// <summary>
        /// Maximum failed verification attempts before triggering lockout.
        /// </summary>
        public int MaxFailedAttempts { get; set; } = 5;
    }
}
