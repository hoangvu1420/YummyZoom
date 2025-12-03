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
}
