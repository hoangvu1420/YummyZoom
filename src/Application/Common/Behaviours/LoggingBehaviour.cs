using YummyZoom.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace YummyZoom.Application.Common.Behaviours;

public class LoggingBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private readonly ILogger _logger;
    private readonly IUser _user;
    private readonly IIdentityService _identityService;

    public LoggingBehaviour(ILogger<TRequest> logger, IUser user, IIdentityService identityService)
    {
        _logger = logger;
        _user = user;
        _identityService = identityService;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userIdString = _user.Id ?? string.Empty; 
        string? userName = string.Empty;

        if (!string.IsNullOrEmpty(userIdString))
        {
            if (Guid.TryParse(userIdString, out var userGuid))
            {
                userName = await _identityService.GetUserNameAsync(userGuid.ToString());
            }
            else
            {
                _logger.LogWarning("UserId is not a valid Guid: {UserId}", userIdString);
            }
        }

        _logger.LogInformation("YummyZoom Request: {Name} {@UserId} {@UserName} {@Request}",
            requestName, userIdString, userName, request);

        return await next();
    }
}
