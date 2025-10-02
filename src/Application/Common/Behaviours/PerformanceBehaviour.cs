using System.Diagnostics;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IServices;

namespace YummyZoom.Application.Common.Behaviours;

public class PerformanceBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly Stopwatch _timer;
    private readonly ILogger<TRequest> _logger;
    private readonly IUser _user;

    public PerformanceBehaviour(
        ILogger<TRequest> logger,
        IUser user)
    {
        _timer = new Stopwatch();

        _logger = logger;
        _user = user;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _timer.Start();

        var response = await next();

        _timer.Stop();

        var elapsedMilliseconds = _timer.ElapsedMilliseconds;

        if (elapsedMilliseconds > 500)
        {
            var requestName = typeof(TRequest).Name;
            var userId = _user.Id ?? string.Empty;

            // Extract username from claims instead of making a database query
            var userName = _user.Principal?.FindFirst(ClaimTypes.Name)?.Value ??
                          _user.Principal?.FindFirst(ClaimTypes.Email)?.Value ??
                          "Unknown";

            _logger.LogWarning("YummyZoom Long Running Request: {Name} ({ElapsedMilliseconds} milliseconds) {@UserId} {@UserName} {@Request}",
                requestName, elapsedMilliseconds, userId, userName, request);
        }

        return response;
    }
}
