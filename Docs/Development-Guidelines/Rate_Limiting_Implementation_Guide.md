# Rate Limiting Implementation Guide

This guide provides step-by-step instructions for implementing rate limiting on endpoints and handlers in the YummyZoom API, based on the patterns established for OTP endpoints.

## Overview

YummyZoom implements a two-layer rate limiting approach:
1. **Middleware-level (Per-IP)**: ASP.NET Core rate limiting middleware
2. **Business-level (Per-Resource)**: Custom throttling logic in command handlers

## When to Implement Rate Limiting

Consider rate limiting for endpoints that:
- Send external communications (SMS, email, push notifications)
- Perform expensive operations (file uploads, complex calculations)
- Access sensitive data or operations
- Are prone to abuse or spam
- Have cost implications (third-party API calls)

## Implementation Steps

### Step 1: Define Rate Limiting Options

Create or extend options classes in `src/Application/Common/Configuration/`:

```csharp
// Example: EmailRateLimitingOptions.cs
public sealed class EmailRateLimitingOptions
{
    public const string SectionName = "RateLimiting:Email";

    public PerIpLimits PerIp { get; set; } = new();
    public PerUserLimits PerUser { get; set; } = new();

    public sealed class PerIpLimits
    {
        public int PerMinute { get; set; } = 10;
        public int PerHour { get; set; } = 100;
    }

    public sealed class PerUserLimits
    {
        public int PerMinute { get; set; } = 2;
        public int PerHour { get; set; } = 20;
    }
}
```

**Key Patterns:**
- Use `const string SectionName` for configuration binding
- Provide sensible defaults in property initializers
- Separate IP-level and resource-level limits
- Use descriptive nested classes for organization

### Step 2: Register Options in DI

Add to `src/Web/DependencyInjection.cs`:

```csharp
// Register options
builder.Services.Configure<EmailRateLimitingOptions>(
    builder.Configuration.GetSection(EmailRateLimitingOptions.SectionName));

// Add middleware policies
builder.Services.AddRateLimiter(options =>
{
    var emailConfig = builder.Configuration
        .GetSection(EmailRateLimitingOptions.SectionName)
        .Get<EmailRateLimitingOptions>() ?? new EmailRateLimitingOptions();
    
    options.AddPolicy("email-send-ip", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = emailConfig.PerIp.PerMinute,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});
```

### Step 3: Create Throttle Store Interface (if needed)

For business-level throttling, create a store interface in `src/Application/Common/Interfaces/IServices/`:

```csharp
// Example: IEmailThrottleStore.cs
public interface IEmailThrottleStore
{
    Task<int> IncrementSendCountAsync(string userId, int windowMinutes, CancellationToken cancellationToken = default);
    Task<int> GetSendCountAsync(string userId, int windowMinutes, CancellationToken cancellationToken = default);
    Task<int> GetRetryAfterSecondsAsync(string userId, int windowMinutes, CancellationToken cancellationToken = default);
    Task ResetSendCountAsync(string userId, CancellationToken cancellationToken = default);
}
```

### Step 4: Implement Throttle Store

Create implementation in `src/Infrastructure/Caching/`:

```csharp
// Example: EmailThrottleStore.cs
public class EmailThrottleStore : IEmailThrottleStore
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<EmailThrottleStore> _logger;
    private const string KeyPrefix = "email:throttle:";

    public EmailThrottleStore(IDistributedCache cache, ILogger<EmailThrottleStore> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<int> IncrementSendCountAsync(string userId, int windowMinutes, CancellationToken cancellationToken = default)
    {
        var key = GetThrottleKey(userId, windowMinutes);
        var expiry = TimeSpan.FromMinutes(windowMinutes);

        try
        {
            var existingBytes = await _cache.GetAsync(key, cancellationToken);
            var currentCount = 0;

            if (existingBytes != null)
            {
                var existingData = JsonSerializer.Deserialize<ThrottleData>(existingBytes);
                currentCount = existingData?.Count ?? 0;
            }

            var newCount = currentCount + 1;
            var newData = new ThrottleData { Count = newCount, Timestamp = DateTimeOffset.UtcNow };
            var newBytes = JsonSerializer.SerializeToUtf8Bytes(newData);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry
            };

            await _cache.SetAsync(key, newBytes, options, cancellationToken);
            return newCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment send count for user {UserId}", userId);
            throw;
        }
    }

    // Implement other methods following similar patterns...

    private static string GetThrottleKey(string userId, int windowMinutes)
        => $"{KeyPrefix}{userId}:{windowMinutes}m";

    private class ThrottleData
    {
        public int Count { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }
}
```

### Step 5: Register Services

Add to `src/Infrastructure/DependencyInjection.cs`:

```csharp
// Register throttle store
builder.Services.AddScoped<IEmailThrottleStore, EmailThrottleStore>();
```

### Step 6: Update Command Handler

Inject options and throttle store into your command handler:

```csharp
public class SendEmailCommandHandler : IRequestHandler<SendEmailCommand, Result>
{
    private readonly IEmailService _emailService;
    private readonly IEmailThrottleStore _throttleStore;
    private readonly EmailRateLimitingOptions _rateLimitingOptions;

    public SendEmailCommandHandler(
        IEmailService emailService,
        IEmailThrottleStore throttleStore,
        IOptions<EmailRateLimitingOptions> rateLimitingOptions)
    {
        _emailService = emailService;
        _throttleStore = throttleStore;
        _rateLimitingOptions = rateLimitingOptions.Value;
    }

    public async Task<Result> Handle(SendEmailCommand request, CancellationToken cancellationToken)
    {
        var userId = request.UserId.ToString();
        
        // Check per-user throttling limits
        var perMinuteLimit = _rateLimitingOptions.PerUser.PerMinute;
        var currentCount = await _throttleStore.GetSendCountAsync(userId, 1, cancellationToken);
        
        if (currentCount >= perMinuteLimit)
        {
            var retryAfterSeconds = await _throttleStore.GetRetryAfterSecondsAsync(userId, 1, cancellationToken);
            return Result.Failure(
                Error.Problem("Email.Throttled", $"Too many emails sent. Please try again in {retryAfterSeconds} seconds."));
        }

        // Increment count before processing
        await _throttleStore.IncrementSendCountAsync(userId, 1, cancellationToken);

        // Process the email
        var result = await _emailService.SendAsync(request.To, request.Subject, request.Body, cancellationToken);
        
        return result.IsSuccess 
            ? Result.Success() 
            : Result.Failure(result.Error);
    }
}
```

### Step 7: Apply Middleware to Endpoint

Add rate limiting policy to your endpoint:

```csharp
// In your endpoint group
emailGroup.MapPost("/send", async ([FromBody] SendEmailCommand command, ISender sender) =>
{
    var result = await sender.Send(command);
    return result.IsSuccess ? Results.Ok() : result.ToIResult();
})
.WithName("SendEmail")
.WithSummary("Send email")
.RequireRateLimiting("email-send-ip")  // Apply IP-level rate limiting
.RequireAuthorization();
```

### Step 8: Update CustomResults (if needed)

If using custom error codes, update `src/Web/Infrastructure/CustomResults.cs`:

```csharp
private static IResult CreateProblemResult(Error error)
{
    // Handle email throttling
    if (error.Code == "Email.Throttled")
    {
        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Type = "https://tools.ietf.org/html/rfc6585#section-4",
            Title = GetTitle(error),
            Detail = error.Description
        };

        var result = Results.Problem(problemDetails);
        var retryAfterSeconds = ExtractRetryAfterFromDescription(error.Description);
        
        if (retryAfterSeconds > 0)
        {
            return new RetryAfterResult(result, retryAfterSeconds);
        }

        return result;
    }
    
    // ... existing OTP handling ...
}
```

### Step 9: Add Configuration

Update `appsettings.json` and `appsettings.Development.json`:

```json
{
  "RateLimiting": {
    "Email": {
      "PerIp": {
        "PerMinute": 10,
        "PerHour": 100
      },
      "PerUser": {
        "PerMinute": 2,
        "PerHour": 20
      }
    }
  }
}
```

### Step 10: Write Tests

Create comprehensive tests in `tests/Application.FunctionalTests/`:

```csharp
public class EmailThrottlingTests : BaseTestFixture
{
    private IEmailThrottleStore _throttleStore = null!;
    private EmailRateLimitingOptions _rateLimitingOptions = null!;

    [SetUp]
    public void SetUp()
    {
        _throttleStore = GetService<IEmailThrottleStore>();
        _rateLimitingOptions = GetService<IOptions<EmailRateLimitingOptions>>().Value;
    }

    [Test]
    public async Task SendEmail_WhenWithinLimits_ShouldSucceed()
    {
        // Arrange
        var command = new SendEmailCommand("user@example.com", "Subject", "Body");

        // Act
        var result = await SendAsync(command);

        // Assert
        result.ShouldBeSuccessful();
    }

    [Test]
    public async Task SendEmail_WhenExceedsPerUserLimit_ShouldReturnThrottled()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var perMinuteLimit = _rateLimitingOptions.PerUser.PerMinute;

        // Simulate exceeding the limit
        for (int i = 0; i < perMinuteLimit; i++)
        {
            await _throttleStore.IncrementSendCountAsync(userId, 1);
        }

        var command = new SendEmailCommand("user@example.com", "Subject", "Body");

        // Act
        var result = await SendAsync(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.Throttled");
    }
}
```

## Best Practices

### Configuration
- Use meaningful section names in `appsettings.json`
- Provide sensible defaults in options classes
- Make limits configurable per environment
- Document the rationale for chosen limits

### Error Handling
- Use consistent error codes (`Resource.Throttled`, `Resource.LockedOut`)
- Include retry timing in error messages
- Always include `Retry-After` headers for 429 responses
- Log throttling events for monitoring

### Performance
- Use distributed cache for multi-instance deployments
- Implement atomic increment operations
- Set appropriate cache expiration times
- Consider memory usage for high-traffic endpoints

### Security
- Implement both IP-level and resource-level limits
- Use normalized identifiers (phone numbers, user IDs)
- Log suspicious activity patterns
- Consider progressive penalties for repeat offenders

### Testing
- Test both success and throttling scenarios
- Verify retry-after timing accuracy
- Test cache expiration behavior
- Include integration tests for middleware

## Common Patterns

### Resource-Based Throttling
```csharp
// Per-user, per-phone, per-restaurant, etc.
var key = $"{resourceType}:{resourceId}:{windowMinutes}m";
```

### Time Windows
```csharp
// Common windows: 1min, 5min, 15min, 1hour, 24hour
var windows = new[] { 1, 5, 15, 60, 1440 };
```

### Error Codes
```csharp
// Consistent naming: {Resource}.{Action}
"Otp.Throttled", "Email.Throttled", "Upload.Throttled"
"Otp.LockedOut", "Account.LockedOut", "Payment.LockedOut"
```

## Monitoring and Observability

### Logging
```csharp
_logger.LogWarning("Rate limit exceeded for {ResourceType} {ResourceId}: {Count}/{Limit} in {Window}min", 
    resourceType, resourceId, currentCount, limit, windowMinutes);
```

### Metrics
- Track throttling rates by endpoint
- Monitor retry-after distribution
- Alert on unusual throttling patterns
- Dashboard for rate limiting effectiveness

## Migration Checklist

When adding rate limiting to existing endpoints:

- [ ] Define options class with sensible defaults
- [ ] Register options and middleware policies
- [ ] Create throttle store interface and implementation
- [ ] Update command handler with throttling logic
- [ ] Apply middleware policy to endpoint
- [ ] Update CustomResults if needed
- [ ] Add configuration to appsettings files
- [ ] Write comprehensive tests
- [ ] Update API documentation
- [ ] Add monitoring and alerting
- [ ] Test in staging environment
- [ ] Plan rollout strategy

## Related Documentation

- [Application Layer Guidelines](./Application_Layer_Guidelines.md)
- [Caching Guide](./Caching_Guide.md)
- [API Documentation Guidelines](./API_Documentation_Creation_Guidelines.md)

---

*This guide is based on the OTP rate limiting implementation. Refer to `src/Application/Auth/Commands/RequestPhoneOtp/` and related files for working examples.*
