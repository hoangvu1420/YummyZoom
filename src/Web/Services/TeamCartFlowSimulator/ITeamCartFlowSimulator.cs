using YummyZoom.Web.Services.TeamCartFlowSimulator.Models;

namespace YummyZoom.Web.Services.TeamCartFlowSimulator;

public interface ITeamCartFlowSimulator
{
    /// <summary>
    /// Simulates a full team cart flow from creation to order conversion.
    /// Creates cart as host, joins members, adds items, locks, processes payments, and converts to order.
    /// </summary>
    Task<SimulationStartResult> SimulateFullFlowAsync(
        string hostPhone,
        string[] memberPhones,
        SimulationRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Simulates member actions on an existing team cart.
    /// Members join, add items, mark ready, and commit payments. Host actions are NOT simulated.
    /// </summary>
    Task<SimulationStartResult> SimulateMemberActionsAsync(
        Guid teamCartId,
        string[] memberPhones,
        SimulationRequest request,
        CancellationToken ct = default);
    
    // Manual control methods
    
    /// <summary>
    /// Triggers members to join the team cart. Members join one-by-one with delays.
    /// </summary>
    Task<SimulationActionResult> TriggerMembersJoinAsync(
        Guid teamCartId,
        int? delayBetweenMembersMs = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Triggers the item addition phase. Members add items automatically with delays between each item.
    /// </summary>
    Task<SimulationActionResult> TriggerStartAddingItemsAsync(
        Guid teamCartId,
        int? delayBetweenItemsMs = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Marks all members (including host) as ready.
    /// </summary>
    Task<SimulationActionResult> TriggerMarkReadyAsync(
        Guid teamCartId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Locks the team cart for payment (host action).
    /// </summary>
    Task<SimulationActionResult> TriggerLockAsync(
        Guid teamCartId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Triggers the payment phase. Members commit payments one-by-one with delays.
    /// </summary>
    Task<SimulationActionResult> TriggerStartPaymentsAsync(
        Guid teamCartId,
        int? delayBetweenPaymentsMs = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Converts the team cart to an order (host action).
    /// </summary>
    Task<SimulationActionResult> TriggerConvertAsync(
        Guid teamCartId,
        DeliveryAddress? address = null,
        string? deliveryNotes = null,
        CancellationToken ct = default);
}
