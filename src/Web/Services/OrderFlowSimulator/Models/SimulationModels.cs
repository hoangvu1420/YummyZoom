namespace YummyZoom.Web.Services.OrderFlowSimulator.Models;

public sealed class SimulationRequest
{
    public string Scenario { get; set; } = "happyPath"; // happyPath | fastHappyPath | rejected | cancelledByRestaurant
    public SimulationDelays? DelaysMs { get; set; }
    public int? EstimatedDeliveryMinutes { get; set; }
}

public sealed class SimulationDelays
{
    public int? PlacedToAcceptedMs { get; set; }
    public int? AcceptedToPreparingMs { get; set; }
    public int? PreparingToReadyMs { get; set; }
    public int? ReadyToDeliveredMs { get; set; }
    public int? PlacedToRejectedMs { get; set; }
    public int? AcceptedToCancelledMs { get; set; }
}

public sealed class SimulationStartResult
{
    public Guid RunId { get; set; }
    public Guid OrderId { get; set; }
    public string Scenario { get; set; } = string.Empty;
    public string Status { get; set; } = "Started";
    public DateTime StartedAtUtc { get; set; }
    public string? NextStep { get; set; }
    public string? Notes { get; set; }
}

