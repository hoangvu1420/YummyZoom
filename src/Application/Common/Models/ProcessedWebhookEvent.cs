namespace YummyZoom.Application.Common.Models;

public class ProcessedWebhookEvent
{
    public required string Id { get; set; } // Stripe Event ID (evt_...)
    public DateTime ProcessedAt { get; set; }
}
