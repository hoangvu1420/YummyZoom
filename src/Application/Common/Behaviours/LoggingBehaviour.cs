using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using YummyZoom.Application.Common.Interfaces.IServices;
using YummyZoom.Application.Orders.Commands.HandleStripeWebhook;
using YummyZoom.Application.TeamCarts.Commands.HandleTeamCartStripeWebhook;

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

        // Reduce log noise for webhook commands (avoid logging full raw JSON bodies)
        if (request is HandleStripeWebhookCommand orderWebhook)
        {
            var (evtId, evtType) = ExtractEventInfo(orderWebhook.RawJson);
            _logger.LogInformation("YummyZoom Request: {Name} {@UserId} {@UserName} {EventId} {EventType} (payload omitted)",
                requestName, userIdString, userName, evtId, evtType);
        }
        else if (request is HandleTeamCartStripeWebhookCommand teamcartWebhook)
        {
            var (evtId, evtType) = ExtractEventInfo(teamcartWebhook.RawJson);
            _logger.LogInformation("YummyZoom Request: {Name} {@UserId} {@UserName} {EventId} {EventType} (payload omitted)",
                requestName, userIdString, userName, evtId, evtType);
        }
        else
        {
            _logger.LogInformation("YummyZoom Request: {Name} {@UserId} {@UserName} {@Request}",
                requestName, userIdString, userName, request);
        }

        return await next();
    }

    private static (string? id, string? type) ExtractEventInfo(string rawJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;
            string? id = null;
            string? type = null;
            if (root.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String)
            {
                id = idEl.GetString();
            }
            if (root.TryGetProperty("type", out var typeEl) && typeEl.ValueKind == JsonValueKind.String)
            {
                type = typeEl.GetString();
            }
            return (id, type);
        }
        catch
        {
            return (null, null);
        }
    }
}
