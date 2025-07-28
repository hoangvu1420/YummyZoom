namespace YummyZoom.Application.Common.Models;

public record WebhookEventResult(
    string EventId,
    string EventType,
    string RelevantObjectId
);
