namespace YummyZoom.Application.Orders.Queries.Common;

/// <summary>
/// Centralizes commonly used status sets & ordering priorities for Order queries
/// to avoid duplication and guarantee consistent semantics across handlers.
/// </summary>
public static class OrderQueryConstants
{
    /// <summary>
    /// Statuses considered "active" (appear on restaurant dashboard list).
    /// </summary>
    public static readonly string[] ActiveStatuses =
    {
        "Placed",
        "Accepted",
        "Preparing",
        "ReadyForDelivery"
    };

    /// <summary>
    /// Status priority for ordering in active lists (lower number = higher priority).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, int> StatusPriority = new Dictionary<string, int>
    {
        { "Placed", 1 },
        { "Accepted", 2 },
        { "Preparing", 3 },
        { "ReadyForDelivery", 4 },
        { "Delivered", 5 },
        { "Cancelled", 6 },
        { "Rejected", 7 },
        { "AwaitingPayment", 8 } // Typically excluded; added for completeness.
    };

    /// <summary>
    /// Returns a CASE expression snippet (PostgreSQL) mapping status to ordering priority.
    /// Example produced: CASE o."Status" WHEN 'Placed' THEN 1 WHEN 'Accepted' THEN 2 ... ELSE 999 END
    /// </summary>
    public static string BuildStatusOrderCase(string statusColumnAlias = "o.\"Status\"")
    {
        var parts = StatusPriority
            .Select(kv => $"WHEN '{kv.Key}' THEN {kv.Value}");
        return $"CASE {statusColumnAlias} {string.Join(' ', parts)} ELSE 999 END";
    }
}
