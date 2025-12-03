namespace YummyZoom.Web.Services.TeamCartFlowSimulator.Models;

public sealed class SimulationRequest
{
    public Guid? RestaurantId { get; set; } // Optional: defaults to first available restaurant
    public string Scenario { get; set; } = "happyPath"; // happyPath | fastHappyPath | memberCollaboration
    public SimulationDelays? DelaysMs { get; set; }
}

public sealed class SimulationDelays
{
    public int? HostCreateToGuestJoinMs { get; set; }
    public int? GuestJoinToAddItemsMs { get; set; }
    public int? AddItemsToMemberReadyMs { get; set; }
    public int? AllReadyToLockMs { get; set; }
    public int? LockToMemberPaymentMs { get; set; }
    public int? PaymentToConvertMs { get; set; }
}

public sealed class SimulationStartResult
{
    public Guid RunId { get; set; }
    public Guid TeamCartId { get; set; }
    public string Scenario { get; set; } = string.Empty;
    public string Status { get; set; } = "Started";
    public DateTime StartedAtUtc { get; set; }
    public string? NextStep { get; set; }
    public List<string> SimulatedMembers { get; set; } = new();
}
