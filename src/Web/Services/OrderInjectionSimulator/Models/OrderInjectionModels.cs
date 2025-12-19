using YummyZoom.Web.Services.OrderFlowSimulator.Models;

namespace YummyZoom.Web.Services.OrderInjectionSimulator.Models;

public sealed class OrderInjectionRequest
{
    public Guid? RestaurantId { get; set; }
    public int? Count { get; set; }
    public string Scenario { get; set; } = "incomingOnly"; // incomingOnly | autoFlow | fastAutoFlow
    public int? InterOrderDelayMs { get; set; } // delay between creating individual orders (ms)
    public SimulationDelays? DelaysMs { get; set; }
    public List<string>? NotePool { get; set; }
    public bool? UseRandomMenuItems { get; set; } = true;
    public int? MaxItemsPerOrder { get; set; }
    public int? MaxQuantityPerItem { get; set; }
}

public sealed class OrderInjectionResult
{
    public Guid RestaurantId { get; set; }
    public List<OrderInjectionOrder> Orders { get; set; } = new();
}

public sealed class OrderInjectionOrder
{
    public Guid OrderId { get; set; }
    public string Scenario { get; set; } = string.Empty;
    public string Status { get; set; } = "Placed";
    public string? FlowStatus { get; set; }
}
