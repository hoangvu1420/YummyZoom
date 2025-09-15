using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IServices;

namespace YummyZoom.Infrastructure.Notifications.Sms;

public class LoggingSmsSender : ISmsSender
{
    private readonly ILogger<LoggingSmsSender> _logger;

    public LoggingSmsSender(ILogger<LoggingSmsSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string phoneE164, string message, CancellationToken ct = default)
    {
        _logger.LogInformation("[SMS] To {Phone}: {Message}", phoneE164, message);
        return Task.CompletedTask;
    }
}

