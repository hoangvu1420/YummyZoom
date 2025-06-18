using Microsoft.Extensions.Logging;
using System.Security.Claims;
using YummyZoom.Application.Common.Interfaces.IServices;

namespace YummyZoom.Application.Common.Behaviours;

public class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly ILogger _logger;
    private readonly IUser _user;

    public LoggingBehaviour(ILogger<TRequest> logger, IUser user)
    {
        _logger = logger;
        _user = user;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userIdString = _user.Id ?? string.Empty; 
        
        // Extract username from claims instead of making a database query
        string? userName = _user.Principal?.FindFirst(ClaimTypes.Name)?.Value ?? 
                          _user.Principal?.FindFirst(ClaimTypes.Email)?.Value ?? 
                          "Unknown";

        _logger.LogInformation("YummyZoom Request: {Name} {@UserId} {@UserName} {@Request}",
            requestName, userIdString, userName, request);

        return await next();
    }
}
