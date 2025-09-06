namespace YummyZoom.Infrastructure.TeamCart;

public sealed class TeamCartStoreOptions
{
    public const string SectionName = "TeamCartStore";

    // Redis key prefix, e.g., "yz:prod" or "yz:dev".
    public string KeyPrefix { get; set; } = "yz";

    // Sliding TTL in minutes for TeamCart VM keys.
    public int TtlMinutes { get; set; } = 60;

    // Pub/Sub channel name for cart updates.
    public string UpdatesChannel { get; set; } = "teamcart:updates";

    // Safety guard: maximum items allowed per cart in VM (soft limit).
    public int MaxItemsPerCart { get; set; } = 200;
}

