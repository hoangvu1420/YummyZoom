namespace YummyZoom.Web.Services.TeamCartFlowSimulator.Models;

public enum SimulationMode
{
    Automatic,  // Default: runs full flow automatically
    Manual      // Manual: waits for step-by-step commands
}

public enum SimulationState
{
    Initialized,
    WaitingForMembersJoin,
    MembersJoining,  // In progress
    MembersJoined,
    AddingItems,  // In progress
    ItemsAdded,
    AllMembersReady,
    Locked,
    ProcessingPayments,  // In progress
    AllPaymentsCommitted,
    Completed,
    Failed
}

public sealed class SimulationRequest
{
    public Guid? RestaurantId { get; set; } // Optional: defaults to first available restaurant
    public string Scenario { get; set; } = "happyPath"; // happyPath | fastHappyPath | memberCollaboration
    public SimulationMode Mode { get; set; } = SimulationMode.Automatic; // automatic (default) or manual
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
    
    // Manual mode delays - for automatic sub-sequences
    public int? MemberJoinDelayMs { get; set; }        // Delay between each member joining
    public int? ItemAdditionDelayMs { get; set; }      // Delay between each item addition
    public int? MemberPaymentDelayMs { get; set; }     // Delay between each member payment
}

public sealed class SimulationStartResult
{
    public Guid RunId { get; set; }
    public Guid TeamCartId { get; set; }
    public string? ShareToken { get; set; }
    public string Scenario { get; set; } = string.Empty;
    public SimulationMode Mode { get; set; } = SimulationMode.Automatic;
    public string Status { get; set; } = "Started";
    public DateTime StartedAtUtc { get; set; }
    public string? NextStep { get; set; }
    public string? CurrentStep { get; set; }  // Current simulation state
    public List<string> SimulatedMembers { get; set; } = new();
}

public sealed class SimulationActionResult
{
    public Guid TeamCartId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CurrentStep { get; set; } = string.Empty;
    public DateTime? ActionPerformedAtUtc { get; set; }
    
    // Optional fields for specific actions
    public long? QuoteVersion { get; set; }
    public decimal? GrandTotal { get; set; }
    public List<string>? Members { get; set; }
    public DateTime? EstimatedCompletionTimeUtc { get; set; }
    public Guid? OrderId { get; set; }
}

public sealed class DeliveryAddress
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}
